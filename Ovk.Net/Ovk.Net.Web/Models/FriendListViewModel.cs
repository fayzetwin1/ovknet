using Ovk.Net.Core.Models;

namespace Ovk.Net.Web.Models;

public class FriendListViewModel
{
    public Profile TargetProfile { get; set; } = null!;
    public ulong? CurrentProfileId { get; set; }
    public string Mode { get; set; } = "friends";
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 6;
    public int Count { get; set; }
    public List<FriendViewModel> Friends { get; set; } = new();

    public bool IsCurrentUser => CurrentProfileId == TargetProfile.Id;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Count / PerPage));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public string ModeTitle => Mode switch
    {
        "online" => "Друзья онлайн",
        "incoming" => "Входящие заявки",
        "followers" => "Подписчики",
        "outcoming" => "Исходящие заявки",
        _ => "Друзья"
    };

    public string EmptyText => Mode switch
    {
        "online" => "Сейчас никто из друзей не онлайн.",
        "incoming" => "Нет входящих заявок.",
        "followers" => "Пока нет подписчиков.",
        "outcoming" => "Нет исходящих заявок.",
        _ => "Пока нет друзей."
    };

    public string ModeQuery => Mode == "friends" ? string.Empty : $"?act={Mode}";
}
