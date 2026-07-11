using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Models;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers;

[Authorize]
[Route("im")]
public class ImController : Controller
{
    private const string UserModel = "openvk\\Web\\Models\\Entities\\User";
    private const int DialogsPerPage = 10;
    private const int MessagesPerPage = 50;
    private readonly OvkDbContext _db;
    private readonly IChandlerContext _currentUser;

    public ImController(OvkDbContext db, IChandlerContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ulong? sel, [FromQuery] int p = 1, [FromQuery] string? q = null)
    {
        var myProfile = await _currentUser.GetProfileAsync();
        if (myProfile == null) return Unauthorized();

        if (sel.HasValue)
        {
            return await Chat(myProfile, sel.Value);
        }

        var page = Math.Max(1, p);
        var messageQuery = BuildUserMessagesQuery(myProfile.Id);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = q.Trim();
            messageQuery = messageQuery.Where(m => m.Content.Contains(pattern));
        }
        var peerQuery = messageQuery
            .Select(m => m.SenderId == myProfile.Id ? m.RecipientId : m.SenderId)
            .Distinct();
        var count = await peerQuery.CountAsync();

        var peers = await messageQuery
            .GroupBy(m => m.SenderId == myProfile.Id ? m.RecipientId : m.SenderId)
            .Select(g => new
            {
                PeerId = g.Key,
                LastCreated = g.Max(m => m.Created),
                LastId = g.Max(m => m.Id)
            })
            .OrderByDescending(p => p.LastCreated)
            .ThenByDescending(p => p.LastId)
            .Skip((page - 1) * DialogsPerPage)
            .Take(DialogsPerPage)
            .ToListAsync();

        var peerIds = peers.Select(p => p.PeerId).ToList();
        var peerProfiles = await _db.Profiles
            .Where(p => peerIds.Contains(p.Id) && p.Deleted == 0)
            .ToDictionaryAsync(p => p.Id);

        var dialogs = new List<ImDialogViewModel>();
        foreach (var peer in peers)
        {
            if (!peerProfiles.TryGetValue(peer.PeerId, out var peerProfile))
            {
                continue;
            }

            var lastMessage = await BuildConversationQuery(myProfile.Id, peer.PeerId)
                .OrderByDescending(m => m.Created)
                .ThenByDescending(m => m.Id)
                .FirstOrDefaultAsync();
            if (lastMessage == null)
            {
                continue;
            }

            dialogs.Add(new ImDialogViewModel
            {
                PeerProfile = peerProfile,
                LastMessage = ToMessageViewModel(lastMessage, myProfile, peerProfile),
                UnreadCount = await BuildConversationQuery(myProfile.Id, peer.PeerId)
                    .CountAsync(m => m.SenderId == peer.PeerId && m.RecipientId == myProfile.Id && m.Unread)
            });
        }

        return View("Index", new ImIndexViewModel
        {
            CurrentProfile = myProfile,
            Dialogs = dialogs,
            Page = page,
            PerPage = DialogsPerPage,
            Count = count
        });
    }

    [HttpPost("send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromForm] ulong peerId, [FromForm] string content)
    {
        var myProfile = await _currentUser.GetProfileAsync();
        if (myProfile == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(content))
        {
            return Redirect($"/im?sel={peerId}");
        }

        var peerProfile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == peerId && p.Deleted == 0);
        if (peerProfile == null) return NotFound();

        var unreadIncoming = await BuildConversationQuery(myProfile.Id, peerId)
            .Where(m => m.SenderId == peerId && m.RecipientId == myProfile.Id && m.Unread)
            .ToListAsync();
        foreach (var message in unreadIncoming)
        {
            message.Unread = false;
        }

        _db.Messages.Add(new Message
        {
            SenderType = UserModel,
            SenderId = myProfile.Id,
            RecipientType = UserModel,
            RecipientId = peerId,
            Content = content.Trim(),
            Created = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Unread = true,
            Deleted = false,
            Ad = false
        });

        await _db.SaveChangesAsync();
        return Redirect($"/im?sel={peerId}");
    }

    [HttpPost("search")]
    [ValidateAntiForgeryToken]
    public IActionResult Search([FromForm] string pattern)
    {
        return string.IsNullOrWhiteSpace(pattern)
            ? Redirect("/im")
            : Redirect($"/im?q={Uri.EscapeDataString(pattern.Trim())}");
    }

    private async Task<IActionResult> Chat(Profile myProfile, ulong peerId)
    {
        var peerProfile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == peerId && p.Deleted == 0);
        if (peerProfile == null) return NotFound();

        var messages = await BuildConversationQuery(myProfile.Id, peerId)
            .OrderByDescending(m => m.Created)
            .ThenByDescending(m => m.Id)
            .Take(MessagesPerPage)
            .ToListAsync();

        var unread = messages.Where(m => m.SenderId == peerId && m.RecipientId == myProfile.Id && m.Unread).ToList();
        if (unread.Count > 0)
        {
            foreach (var message in unread)
            {
                message.Unread = false;
            }

            await _db.SaveChangesAsync();
        }

        var messageViewModels = messages
            .OrderBy(m => m.Created)
            .ThenBy(m => m.Id)
            .Select(message => ToMessageViewModel(message, myProfile, peerProfile))
            .ToList();

        return View("Chat", new ImChatViewModel
        {
            CurrentProfile = myProfile,
            PeerProfile = peerProfile,
            Messages = messageViewModels,
            CanWrite = true
        });
    }

    private IQueryable<Message> BuildUserMessagesQuery(ulong profileId)
    {
        return _db.Messages.Where(m =>
            !m.Deleted &&
            m.SenderType == UserModel &&
            m.RecipientType == UserModel &&
            (m.SenderId == profileId || m.RecipientId == profileId));
    }

    private IQueryable<Message> BuildConversationQuery(ulong firstProfileId, ulong secondProfileId)
    {
        return _db.Messages.Where(m =>
            !m.Deleted &&
            m.SenderType == UserModel &&
            m.RecipientType == UserModel &&
            ((m.SenderId == firstProfileId && m.RecipientId == secondProfileId) ||
             (m.SenderId == secondProfileId && m.RecipientId == firstProfileId)));
    }

    private static ImMessageViewModel ToMessageViewModel(Message message, Profile myProfile, Profile peerProfile)
    {
        var isMine = message.SenderId == myProfile.Id;
        var sender = isMine ? myProfile : peerProfile;

        return new ImMessageViewModel
        {
            Id = message.Id,
            SenderId = sender.Id,
            SenderName = $"{sender.FirstName} {sender.LastName}".Trim(),
            SenderUrl = $"/id{sender.Id}",
            SenderAvatarUrl = $"/avatars/{sender.Id}",
            Content = message.Content,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds((long)message.Created).LocalDateTime,
            IsMine = isMine,
            IsUnread = message.Unread
        };
    }
}
