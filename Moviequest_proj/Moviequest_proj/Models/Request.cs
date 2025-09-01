namespace Moviequest_proj.Models
{
    public class Request
    {
        public int RequestID { get; set; }


        public string? Language { get; set; }

        public string? MovieTitle { get; set; }
        public string ?Duration { get; set; }
        public string ?Image { get; set; }
        public string? Genres { get; set; } 
        public DateTime? ReleaseDate { get; set; }
        public string ?Description { get; set; }
        public string ?Country { get; set; }
        public string? Director { get; set; }
        public string? CastMembers { get; set; } 
        public string? Status { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }
        public User? User { get; set; }
    }
}
