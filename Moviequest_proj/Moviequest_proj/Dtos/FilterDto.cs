public class FilterDto
{
    public List<string> Genres { get; set; } = new List<string>();
    public string Language { get; set; } = string.Empty;
    public string Era { get; set; } = string.Empty;
    public List<string> FavoriteMovies { get; set; } = new List<string>();
}