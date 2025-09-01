using Microsoft.AspNetCore.Http;

namespace Moviequest_proj.Models
{
    public class MovieCreateDto
    {
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public string? Language { get; set; }
        public string? Year { get; set; } // Will be parsed to DateTime
        public string? Rating { get; set; }
        public string? Description { get; set; }
        public string? Country { get; set; }
        public string? Production { get; set; }
        public string? Cast { get; set; }
        public List<string>? Genres { get; set; }

        public IFormFile? PosterImage { get; set; }
    }
}
