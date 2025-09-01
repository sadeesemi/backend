using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Data;
using Moviequest_proj.Models;
using System.Security.Claims;

namespace Moviequest_proj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WatchlistsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WatchlistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get all watchlists for the logged-in user with their movies
        [HttpGet]
        public async Task<IActionResult> GetUserWatchlists()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lists = await _context.WatchLists
                .Include(w => w.WatchListMovies)
                    .ThenInclude(wlm => wlm.Movie)
                        .ThenInclude(m => m.MovieGenres)
                            .ThenInclude(mg => mg.Genre)
                .Include(w => w.WatchListMovies)
                    .ThenInclude(wlm => wlm.Movie)
                        .ThenInclude(m => m.Reviews) // ✅ include reviews for average rating
                .Where(w => w.UserId == userId)
                .ToListAsync();

            // Map watchlists to include average rating per movie
            var result = lists.Select(wl => new
            {
                wl.WatchListID,
                wl.ListName,
                wl.Description,
                wl.CreatedDate,
                Movies = wl.WatchListMovies.Select(wlm => new
                {
                    wlm.Movie.MovieID,
                    wlm.Movie.Title,
                    wlm.Movie.Image,
                    wlm.Movie.Duration,
                    wlm.Movie.Description,
                    ReleaseDate = wlm.Movie.ReleaseDate,
                    Genres = wlm.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    AverageRating = wlm.Movie.Reviews.Any() ? Math.Round(wlm.Movie.Reviews.Average(r => r.Rating), 1) : 0
                }).ToList()
            });

            return Ok(result);
        }


        // ✅ Create a new watchlist
        [HttpPost]
        public async Task<IActionResult> CreateWatchlist([FromBody] WatchList dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var watchlist = new WatchList
            {
                ListName = dto.ListName,
                Description = dto.Description,
                UserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.WatchLists.Add(watchlist);
            await _context.SaveChangesAsync();
            return Ok(watchlist);
        }

        // ✅ Update watchlist details
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWatchlist(int id, [FromBody] WatchList dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var watchlist = await _context.WatchLists
                .FirstOrDefaultAsync(w => w.WatchListID == id && w.UserId == userId);

            if (watchlist == null) return NotFound("Watchlist not found.");

            watchlist.ListName = dto.ListName;
            watchlist.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(watchlist);
        }

        // ✅ Add movie to a watchlist
        [HttpPost("{watchlistId}/add-movie/{movieId}")]
        public async Task<IActionResult> AddMovieToWatchlist(int watchlistId, int movieId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var watchlist = await _context.WatchLists
                .Include(w => w.WatchListMovies)
                .FirstOrDefaultAsync(w => w.WatchListID == watchlistId && w.UserId == userId);

            if (watchlist == null) return NotFound("Watchlist not found.");

            var exists = watchlist.WatchListMovies.Any(wlm => wlm.MovieID == movieId);
            if (exists) return BadRequest("Movie already in watchlist.");

            var watchlistMovie = new WatchListMovie
            {
                WatchListID = watchlistId,
                MovieID = movieId
            };

            _context.WatchListMovies.Add(watchlistMovie);
            await _context.SaveChangesAsync();

            return Ok(watchlistMovie);
        }

        // ✅ Remove movie from a watchlist
        [HttpDelete("{watchlistId}/remove-movie/{movieId}")]
        public async Task<IActionResult> RemoveMovieFromWatchlist(int watchlistId, int movieId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var watchlistMovie = await _context.WatchListMovies
                .Include(wlm => wlm.WatchList)
                .FirstOrDefaultAsync(wlm => wlm.WatchListID == watchlistId
                                           && wlm.MovieID == movieId
                                           && wlm.WatchList.UserId == userId);

            if (watchlistMovie == null) return NotFound("Movie not found in watchlist.");

            _context.WatchListMovies.Remove(watchlistMovie);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Movie removed from watchlist." });
        }

        // ✅ Delete entire watchlist
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWatchlist(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var watchlist = await _context.WatchLists
                .Include(w => w.WatchListMovies)
                .FirstOrDefaultAsync(w => w.WatchListID == id && w.UserId == userId);

            if (watchlist == null) return NotFound("Watchlist not found.");

            _context.WatchListMovies.RemoveRange(watchlist.WatchListMovies);
            _context.WatchLists.Remove(watchlist);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Watchlist deleted." });
        }
    }
}
