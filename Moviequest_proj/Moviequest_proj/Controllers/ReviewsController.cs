using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Data;
using Moviequest_proj.Models;

namespace Moviequest_proj.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Submit a new review
        [HttpPost("{movieId}")]
        public async Task<IActionResult> PostReview(int movieId, [FromBody] ReviewDto reviewDto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return NotFound("Movie not found");

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MovieID == movieId && r.UserId == user.Id);

            if (existingReview != null)
            {
                return BadRequest("You have already submitted a review for this movie.");
            }

            var review = new Review
            {
                Comment = reviewDto.Comment,
                Rating = reviewDto.Rating,
                CreatedAt = DateTime.UtcNow,
                MovieID = movieId,
                UserId = user.Id
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                review.ReviewID,
                review.Comment,
                review.Rating,
                review.CreatedAt,
                userName = user.UserName
            });
        }
        [HttpPut("{movieId}")]
        public async Task<IActionResult> UpdateReview(int movieId, [FromBody] ReviewDto reviewDto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MovieID == movieId && r.UserId == user.Id);

            if (review == null) return NotFound("Review not found");

            review.Comment = reviewDto.Comment;
            review.Rating = reviewDto.Rating;
            review.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                review.ReviewID,
                review.Comment,
                review.Rating,
                review.CreatedAt,
                UserName = user.FullName ?? user.UserName,
                UserEmail = user.Email
            });
        }
        [HttpDelete("{movieId}")]
        public async Task<IActionResult> DeleteReview(int movieId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MovieID == movieId && r.UserId == user.Id);

            if (review == null) return NotFound("Review not found");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review deleted successfully" });
        }
        [HttpGet("with-reviews")]
        public async Task<IActionResult> GetMoviesWithReviews()
        {
            var moviesWithReviews = await _context.Movies
                .Where(m => m.Reviews.Any()) // Only movies with at least 1 review
                .Select(m => new
                {
                    m.MovieID,  // <-- changed here
                    m.Title,
                    m.Image,
                    Reviews = m.Reviews.Select(r => new
                    {
                        r.ReviewID,
                        r.Comment,
                        r.Rating,
                        r.CreatedAt,
                        UserName = r.User.UserName
                    }).ToList()
                })
                .ToListAsync();

            return Ok(moviesWithReviews);
        }

        [HttpGet("my-reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var myReviews = await _context.Reviews
                .Include(r => r.Movie)
                .Where(r => r.UserId == user.Id)
                .Select(r => new
                {
                    r.ReviewID,
                    r.Comment,
                    r.Rating,
                    Date = r.CreatedAt,
                    MovieId = r.MovieID,
                    MovieTitle = r.Movie.Title,
                    MovieImage = r.Movie.Image,
                    MovieYear = r.Movie.ReleaseDate.Year,
                    MovieDuration = r.Movie.Duration,
                    UserName = user.FullName ?? user.UserName,
                    UserEmail = user.Email
                })
                .ToListAsync();

            return Ok(myReviews);
        }






    }

}