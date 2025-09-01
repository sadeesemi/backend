using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moviequest_proj.Dtos;
using Moviequest_proj.Models;
using System.Security.Claims;

namespace Moviequest_proj.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {

            Console.WriteLine("[DEBUG] GetProfile endpoint hit");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            return Ok(new
            {
                user.FullName,
                user.Email,
                user.Gender,
                DateOfBirth = user.DateOfBirth.ToString("yyyy-MM-dd"),
                user.PreferredLanguages,
                user.FavoriteMovies,
                user.MovieEraPreference,
                user.MovieWatchingFrequency,
                user.EnjoyRewatching,
                user.EnjoyWatchingWithFamily,
                user.SearchHistory
            });
        }

        // PUT: api/user/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] EditProfileDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            // Update basic fields
            user.FullName = model.FullName;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;

            // Update email if changed
            if (!string.IsNullOrWhiteSpace(model.Email) && model.Email != user.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != user.Id)
                    return BadRequest("Email is already in use by another user.");

                user.Email = model.Email;
                user.UserName = model.Email;
            }

            // Save user
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors);

            // Handle optional password change
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                    return BadRequest(passwordResult.Errors);
            }

            return Ok("Profile updated successfully.");
        }

        [HttpPost("add-search-history")]
        public async Task<IActionResult> AddToSearchHistory([FromBody] string movieTitle)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            // Append new movie title to existing history
            var currentHistory = user.SearchHistory ?? "";
            var updatedHistory = string.IsNullOrWhiteSpace(currentHistory)
                ? movieTitle
                : $"{currentHistory}, {movieTitle}";

            user.SearchHistory = updatedHistory;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest("Failed to update search history");

            return Ok("Search history updated");
        }


    }
}
