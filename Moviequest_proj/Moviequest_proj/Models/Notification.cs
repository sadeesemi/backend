namespace Moviequest_proj.Models
{
        public class Notification
        {
            public int NotificationID { get; set; }

            public string? Message { get; set; } 

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public bool IsRead { get; set; } = false;

            public string? UserId { get; set; }
            public User? User { get; set; }
        }
    }


