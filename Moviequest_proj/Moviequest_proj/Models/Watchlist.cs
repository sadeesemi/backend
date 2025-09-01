using Moviequest_proj.Models;

public class WatchList
{
    public int WatchListID { get; set; }
    public string? ListName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }
    public User? User { get; set; }

  
    public ICollection<WatchListMovie> WatchListMovies { get; set; } = new List<WatchListMovie>();
}
