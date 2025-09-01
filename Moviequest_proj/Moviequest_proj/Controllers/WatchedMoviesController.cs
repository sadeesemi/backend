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
    [Authorize] // ✅ Require login
    public class WatchedMoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WatchedMoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/WatchedMovies/{movieId}
        [HttpPost("{movieId}")]
        public async Task<IActionResult> MarkAsWatched(int movieId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // from JWT
            if (userId == null) return Unauthorized();

            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return NotFound("Movie not found");

            // check if already marked
            bool exists = await _context.WatchedMovies
                .AnyAsync(w => w.MovieID == movieId && w.UserId == userId);

            if (exists)
                return BadRequest("Already marked as watched");

            var watchedMovie = new WatchedMovie
            {
                MovieID = movieId,
                UserId = userId,
                WatchedDate = DateTime.UtcNow
            };

            _context.WatchedMovies.Add(watchedMovie);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Movie marked as watched ✅" });
        }
        // GET: api/WatchedMovies
        [HttpGet]
        public async Task<IActionResult> GetWatchedMovies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var watched = await _context.WatchedMovies
                .Include(w => w.Movie) // Include movie details
                .Where(w => w.UserId == userId)
                .Select(w => new {
                    id = w.Movie.MovieID,
                    title = w.Movie.Title,
                    image = w.Movie.Image,
                    duration = w.Movie.Duration,
                    year = w.Movie.ReleaseDate.Year
                })
                .ToListAsync();

            return Ok(watched);
        }

        // DELETE: api/WatchedMovies/{movieId}
        [HttpDelete("{movieId}")]
        public async Task<IActionResult> UnwatchMovie(int movieId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var watched = await _context.WatchedMovies
                .FirstOrDefaultAsync(w => w.MovieID == movieId && w.UserId == userId);

            if (watched == null) return NotFound("Movie not marked as watched");

            _context.WatchedMovies.Remove(watched);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Movie unmarked as watched ✅" });
        }

    }

}
