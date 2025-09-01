

namespace Moviequest_proj.Models
{
    public class WatchListMovie
    {
        public int WatchListMovieID { get; set; }

        public int WatchListID { get; set; }
        public WatchList WatchList { get; set; } = null!; // Required reference

        public int MovieID { get; set; }
        public Movie Movie { get; set; } = null!; // Required reference
    }
}
