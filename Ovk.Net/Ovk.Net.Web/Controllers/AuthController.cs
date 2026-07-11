using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Models;
using Ovk.Net.Core.Security;
using Ovk.Net.Infrastructure.Data;

namespace Ovk.Net.Web.Controllers;

public class AuthController : Controller
{
    private readonly OvkDbContext _db;

    public AuthController(OvkDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string login, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login && !u.Deleted);
        
        if (user == null || !ChandlerPasswordHasher.VerifyHash(password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Неверный e-mail или пароль.");
            return View();
        }

        await SignInThroughChandlerAsync(user);

        // Redirect to their profile page (e.g., /id1) - assuming we can parse their profile ID if we had one.
        // Profiles table has a string 'user' which is user.Id, and an auto-incrementing ulong 'id'.
        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id.ToString());
        if (profile != null)
        {
            return Redirect($"/id{profile.Id}");
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string firstName, string lastName, string login, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Login == login))
        {
            ModelState.AddModelError("", "Пользователь с таким E-mail уже существует.");
            return View();
        }

        // 1. Create User
        var userId = Guid.NewGuid(); // Chandler uses UUID v4 strings for User.id
        var user = new User
        {
            Id = userId,
            Login = login,
            PasswordHash = ChandlerPasswordHasher.MakeHash(password),
            Deleted = false
        };

        _db.Users.Add(user);

        // 2. Create Profile
        var profile = new Profile
        {
            UserId = userId.ToString(),
            FirstName = firstName,
            LastName = lastName,
            Since = DateTime.UtcNow
        };

        _db.Profiles.Add(profile);

        _db.ChandlerAclRelations.Add(new ChandlerAclRelation
        {
            UserId = userId,
            GroupId = ChandlerClaims.UsersGroupId,
            Priority = 32
        });

        await _db.SaveChangesAsync();

        // 3. Auto-login
        await SignInThroughChandlerAsync(user);

        return Redirect($"/id{profile.Id}");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var tokenId = User.FindFirstValue(ChandlerClaims.Token);
        if (!string.IsNullOrEmpty(tokenId))
        {
            var token = await _db.Tokens.FindAsync(tokenId);
            if (token != null)
            {
                _db.Tokens.Remove(token);
                await _db.SaveChangesAsync();
            }
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInThroughChandlerAsync(User user)
    {
        var tokenId = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .ToLowerInvariant();
        _db.Tokens.Add(new Token
        {
            TokenId = tokenId,
            UserId = user.Id,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Ua = Request.Headers.UserAgent.ToString()
        });
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ChandlerClaims.Token, tokenId)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
