using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Security;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Models;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers;

[Authorize]
public partial class SettingsController : Controller
{
    private static readonly HashSet<string> ReservedShortcodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "edit", "settings", "im", "friends", "groups", "search", "support",
        "about", "terms", "auth", "api", "assets", "css", "js"
    };
    private readonly OvkDbContext _db;
    private readonly IChandlerContext _currentUser;

    public SettingsController(OvkDbContext db, IChandlerContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> Index([FromQuery] string act = "main")
    {
        var profile = await _currentUser.GetProfileAsync();
        var user = await _currentUser.GetChandlerUserAsync();
        if (profile is null || user is null) return Unauthorized();

        var mode = act is "security" or "interface" ? act : "main";
        var currentToken = User.FindFirstValue(ChandlerClaims.Token);
        var sessions = mode == "security"
            ? await _db.Tokens.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync()
            : [];

        return View(new SettingsViewModel
        {
            Mode = mode,
            ProfileId = profile.Id,
            Shortcode = profile.Shortcode,
            AvatarStyle = profile.StyleAvatar ?? 0,
            ShowRating = profile.ShowRating ?? true,
            Sessions = sessions.Select(x => new ChandlerSessionViewModel
            {
                TokenId = x.TokenId,
                Ip = x.Ip,
                UserAgent = x.Ua,
                IsCurrent = x.TokenId == currentToken
            }).ToList()
        });
    }

    [HttpPost("settings/general")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> General([FromForm] string? shortcode)
    {
        var current = await _currentUser.GetProfileAsync();
        if (current is null) return Unauthorized();

        var normalized = string.IsNullOrWhiteSpace(shortcode) ? null : shortcode.Trim().ToLowerInvariant();
        if (normalized is not null && (!ShortcodeRegex().IsMatch(normalized) || ReservedShortcodes.Contains(normalized)))
        {
            TempData["SettingsError"] = "Адрес должен содержать от 3 до 32 латинских букв, цифр или знаков подчёркивания.";
            return Redirect("/settings");
        }
        if (normalized is not null && await _db.Profiles.AnyAsync(x => x.Id != current.Id && x.Shortcode == normalized))
        {
            TempData["SettingsError"] = "Этот адрес уже занят.";
            return Redirect("/settings");
        }

        var profile = await _db.Profiles.FindAsync(current.Id);
        if (profile is null) return NotFound();
        profile.Shortcode = normalized;
        await _db.SaveChangesAsync();
        TempData["SettingsSaved"] = true;
        return Redirect("/settings");
    }

    [HttpPost("settings/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Password(
        [FromForm] string oldPassword,
        [FromForm] string newPassword,
        [FromForm] string repeatPassword)
    {
        var user = await _currentUser.GetChandlerUserAsync();
        if (user is null) return Unauthorized();
        if (!ChandlerPasswordHasher.VerifyHash(oldPassword, user.PasswordHash))
            return PasswordError("Текущий пароль указан неверно.");
        if (newPassword.Length < 8)
            return PasswordError("Новый пароль должен содержать не менее 8 символов.");
        if (newPassword != repeatPassword)
            return PasswordError("Новые пароли не совпадают.");

        var tracked = await _db.Users.FindAsync(user.Id);
        if (tracked is null) return NotFound();
        tracked.PasswordHash = ChandlerPasswordHasher.MakeHash(newPassword);
        await _db.SaveChangesAsync();
        TempData["SettingsSaved"] = true;
        return Redirect("/settings?act=security");
    }

    [HttpPost("settings/interface")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Interface([FromForm] int avatarStyle, [FromForm] bool showRating)
    {
        var current = await _currentUser.GetProfileAsync();
        if (current is null) return Unauthorized();
        var profile = await _db.Profiles.FindAsync(current.Id);
        if (profile is null) return NotFound();
        profile.StyleAvatar = Math.Clamp(avatarStyle, 0, 2);
        profile.ShowRating = showRating;
        await _db.SaveChangesAsync();
        TempData["SettingsSaved"] = true;
        return Redirect("/settings?act=interface");
    }

    [HttpPost("settings/sessions/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession([FromForm] string tokenId)
    {
        var user = await _currentUser.GetChandlerUserAsync();
        if (user is null) return Unauthorized();
        var token = await _db.Tokens.FirstOrDefaultAsync(x => x.TokenId == tokenId && x.UserId == user.Id);
        if (token is not null)
        {
            _db.Tokens.Remove(token);
            await _db.SaveChangesAsync();
        }
        if (tokenId == User.FindFirstValue(ChandlerClaims.Token)) return Redirect("/Auth/Logout");
        TempData["SettingsSaved"] = true;
        return Redirect("/settings?act=security");
    }

    private IActionResult PasswordError(string message)
    {
        TempData["SettingsError"] = message;
        return Redirect("/settings?act=security");
    }

    [GeneratedRegex("^[a-z0-9_]{3,32}$")]
    private static partial Regex ShortcodeRegex();
}
