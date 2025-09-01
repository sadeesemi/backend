
using System.Text.Json.Serialization;

namespace Moviequest_proj.Models
{
    public class Movie
    {
        public int MovieID { get; set; }

        public string? Title { get; set; }
        public string? Duration { get; set; }
        public string? Image { get; set; }

        public string? Language { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? Description { get; set; }
        public string? Country { get; set; }
        public string? Director { get; set; }
        public string? CastMembers { get; set; }

        public double Ratings { get; set; }

        // Relationships
        [JsonIgnore]
        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public ICollection<WatchListMovie> WatchListMovies { get; set; } = new List<WatchListMovie>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WatchedMovie> WatchedMovies { get; set; } = new List<WatchedMovie>();
    }
}
