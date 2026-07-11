using Ovk.Net.Core.Models;
using System.Collections.Generic;

namespace Ovk.Net.Web.Models;

public class UserProfileViewModel
{
    public Profile Profile { get; set; } = null!;
    public List<PostViewModel> Posts { get; set; } = new();
    public bool IsCurrentUser { get; set; }
    public int FriendsCount { get; set; }
    public int FollowersCount { get; set; }
    public bool IsFriend { get; set; }
    public UserSubscriptionStatus SubscriptionStatus { get; set; }
    public bool CanSendMessage { get; set; }
    public List<FriendViewModel> Friends { get; set; } = new();
    public List<FriendViewModel> FriendsOnline { get; set; } = new();

    public string NormalAvatarUrl => $"/avatars/{Profile.Id}";
}
