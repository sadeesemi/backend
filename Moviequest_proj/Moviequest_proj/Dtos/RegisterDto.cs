namespace Moviequest_proj.Dtos
{
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PreferredLanguages { get; set; } = string.Empty;
        public string? FavoriteMovies { get; set; }
        public string MovieEraPreference { get; set; } = string.Empty;
        public string MovieWatchingFrequency { get; set; } = string.Empty;
        public bool EnjoyRewatching { get; set; }
        public bool EnjoyWatchingWithFamily { get; set; }
        public string? SearchHistory { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

    }
}