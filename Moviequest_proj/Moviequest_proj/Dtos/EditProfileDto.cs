using System.ComponentModel.DataAnnotations;

namespace Moviequest_proj.Dtos
{
    public class EditProfileDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Optional password (only update if provided)
        public string? Password { get; set; }
    }
}
