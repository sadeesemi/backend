using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Data;
using Moviequest_proj.Models;

namespace Moviequest_proj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MoviesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> AddMovie([FromForm] MovieCreateDto dto)
        {
            string? imagePath = null;

            if (dto.PosterImage != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.PosterImage.FileName);
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.PosterImage.CopyToAsync(stream);

                imagePath = $"/uploads/{fileName}";
            }

            var movie = new Movie
            {
                Title = dto.Title,
                Duration = dto.Duration,
                Description = dto.Description,
                Country = dto.Country,
                Language = dto.Language,
                Director = dto.Production,
                CastMembers = dto.Cast,
                Ratings = double.TryParse(dto.Rating, out var rate) ? rate : 0,
                ReleaseDate = int.TryParse(dto.Year, out var year)
                    ? new DateTime(year, 1, 1)
                    : DateTime.UtcNow,
                Image = imagePath
            };

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            foreach (var genreName in dto.Genres ?? [])
            {
                var trimmed = genreName.Trim().ToLower();
                var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name!.ToLower() == trimmed);
                if (genre == null)
                {
                    genre = new Genre { Name = genreName };
                    _context.Genres.Add(genre);
                    await _context.SaveChangesAsync();
                }

                _context.MovieGenres.Add(new MovieGenre
                {
                    MovieID = movie.MovieID,
                    GenreID = genre.GenreID
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                movie.MovieID,
                movie.Title,
                movie.Image,
                Year = movie.ReleaseDate.Year, // ✅ return only year
                movie.Description,
                movie.Ratings,
                Genres = dto.Genres
            });
        }

        [HttpGet("genre/{genreId}")]
        public async Task<IActionResult> GetMoviesByGenre(int genreId)
        {
            var movies = await _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .Where(m => m.MovieGenres.Any(mg => mg.GenreID == genreId))
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Image,
                    m.Description,
                    Rating = m.Reviews.Any() ? Math.Round(m.Reviews.Average(r => r.Rating), 1) : 0,
                    Genres = m.MovieGenres.Select(g => g.Genre.Name)
                })
                .ToListAsync();

            return Ok(movies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovieById(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.MovieID == id);

            if (movie == null)
                return NotFound();

            var averageRating = movie.Reviews.Any() ? Math.Round(movie.Reviews.Average(r => r.Rating), 1) : 0;

            return Ok(new
            {
                movie.MovieID,
                movie.Title,
                movie.Duration,
                movie.Image,
                movie.Language,
                Year = movie.ReleaseDate.Year, // ✅ return year only
                movie.Description,
                movie.Country,
                movie.Director,
                movie.CastMembers,
                Rating = averageRating,
                Genres = movie.MovieGenres.Select(g => g.Genre.Name).ToList(),
                Reviews = movie.Reviews.Select(r => new
                {
                    r.ReviewID,
                    r.Comment,
                    r.Rating,
                    createdAt = r.CreatedAt,
                    userEmail = r.User.Email,
                    userName = r.User.FullName ?? r.User.UserName
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMovies()
        {
            var movies = await _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Duration,
                    m.Image,
                    m.Language,
                    Year = m.ReleaseDate.Year, // ✅ return year only
                    m.Description,
                    m.Country,
                    m.Director,
                    m.CastMembers,
                    Rating = m.Reviews.Any()
                        ? Math.Round(m.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList()
                })
                .ToListAsync();

            return Ok(movies);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovie(int id, [FromForm] MovieCreateDto dto)
        {
            var movie = await _context.Movies.Include(m => m.MovieGenres).FirstOrDefaultAsync(m => m.MovieID == id);

            if (movie == null)
                return NotFound();

            if (dto.PosterImage != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.PosterImage.FileName);
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadPath);
                var filePath = Path.Combine(uploadPath, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.PosterImage.CopyToAsync(stream);
                movie.Image = $"/uploads/{fileName}";
            }

            movie.Title = dto.Title;
            movie.Description = dto.Description;
            movie.Language = dto.Language;
            movie.Country = dto.Country;
            movie.Director = dto.Production;
            movie.CastMembers = dto.Cast;
            movie.Duration = dto.Duration;
            movie.Ratings = double.TryParse(dto.Rating, out var rating) ? rating : 0;
            movie.ReleaseDate = int.TryParse(dto.Year, out var year)
                ? new DateTime(year, 1, 1)
                : DateTime.UtcNow;

            _context.MovieGenres.RemoveRange(movie.MovieGenres);

            foreach (var genreName in dto.Genres ?? [])
            {
                var trimmed = genreName.Trim().ToLower();
                var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name.ToLower() == trimmed);
                if (genre == null)
                {
                    genre = new Genre { Name = genreName };
                    _context.Genres.Add(genre);
                    await _context.SaveChangesAsync();
                }
                _context.MovieGenres.Add(new MovieGenre
                {
                    MovieID = movie.MovieID,
                    GenreID = genre.GenreID
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Movie updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            var movie = await _context.Movies.Include(m => m.MovieGenres).FirstOrDefaultAsync(m => m.MovieID == id);

            if (movie == null)
                return NotFound(new { message = "Movie not found" });

            if (movie.MovieGenres != null && movie.MovieGenres.Any())
            {
                _context.MovieGenres.RemoveRange(movie.MovieGenres);
            }

            try
            {
                if (!string.IsNullOrEmpty(movie.Image))
                {
                    var imagePath = movie.Image.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", imagePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            catch { }

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Movie deleted successfully" });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMovies([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty.");

            query = query.ToLower();

            var movies = await _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .Where(m => m.Title.ToLower().StartsWith(query))
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Duration,
                    m.Image,
                    m.Description,
                    Year = m.ReleaseDate.Year, // ✅ return only year
                    AverageRating = m.Reviews.Any()
                        ? Math.Round(m.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    Genres = m.MovieGenres.Select(g => g.Genre.Name)
                })
                .ToListAsync();

            return Ok(movies);
        }

        [HttpGet("toprated")]
        public async Task<IActionResult> GetTopRatedMovies()
        {
            var topMovies = await _context.Movies
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Image,
                    m.Description,
                    AverageRating = _context.Reviews
                        .Where(r => r.MovieID == m.MovieID)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = _context.Reviews.Count(r => r.MovieID == m.MovieID),
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                })
                .OrderByDescending(m => m.AverageRating)
                .ThenByDescending(m => m.ReviewCount)
                .Take(10)
                .ToListAsync();

            return Ok(topMovies);
        }
        [HttpGet("recommended/{userId}")]
        public async Task<IActionResult> GetRecommendedMovies(string userId)
        {
            // 1. Fetch user registration + activity info (fresh every time)
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

            if (user == null)
                return NotFound("User not found.");

            // 2. Determine era range
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

            // 3. Base movie query: exclude watched + era filter
            var moviesQuery = _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .Where(m => !user.WatchedMovieIds.Contains(m.MovieID)
                            && m.ReleaseDate >= eraStart && m.ReleaseDate <= eraEnd);

            // 4. Preferred language filter
            if (!string.IsNullOrWhiteSpace(user.PreferredLanguages))
            {
                var languages = user.PreferredLanguages
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().ToLower())
                    .ToList();

                moviesQuery = moviesQuery.Where(m => languages.Contains(m.Language!.ToLower()));
            }

            // 5. Determine favorite genres
            List<string> favoriteGenres = new List<string>();

            if (user.ReviewMovieIds.Any())
            {
                // Warm start: use reviewed movies
                favoriteGenres = await _context.Movies
                    .Where(m => user.ReviewMovieIds.Contains(m.MovieID))
                    .SelectMany(m => m.MovieGenres.Select(mg => mg.Genre.Name))
                    .Distinct()
                    .ToListAsync();
            }
            else if (!string.IsNullOrWhiteSpace(user.FavoriteMovies))
            {
                // Cold start: use favorite movies from registration
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
            {
                moviesQuery = moviesQuery.Where(m => m.MovieGenres.Any(mg => favoriteGenres.Contains(mg.Genre.Name)));
            }

            // 6. Fetch all matching movies
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
                .ToListAsync();

            // 7. Boost by search history (add score instead of strict filter)
            if (!string.IsNullOrWhiteSpace(user.SearchHistory))
            {
                var searchTerms = user.SearchHistory
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLower())
                    .ToList();

                allMovies = allMovies
                    .OrderByDescending(m => searchTerms.Count(term => m.Title.ToLower().Contains(term)))
                    .ThenByDescending(m => m.Rating)
                    .Take(15) // give a few more options
                    .ToList();
            }
            else
            {
                allMovies = allMovies
                    .OrderByDescending(m => m.Rating)
                    .Take(15)
                    .ToList();
            }

            // 8. Add randomness so results feel fresh
            var random = new Random();
            var finalMovies = allMovies
                .OrderBy(m => random.Next())
                .Take(10)
                .ToList();

            return Ok(finalMovies);
        }



        [HttpPost("filter")]
        public async Task<IActionResult> FilterMovies([FromBody] FilterDto filters)
        {
            // Start query with related tables
            var query = _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .AsQueryable();

            // Filter by genres if provided
            if (filters.Genres != null && filters.Genres.Any())
            {
                query = query.Where(m => m.MovieGenres.Any(mg => filters.Genres.Contains(mg.Genre.Name)));
            }

            // Filter by language if provided
            if (!string.IsNullOrEmpty(filters.Language))
            {
                query = query.Where(m => m.Language.ToLower() == filters.Language.ToLower());
            }

            // Filter by era if provided
            if (!string.IsNullOrEmpty(filters.Era))
            {
                switch (filters.Era)
                {
                    case "Classic (Before 1980)":
                        query = query.Where(m => m.ReleaseDate.Year < 1980);
                        break;
                    case "Modern (1980-2000)":
                        query = query.Where(m => m.ReleaseDate.Year >= 1980 && m.ReleaseDate.Year <= 2000);
                        break;
                    case "Contemporary (2000-Present)":
                        query = query.Where(m => m.ReleaseDate.Year > 2000);
                        break;
                }
            }

            // Fetch candidate movies
            var candidateMovies = await query
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Image,
                    m.Description,
                    Year = m.ReleaseDate.Year,
                    m.Language,
                    Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList(),
                    Rating = m.Reviews.Any() ? Math.Round(m.Reviews.Average(r => r.Rating), 1) : 0
                })
                .ToListAsync();

            // Boost score if movie matches favorite movies (optional)
            var favTitles = filters.FavoriteMovies?.Select(f => f.ToLower()).ToList() ?? new List<string>();
            var scoredMovies = candidateMovies.Select(m =>
            {
                double score = m.Rating;

                // Boost if movie is in favorite movies
                if (favTitles.Contains(m.Title.ToLower())) score += 1.5;

                return new { m.MovieID, m.Title, m.Image, m.Description, m.Genres, m.Language, m.Year, m.Rating, Score = score };
            })
            .OrderByDescending(m => m.Score)
            .Take(10)
            .ToList();

            return Ok(scoredMovies);
        }

    }
}
