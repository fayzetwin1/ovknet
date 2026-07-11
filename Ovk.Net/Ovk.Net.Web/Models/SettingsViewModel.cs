namespace Ovk.Net.Web.Models;

public class SettingsViewModel
{
    public string Mode { get; set; } = "main";
    public ulong ProfileId { get; set; }
    public string? Shortcode { get; set; }
    public int AvatarStyle { get; set; }
    public bool ShowRating { get; set; } = true;
    public List<ChandlerSessionViewModel> Sessions { get; set; } = [];
}

public class ChandlerSessionViewModel
{
    public string TokenId { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}
