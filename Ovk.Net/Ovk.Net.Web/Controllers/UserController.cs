using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Ovk.Net.Core.Models;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers;

public class UserController : Controller
{
    private readonly OvkDbContext _db;
    private readonly IChandlerContext _currentUser;
    private const string UserModel = "openvk\\Web\\Models\\Entities\\User";

    public UserController(OvkDbContext db, IChandlerContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [Route("id{id}")]
    public async Task<IActionResult> Index(ulong id)
    {
        var targetProfile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == id);
        if (targetProfile == null)
        {
            return NotFound();
        }

        var currentUserProfile = await _currentUser.GetProfileAsync();
        var currentUserId = currentUserProfile?.Id ?? 0;
        var isCurrentUser = currentUserId == id;

        var friendIds = BuildMutualFriendIdsQuery(id);
        var friendsCount = await friendIds.CountAsync();

        var friendsProfiles = await _db.Profiles
            .Where(p => friendIds.Contains(p.Id))
            .OrderByDescending(p => p.Online)
            .ThenBy(p => p.Id)
            .Take(6)
            .ToListAsync();

        var friendsViewModels = friendsProfiles.Select(p => new FriendViewModel
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            AvatarUrl = $"/avatars/{p.Id}",
            IsOnline = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)p.Online) < 300
        }).ToList();

        var onlineFriendIds = BuildOnlineFriendIdsQuery(friendIds);
        var onlineFriendsProfiles = await _db.Profiles
            .Where(p => onlineFriendIds.Contains(p.Id))
            .OrderByDescending(p => p.Online)
            .ThenBy(p => p.Id)
            .Take(6)
            .ToListAsync();
        var onlineFriendsViewModels = onlineFriendsProfiles.Select(p => new FriendViewModel
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            AvatarUrl = $"/avatars/{p.Id}",
            IsOnline = true
        }).ToList();

        // Followers (subscribe to the user but not necessarily mutual)
        var followersCount = await _db.Subscriptions
            .CountAsync(s => s.TargetId == id && s.TargetModel == UserModel);

        var subscriptionStatus = currentUserId != 0 && !isCurrentUser
            ? await GetSubscriptionStatusAsync(currentUserId, id)
            : UserSubscriptionStatus.Absent;
        var isFriend = subscriptionStatus == UserSubscriptionStatus.Mutual;
        var canSendMessage = currentUserId != 0; // TODO check privacy

        // Fetch posts for this user's wall
        var posts = await _db.Posts
            .Where(p => p.WallId == id && !p.Deleted)
            .OrderByDescending(p => p.Created)
            .ToListAsync();

        var ownerIds = posts.Select(p => p.OwnerId).Distinct().ToList();
        var owners = await _db.Profiles
            .Where(p => ownerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        
        var postViewModels = posts.Select(p => {
            owners.TryGetValue(p.OwnerId, out var ownerProfile);
            return new PostViewModel
            {
                Id = p.Id,
                Content = p.Content,
                Created = DateTimeOffset.FromUnixTimeSeconds((long)p.Created).UtcDateTime,
                OwnerId = p.OwnerId,
                OwnerName = ownerProfile != null ? $"{ownerProfile.FirstName} {ownerProfile.LastName}" : $"id{p.OwnerId}",
                OwnerAvatarUrl = ownerProfile != null ? $"/avatars/{ownerProfile.Id}" : null,
                IsOnline = ownerProfile != null && (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)ownerProfile.Online) < 300,
                IsVerified = ownerProfile?.Verified ?? false,
                IsFemale = ownerProfile?.Sex == false,
                TargetWallId = p.WallId,
                LikesCount = 0, // TODO: Implement likes
                RepostsCount = 0,
                CommentsCount = 0,
                IsLikedByCurrentUser = false,
                CanBeDeleted = currentUserId == p.OwnerId || currentUserId == p.WallId,
                CanBePinned = currentUserId == p.WallId,
                IsPinned = false // TODO: implement pins
            };
        }).ToList();

        var viewModel = new UserProfileViewModel
        {
            Profile = targetProfile,
            Posts = postViewModels,
            IsCurrentUser = isCurrentUser,
            FriendsCount = friendsCount,
            FollowersCount = followersCount,
            IsFriend = isFriend,
            SubscriptionStatus = subscriptionStatus,
            CanSendMessage = canSendMessage,
            Friends = friendsViewModels,
            FriendsOnline = onlineFriendsViewModels
        };

        return View(viewModel);
    }

    [HttpGet("{shortcode}", Order = 100)]
    public async Task<IActionResult> ByShortcode(string shortcode)
    {
        var profileId = await _db.Profiles
            .Where(x => x.Shortcode == shortcode && x.Deleted == 0)
            .Select(x => (ulong?)x.Id)
            .FirstOrDefaultAsync();
        return profileId.HasValue ? await Index(profileId.Value) : NotFound();
    }

    [Authorize]
    [HttpGet("edit")]
    public async Task<IActionResult> Edit([FromQuery] string act = "main")
    {
        var profile = await _currentUser.GetProfileAsync();
        if (profile is null) return Unauthorized();
        var mode = act is "contacts" or "interests" or "avatar" ? act : "main";
        return View(new ProfileEditViewModel
        {
            Mode = mode,
            ProfileId = profile.Id,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Status = profile.Status,
            Sex = profile.Sex,
            Hometown = profile.Hometown,
            City = profile.City,
            Info = profile.Info,
            About = profile.About,
            Email = profile.Email,
            Phone = profile.Phone
        });
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromQuery] string act, ProfileEditViewModel model)
    {
        var current = await _currentUser.GetProfileAsync();
        if (current is null) return Unauthorized();
        var mode = act is "contacts" or "interests" ? act : "main";
        model.Mode = mode;
        model.ProfileId = current.Id;
        if (mode != "main")
        {
            ModelState.Remove(nameof(model.FirstName));
            ModelState.Remove(nameof(model.LastName));
        }
        if (!ModelState.IsValid) return View(model);

        var profile = await _db.Profiles.FindAsync(current.Id);
        if (profile is null) return NotFound();
        if (mode == "main")
        {
            profile.FirstName = model.FirstName.Trim();
            profile.LastName = model.LastName.Trim();
            profile.Status = Clean(model.Status);
            profile.Sex = model.Sex;
            profile.Hometown = Clean(model.Hometown);
            profile.City = Clean(model.City);
        }
        else if (mode == "contacts")
        {
            profile.Email = Clean(model.Email);
            profile.Phone = Clean(model.Phone);
        }
        else
        {
            profile.Info = Clean(model.Info);
            profile.About = Clean(model.About);
        }
        await _db.SaveChangesAsync();
        TempData["ProfileSaved"] = true;
        return Redirect($"/edit?act={mode}");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<ulong> BuildMutualFriendIdsQuery(ulong profileId)
    {
        var userSubscriptions = _db.Subscriptions.Where(s => s.TargetModel == UserModel);
        var incoming = userSubscriptions.Where(s => s.TargetId == profileId);
        var outgoing = userSubscriptions.Where(s => s.FollowerId == profileId);

        return incoming
            .Join(outgoing, i => i.FollowerId, o => o.TargetId, (i, o) => i.FollowerId)
            .Distinct();
    }

    private IQueryable<ulong> BuildOnlineFriendIdsQuery(IQueryable<ulong> friendIds)
    {
        var onlineThreshold = (ulong)Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300);

        return friendIds
            .Join(_db.Profiles, id => id, p => p.Id, (id, profile) => new { id, profile.Online })
            .Where(x => x.Online > onlineThreshold)
            .Select(x => x.id);
    }

    private async Task<UserSubscriptionStatus> GetSubscriptionStatusAsync(ulong currentProfileId, ulong targetProfileId)
    {
        var relations = await _db.Subscriptions
            .Where(s => s.TargetModel == UserModel &&
                ((s.FollowerId == currentProfileId && s.TargetId == targetProfileId) ||
                 (s.FollowerId == targetProfileId && s.TargetId == currentProfileId)))
            .ToListAsync();

        var outgoing = relations.Any(s => s.FollowerId == currentProfileId && s.TargetId == targetProfileId);
        var incoming = relations.Any(s => s.FollowerId == targetProfileId && s.TargetId == currentProfileId);

        return (incoming, outgoing) switch
        {
            (true, true) => UserSubscriptionStatus.Mutual,
            (true, false) => UserSubscriptionStatus.Incoming,
            (false, true) => UserSubscriptionStatus.Outgoing,
            _ => UserSubscriptionStatus.Absent
        };
    }
}
