

namespace Moviequest_proj.Models
{
    public class WatchedMovie
    {
        public int WatchedMovieID { get; set; }
        public DateTime WatchedDate { get; set; }

        public string ?UserId { get; set; }
        public User? User { get; set; }

        public int MovieID { get; set; }
        public Movie? Movie { get; set; }
    } 
}
