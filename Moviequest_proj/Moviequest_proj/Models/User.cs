using Microsoft.AspNetCore.Identity;

namespace Moviequest_proj.Models
{
    public class User : IdentityUser
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

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; } 

        public ICollection<Request> Requests { get; set; } = [];
        public ICollection<WatchList> WatchLists { get; set; } = [];
        public ICollection<WatchedMovie> WatchedMovies { get; set; } = [];
        public ICollection<Review> Reviews { get; set; } = [];
    }
}
