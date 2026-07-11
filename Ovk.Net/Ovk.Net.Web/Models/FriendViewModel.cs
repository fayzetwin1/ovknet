namespace Ovk.Net.Web.Models;

public enum UserSubscriptionStatus
{
    Absent = 0,
    Incoming = 1,
    Outgoing = 2,
    Mutual = 3
}

public class FriendViewModel
{
    public ulong Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? Status { get; set; }
    public DateTime Since { get; set; }
    public bool IsOnline { get; set; }
    public bool IsVerified { get; set; }
    public UserSubscriptionStatus SubscriptionStatus { get; set; }

    public string NormalAvatarUrl => !string.IsNullOrEmpty(AvatarUrl) ? AvatarUrl : $"/avatars/{Id}";
    public string MinisculeAvatarUrl => !string.IsNullOrEmpty(AvatarUrl) ? AvatarUrl : $"/avatars/{Id}";
    public string FullName => $"{FirstName} {LastName}".Trim();
}
