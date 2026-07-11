namespace Ovk.Net.Web.Models;

public class PostViewModel
{
    public ulong Id { get; set; }
    public string Content { get; set; } = null!;
    public DateTime Created { get; set; }
    
    // Author details
    public ulong OwnerId { get; set; }
    public string OwnerName { get; set; } = null!;
    public string? OwnerAvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFemale { get; set; }

    public string OwnerMinisculeAvatarUrl => !string.IsNullOrEmpty(OwnerAvatarUrl) ? OwnerAvatarUrl : $"/avatars/{OwnerId}";
    
    // Target Wall
    public ulong TargetWallId { get; set; }
    
    // Interactions
    public int LikesCount { get; set; }
    public int RepostsCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    
    // Permissions
    public bool CanBeDeleted { get; set; }
    public bool CanBePinned { get; set; }
    public bool IsPinned { get; set; }
    
    // UI Helpers
    public string PrettyDate
    {
        get
        {
            var diff = DateTime.UtcNow - Created;
            if (diff.TotalMinutes < 1) return "Только что";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} минут назад";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} часов назад";
            if (diff.TotalDays < 2) return "Вчера в " + Created.ToString("HH:mm");
            return Created.ToString("d MMMM yyyy");
        }
    }
}
