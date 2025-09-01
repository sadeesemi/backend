namespace Moviequest_proj.Dtos
{
    public class RequestDto
    {
        public string? MovieTitle { get; set; }
        public string? Language { get; set; }
        public string? Duration { get; set; }
        public IFormFile? Image { get; set; }
        public string? Genres { get; set; }
        public DateTime ? ReleaseDate { get; set; }
        public string? Description { get; set; }
        public string? Country { get; set; }
        public string? Director { get; set; }
        public string? CastMembers { get; set; }
        public string? UserId { get; set; }
    }

}
