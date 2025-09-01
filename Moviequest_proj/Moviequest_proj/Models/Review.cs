

namespace Moviequest_proj.Models { 
    public class Review
    {
        public int ReviewID { get; set; }
        public string ?Comment { get; set; }
        public double Rating { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? UserId { get; set; }
        public User? User { get; set; }

        public int MovieID { get; set; }
        public Movie? Movie { get; set; }
    }
}