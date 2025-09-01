using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Data;
using Moviequest_proj.Dtos;
using Moviequest_proj.Models;

namespace Moviequest_proj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RequestController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ USER: Create request
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRequest([FromForm] RequestDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.MovieTitle))
                return BadRequest("Invalid data.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            string? imageUrl = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                imageUrl = $"/uploads/{fileName}";
            }

            var request = new Request
            {
                MovieTitle = model.MovieTitle,
                Language = model.Language,
                Duration = model.Duration,
                Image = imageUrl,
                Genres = model.Genres,
                ReleaseDate = model.ReleaseDate,
                Description = model.Description,
                Country = model.Country,
                Director = model.Director,
                CastMembers = model.CastMembers,
                Status = "Pending",
                RequestDate = DateTime.UtcNow,
                UserId = userId
            };

            _context.Requests.Add(request);

            // ✅ Create admin notifications
            var admins = await _context.Users
                .Where(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == _context.Roles.FirstOrDefault(r => r.Name == "Admin")!.Id))
                .ToListAsync();

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Message = $"New movie request: '{request.MovieTitle}' by a user.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, requestId = request.RequestID });
        }


        // ✅ ADMIN: View all requests with user details
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _context.Requests
                .Include(r => r.User)
                .Select(r => new
                {
                    r.RequestID,
                    r.MovieTitle,
                    r.Language,
                    r.Duration,
                    r.Image,
                    r.Genres,
                    r.ReleaseDate,
                    r.Description,
                    r.Country,
                    r.Director,
                    r.CastMembers,
                    r.Status,
                    r.RequestDate,
                    User = new
                    {
                        r.User.Id,
                        r.User.UserName,
                        r.User.Email
                    }
                })
                .ToListAsync();

            return Ok(requests);
        }

        // ✅ ADMIN: Edit a request
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditRequest(int id, [FromForm] RequestDto model)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound("Request not found.");

            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                request.Image = $"/uploads/{fileName}";
            }

            request.MovieTitle = model.MovieTitle;
            request.Language = model.Language;
            request.Duration = model.Duration;
            request.Genres = model.Genres;
            request.ReleaseDate = model.ReleaseDate;
            request.Description = model.Description;
            request.Country = model.Country;
            request.Director = model.Director;
            request.CastMembers = model.CastMembers;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Request updated by admin." });
        }

        // ✅ ADMIN: Change request status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRequestStatus(int id, [FromQuery] string status)
        {
            var request = await _context.Requests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null) return NotFound("Request not found.");
            if (status != "Approved" && status != "Rejected")
                return BadRequest("Invalid status. Use 'Approved' or 'Rejected'.");

            request.Status = status;

            // Create notification if User exists
            if (!string.IsNullOrEmpty(request.UserId))
            {
                string message = status switch
                {
                    "Approved" => $"Your request for '{request.MovieTitle}' has been approved.",
                    "Rejected" => $"Your request for '{request.MovieTitle}' has been rejected.",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(message))
                {
                    var notification = new Notification
                    {
                        UserId = request.UserId,
                        Message = message,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
            }

            // Add movie if approved
            if (status == "Approved")
            {
                var movie = new Movie
                {
                    Title = request.MovieTitle,
                    Duration = request.Duration,
                    Description = request.Description,
                    Country = request.Country,
                    Language = request.Language,
                    Director = request.Director,
                    CastMembers = request.CastMembers,
                    Ratings = 0,
                    ReleaseDate = request.ReleaseDate ?? DateTime.UtcNow,
                    Image = request.Image
                };
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync(); // MovieID is generated

                // Add genres
                if (!string.IsNullOrEmpty(request.Genres))
                {
                    var genreNames = request.Genres.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var genreName in genreNames)
                    {
                        var trimmed = genreName.Trim();
                        var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == trimmed);
                        if (genre == null)
                        {
                            genre = new Genre { Name = trimmed };
                            _context.Genres.Add(genre);
                            await _context.SaveChangesAsync();
                        }
                        _context.MovieGenres.Add(new MovieGenre
                        {
                            MovieID = movie.MovieID,
                            GenreID = genre.GenreID
                        });
                    }
                }

                // Movie added notification
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = request.UserId,
                        Message = $"The movie '{request.MovieTitle}' has been added to the system.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"Request status changed to '{status}'." });
        }

        // ✅ ADMIN: Delete a request
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound("Request not found.");

            // Delete image file if exists
            if (!string.IsNullOrEmpty(request.Image))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", request.Image.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Request deleted." });
        }
    }
}
