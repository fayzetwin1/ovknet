using Ovk.Net.Core.Models;

namespace Ovk.Net.Web.Models;

public class ImIndexViewModel
{
    public Profile CurrentProfile { get; set; } = null!;
    public List<ImDialogViewModel> Dialogs { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 10;
    public int Count { get; set; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Count / PerPage));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class ImDialogViewModel
{
    public Profile PeerProfile { get; set; } = null!;
    public ImMessageViewModel LastMessage { get; set; } = null!;
    public int UnreadCount { get; set; }

    public ulong PeerId => PeerProfile.Id;
    public string PeerName => $"{PeerProfile.FirstName} {PeerProfile.LastName}".Trim();
    public string PeerUrl => $"/id{PeerProfile.Id}";
    public string PeerAvatarUrl => $"/avatars/{PeerProfile.Id}";
    public bool IsOnline => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)PeerProfile.Online < 300;
}

public class ImChatViewModel
{
    public Profile CurrentProfile { get; set; } = null!;
    public Profile PeerProfile { get; set; } = null!;
    public List<ImMessageViewModel> Messages { get; set; } = new();
    public bool CanWrite { get; set; } = true;

    public string PeerName => $"{PeerProfile.FirstName} {PeerProfile.LastName}".Trim();
    public string PeerUrl => $"/id{PeerProfile.Id}";
    public bool PeerIsOnline => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)PeerProfile.Online < 300;
    public string PeerLastSeenText => PeerIsOnline
        ? "онлайн"
        : $"был(а) онлайн {FormatMessageTime(PeerProfile.Online)}";

    private static string FormatMessageTime(ulong unixTime)
    {
        if (unixTime == 0)
        {
            return "давно";
        }

        var date = DateTimeOffset.FromUnixTimeSeconds((long)unixTime).LocalDateTime;
        return date.Date == DateTime.Today ? date.ToString("HH:mm") : date.ToString("dd.MM.yy");
    }
}

public class ImMessageViewModel
{
    public ulong Id { get; set; }
    public ulong SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderUrl { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = "/assets/packages/static/openvk/img/camera_200.png";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
    public bool IsUnread { get; set; }

    public string SentText => CreatedAt.Date == DateTime.Today
        ? CreatedAt.ToString("HH:mm:ss")
        : CreatedAt.ToString("dd.MM.yy");
}
