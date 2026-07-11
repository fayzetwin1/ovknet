using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Models;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers;

[Authorize]
public class FriendsController : Controller
{
    private readonly OvkDbContext _db;
    private readonly IChandlerContext _currentUser;
    private const string UserModel = "openvk\\Web\\Models\\Entities\\User";
    private const int PerPage = 6;
    private const byte RejectedRequestFlags = 0b10000000;

    public FriendsController(OvkDbContext db, IChandlerContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Route("friends{id}")]
    public async Task<IActionResult> Index(ulong id, string? act, int p = 1)
    {
        var targetProfile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == id);
        if (targetProfile == null) return NotFound();

        var currentProfile = await GetCurrentProfileAsync();
        var mode = NormalizeMode(act);
        if ((mode == "incoming" || mode == "outcoming") && currentProfile?.Id != id)
        {
            return Redirect($"/id{id}");
        }

        var page = Math.Max(1, p);
        var relationIds = BuildRelationIdsQuery(id, mode);
        var count = await relationIds.CountAsync();

        var profiles = await _db.Profiles
            .Where(p => relationIds.Contains(p.Id) && p.Deleted == 0)
            .OrderByDescending(p => p.Online)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * PerPage)
            .Take(PerPage)
            .ToListAsync();

        var viewModel = new FriendListViewModel
        {
            TargetProfile = targetProfile,
            CurrentProfileId = currentProfile?.Id,
            Mode = mode,
            Page = page,
            PerPage = PerPage,
            Count = count,
            Friends = await ToFriendViewModelsAsync(profiles, currentProfile?.Id)
        };

        return View(viewModel);
    }

    [HttpPost]
    [Route("setSub/user")]
    public async Task<IActionResult> SetSub(ulong id, string act)
    {
        var myProfile = await _currentUser.GetProfileAsync();
        if (myProfile == null) return Unauthorized();

        var targetProfileExists = await _db.Profiles.AnyAsync(p => p.Id == id && p.Deleted == 0);
        if (!targetProfileExists) return NotFound();
        if (myProfile.Id == id) return RedirectBack(id);

        if (act == "add")
        {
            var exists = await _db.Subscriptions.AnyAsync(s => s.FollowerId == myProfile.Id && s.TargetId == id && s.TargetModel == UserModel);
            if (!exists)
            {
                _db.Subscriptions.Add(new Subscription
                {
                    FollowerId = myProfile.Id,
                    TargetId = id,
                    TargetModel = UserModel,
                    Flags = 0
                });
            }
        }
        else if (act == "rem")
        {
            var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.FollowerId == myProfile.Id && s.TargetId == id && s.TargetModel == UserModel);
            if (sub != null)
            {
                _db.Subscriptions.Remove(sub);
            }
        }
        else if (act == "rej")
        {
            var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.FollowerId == id && s.TargetId == myProfile.Id && s.TargetModel == UserModel);
            if (sub != null)
            {
                sub.Flags = RejectedRequestFlags;
            }
        }

        await _db.SaveChangesAsync();

        return RedirectBack(id);
    }

    private async Task<Profile?> GetCurrentProfileAsync()
    {
        return await _currentUser.GetProfileAsync();
    }

    private IQueryable<ulong> BuildRelationIdsQuery(ulong profileId, string mode)
    {
        var userSubscriptions = _db.Subscriptions.Where(s => s.TargetModel == UserModel);
        var incoming = userSubscriptions.Where(s => s.TargetId == profileId);
        var outgoing = userSubscriptions.Where(s => s.FollowerId == profileId);
        var mutual = incoming
            .Join(outgoing, i => i.FollowerId, o => o.TargetId, (i, o) => i.FollowerId)
            .Distinct();

        return mode switch
        {
            "online" => BuildOnlineFriendIdsQuery(mutual),
            "incoming" => incoming
                .Where(i => i.Flags == 0 && !outgoing.Any(o => o.TargetId == i.FollowerId))
                .Select(i => i.FollowerId)
                .Distinct(),
            "followers" => incoming
                .Where(i => !outgoing.Any(o => o.TargetId == i.FollowerId))
                .Select(i => i.FollowerId)
                .Distinct(),
            "outcoming" => outgoing
                .Where(o => !incoming.Any(i => i.FollowerId == o.TargetId))
                .Select(o => o.TargetId)
                .Distinct(),
            _ => mutual
        };
    }

    private IQueryable<ulong> BuildOnlineFriendIdsQuery(IQueryable<ulong> friendIds)
    {
        var onlineThreshold = (ulong)Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300);

        return friendIds
            .Join(_db.Profiles, id => id, p => p.Id, (id, profile) => new { id, profile.Online })
            .Where(x => x.Online > onlineThreshold)
            .Select(x => x.id);
    }

    private async Task<List<FriendViewModel>> ToFriendViewModelsAsync(List<Profile> profiles, ulong? currentProfileId)
    {
        var statuses = currentProfileId.HasValue
            ? await GetSubscriptionStatusesAsync(currentProfileId.Value, profiles.Select(p => p.Id).ToList())
            : new Dictionary<ulong, UserSubscriptionStatus>();

        return profiles.Select(profile =>
        {
            statuses.TryGetValue(profile.Id, out var subscriptionStatus);

            return new FriendViewModel
            {
                Id = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                AvatarUrl = $"/avatars/{profile.Id}",
                Status = profile.Status,
                Since = profile.Since,
                IsOnline = IsOnline(profile),
                IsVerified = profile.Verified,
                SubscriptionStatus = subscriptionStatus
            };
        }).ToList();
    }

    private async Task<Dictionary<ulong, UserSubscriptionStatus>> GetSubscriptionStatusesAsync(ulong currentProfileId, List<ulong> profileIds)
    {
        var ids = profileIds.Where(id => id != currentProfileId).Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => UserSubscriptionStatus.Absent);

        if (ids.Count == 0) return result;

        var subscriptions = await _db.Subscriptions
            .Where(s => s.TargetModel == UserModel &&
                ((s.FollowerId == currentProfileId && ids.Contains(s.TargetId)) ||
                 (ids.Contains(s.FollowerId) && s.TargetId == currentProfileId)))
            .ToListAsync();

        var outgoing = subscriptions
            .Where(s => s.FollowerId == currentProfileId)
            .Select(s => s.TargetId)
            .ToHashSet();
        var incoming = subscriptions
            .Where(s => s.TargetId == currentProfileId)
            .Select(s => s.FollowerId)
            .ToHashSet();

        foreach (var id in ids)
        {
            result[id] = (incoming.Contains(id), outgoing.Contains(id)) switch
            {
                (true, true) => UserSubscriptionStatus.Mutual,
                (true, false) => UserSubscriptionStatus.Incoming,
                (false, true) => UserSubscriptionStatus.Outgoing,
                _ => UserSubscriptionStatus.Absent
            };
        }

        return result;
    }

    private IActionResult RedirectBack(ulong fallbackProfileId)
    {
        var referer = Request.Headers["Referer"].ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var absoluteReferer) &&
            string.Equals(absoluteReferer.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(absoluteReferer.PathAndQuery);
        }

        if (Url.IsLocalUrl(referer))
        {
            return Redirect(referer);
        }

        return Redirect($"/id{fallbackProfileId}");
    }

    private static string NormalizeMode(string? act)
    {
        return act switch
        {
            "online" => "online",
            "incoming" => "incoming",
            "followers" => "followers",
            "outcoming" => "outcoming",
            _ => "friends"
        };
    }

    private static bool IsOnline(Profile profile)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)profile.Online < 300;
    }
}
