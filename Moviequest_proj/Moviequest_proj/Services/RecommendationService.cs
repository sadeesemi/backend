using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Data;

namespace Moviequest_proj.Services
{
    public class RecommendationService
    {
        private readonly ApplicationDbContext _context;

        public RecommendationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<object>> GetRecommendedMoviesForUser(string userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.PreferredLanguages,
                    u.FavoriteMovies,
                    u.MovieEraPreference,
                    u.SearchHistory,
                    WatchedMovieIds = u.WatchedMovies.Select(w => w.MovieID).ToList(),
                    ReviewMovieIds = u.Reviews.Select(r => r.MovieID).ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null) return new List<object>();

            DateTime eraStart, eraEnd;
            switch (user.MovieEraPreference)
            {
                case "Classic (Before 1980)":
                    eraStart = DateTime.MinValue;
                    eraEnd = new DateTime(1979, 12, 31);
                    break;
                case "Modern (1980-2000)":
                    eraStart = new DateTime(1980, 1, 1);
                    eraEnd = new DateTime(2000, 12, 31);
                    break;
                case "Contemporary (2000-Present)":
                    eraStart = new DateTime(2001, 1, 1);
                    eraEnd = DateTime.MaxValue;
                    break;
                default:
                    eraStart = DateTime.MinValue;
                    eraEnd = DateTime.MaxValue;
                    break;
            }

            var moviesQuery = _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .Where(m => !user.WatchedMovieIds.Contains(m.MovieID)
                            && m.ReleaseDate >= eraStart && m.ReleaseDate <= eraEnd);

            if (!string.IsNullOrWhiteSpace(user.PreferredLanguages))
            {
                var languages = user.PreferredLanguages
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().ToLower())
                    .ToList();

                moviesQuery = moviesQuery.Where(m => languages.Contains(m.Language!.ToLower()));
            }

            List<string> favoriteGenres = new List<string>();
            if (user.ReviewMovieIds.Any())
            {
                favoriteGenres = await _context.Movies
                    .Where(m => user.ReviewMovieIds.Contains(m.MovieID))
                    .SelectMany(m => m.MovieGenres.Select(mg => mg.Genre.Name))
                    .Distinct()
                    .ToListAsync();
            }
            else if (!string.IsNullOrWhiteSpace(user.FavoriteMovies))
            {
                var favoriteTitles = user.FavoriteMovies
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .ToList();

                favoriteGenres = await _context.Movies
                    .Where(m => favoriteTitles.Contains(m.Title!.ToLower()))
                    .SelectMany(m => m.MovieGenres.Select(mg => mg.Genre.Name))
                    .Distinct()
                    .ToListAsync();
            }

            if (favoriteGenres.Any())
                moviesQuery = moviesQuery.Where(m => m.MovieGenres.Any(mg => favoriteGenres.Contains(mg.Genre.Name)));

            var allMovies = await moviesQuery
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Image,
                    m.Description,
                    Rating = m.Reviews.Any() ? Math.Round(m.Reviews.Average(r => r.Rating), 1) : 0,
                    Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList(),
                    Year = m.ReleaseDate.Year
                })
                .ToListAsync<object>(); // ✅ Anonymous object list

            if (!string.IsNullOrWhiteSpace(user.SearchHistory))
            {
                var searchTerms = user.SearchHistory
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLower())
                    .ToList();

                allMovies = allMovies
                    .OrderByDescending(m => searchTerms.Count(term => m.GetType().GetProperty("Title")!.GetValue(m)!.ToString()!.ToLower().Contains(term)))
                    .ThenByDescending(m => (double)m.GetType().GetProperty("Rating")!.GetValue(m)!)
                    .Take(10)
                    .ToList();
            }
            else
            {
                allMovies = allMovies
                    .OrderByDescending(m => (double)m.GetType().GetProperty("Rating")!.GetValue(m)!)
                    .Take(10)
                    .ToList();
            }

            return allMovies;
        }
    }
}
