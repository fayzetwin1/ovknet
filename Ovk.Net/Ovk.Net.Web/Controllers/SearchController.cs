using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;

namespace Ovk.Net.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private const int PerPage = 10;
    private const string ClubModel = "openvk\\Web\\Models\\Entities\\Club";
    private readonly OvkDbContext _db;

    public SearchController(OvkDbContext db)
    {
        _db = db;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Index(
        [FromQuery] string? q,
        [FromQuery] string section = "users",
        [FromQuery] int p = 1,
        [FromQuery] string? city = null,
        [FromQuery] int gender = 3,
        [FromQuery(Name = "is_online")] bool onlineOnly = false)
    {
        section = section == "groups" ? "groups" : "users";
        var query = q?.Trim() ?? string.Empty;
        var page = Math.Max(1, p);
        var model = new SearchViewModel
        {
            Section = section,
            Query = query,
            City = city,
            Gender = gender,
            OnlineOnly = onlineOnly,
            Page = page,
            PerPage = PerPage
        };

        if (section == "groups")
        {
            var groups = _db.Clubs.AsNoTracking();
            if (query.Length > 0)
            {
                groups = groups.Where(x =>
                    x.Name.Contains(query) ||
                    (x.About != null && x.About.Contains(query)) ||
                    (x.Shortcode != null && x.Shortcode.Contains(query)));
            }
            model.Count = await groups.CountAsync();
            model.Groups = await groups.OrderByDescending(x => x.Id)
                .Skip((page - 1) * PerPage).Take(PerPage)
                .Select(x => new SearchClubViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    About = x.About,
                    Verified = x.Verified,
                    Members = _db.Subscriptions.Count(s => s.TargetModel == ClubModel && s.TargetId == x.Id)
                }).ToListAsync();
        }
        else
        {
            var users = _db.Profiles.AsNoTracking().Where(x => x.Deleted == 0);
            if (query.Length > 0)
            {
                users = users.Where(x =>
                    x.FirstName.Contains(query) || x.LastName.Contains(query) ||
                    (x.Pseudo != null && x.Pseudo.Contains(query)) ||
                    (x.Status != null && x.Status.Contains(query)) ||
                    (x.About != null && x.About.Contains(query)));
            }
            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityQuery = city.Trim();
                users = users.Where(x => x.City != null && x.City.Contains(cityQuery));
            }
            if (gender is 0 or 1) users = users.Where(x => x.Sex == (gender == 0));
            var onlineThreshold = (ulong)Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300);
            if (onlineOnly) users = users.Where(x => x.Online >= onlineThreshold);

            model.Count = await users.CountAsync();
            model.Users = await users.OrderByDescending(x => x.Id)
                .Skip((page - 1) * PerPage).Take(PerPage)
                .Select(x => new SearchUserViewModel
                {
                    Id = x.Id,
                    Name = x.FirstName + " " + x.LastName,
                    Status = x.Status,
                    City = x.City,
                    Sex = x.Sex,
                    Verified = x.Verified,
                    Online = x.Online >= onlineThreshold,
                    Since = x.Since
                }).ToListAsync();
        }

        return View(model);
    }
}
