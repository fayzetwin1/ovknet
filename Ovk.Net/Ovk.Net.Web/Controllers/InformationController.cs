using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;

namespace Ovk.Net.Web.Controllers;

public class InformationController : Controller
{
    private readonly OvkDbContext _db;

    public InformationController(OvkDbContext db)
    {
        _db = db;
    }

    [HttpGet("about")]
    public async Task<IActionResult> About()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var onlineThreshold = (ulong)Math.Max(0, now - 300);
        var activeThreshold = (ulong)Math.Max(0, now - 30 * 24 * 60 * 60);
        return View(new AboutViewModel
        {
            Users = await _db.Profiles.CountAsync(x => x.Deleted == 0),
            OnlineUsers = await _db.Profiles.CountAsync(x => x.Deleted == 0 && x.Online >= onlineThreshold),
            ActiveUsers = await _db.Profiles.CountAsync(x => x.Deleted == 0 && x.Online >= activeThreshold),
            Groups = await _db.Clubs.CountAsync(),
            Posts = await _db.Posts.CountAsync(x => !x.Deleted)
        });
    }

    [HttpGet("terms")]
    public IActionResult Terms() => View();

    [HttpGet("privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("support")]
    public IActionResult Help() => View();

    [HttpGet("about:openvk")]
    [HttpGet("about/openvk")]
    public IActionResult Version()
    {
        ViewData["Framework"] = RuntimeInformation.FrameworkDescription;
        ViewData["Runtime"] = RuntimeInformation.RuntimeIdentifier;
        return View();
    }

    [HttpGet("robots.txt")]
    public ContentResult Robots() => Content("""
        User-agent: *
        Disallow: /settings
        Disallow: /edit
        Disallow: /im
        Disallow: /api
        Disallow: /Auth
        Disallow: /*?*hash=
        """, "text/plain");
}
