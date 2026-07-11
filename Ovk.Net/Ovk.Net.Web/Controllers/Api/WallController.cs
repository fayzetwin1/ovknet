using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ovk.Net.Core.Models;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers.Api;

[Route("api/wall")]
[Authorize]
public class WallController : Controller
{
    private readonly OvkDbContext _db;
    private readonly IChandlerContext _currentUser;

    public WallController(OvkDbContext db, IChandlerContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddPost([FromForm] ulong wallId, [FromForm] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new { error = "Content cannot be empty" });
        }

        var profile = await _currentUser.GetProfileAsync();
        if (profile == null) return Unauthorized();

        var post = new Post
        {
            OwnerId = profile.Id,
            WallId = wallId,
            Content = content,
            Created = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Deleted = false,
            Nsfw = false,
            Ad = false,
            Flags = 0,
            VirtualId = 0 // Normally assigned some meaningful ID, 0 for now
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        var viewModel = new PostViewModel
        {
            Id = post.Id,
            Content = post.Content,
            Created = DateTimeOffset.FromUnixTimeSeconds((long)post.Created).UtcDateTime,
            OwnerId = profile.Id,
            OwnerName = $"{profile.FirstName} {profile.LastName}",
            OwnerAvatarUrl = $"/avatars/{profile.Id}",
            IsOnline = true,
            IsVerified = profile.Verified,
            IsFemale = profile.Sex == false,
            TargetWallId = post.WallId,
            LikesCount = 0,
            RepostsCount = 0,
            CommentsCount = 0,
            IsLikedByCurrentUser = false,
            CanBeDeleted = true,
            CanBePinned = profile.Id == post.WallId,
            IsPinned = false
        };

        return PartialView("_Post", viewModel);
    }
}
