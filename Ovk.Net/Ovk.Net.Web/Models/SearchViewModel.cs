namespace Ovk.Net.Web.Models;

public class SearchViewModel
{
    public string Section { get; set; } = "users";
    public string Query { get; set; } = string.Empty;
    public string? City { get; set; }
    public int Gender { get; set; } = 3;
    public bool OnlineOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 10;
    public int Count { get; set; }
    public List<SearchUserViewModel> Users { get; set; } = [];
    public List<SearchClubViewModel> Groups { get; set; } = [];
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Count / PerPage));
}

public class SearchUserViewModel
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? City { get; set; }
    public bool Sex { get; set; }
    public bool Verified { get; set; }
    public bool Online { get; set; }
    public DateTime Since { get; set; }
}

public class SearchClubViewModel
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? About { get; set; }
    public bool Verified { get; set; }
    public int Members { get; set; }
}
