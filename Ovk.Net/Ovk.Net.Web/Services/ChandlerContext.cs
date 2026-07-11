using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Models;
using Ovk.Net.Infrastructure.Data;

namespace Ovk.Net.Web.Services;

public class ChandlerContext : IChandlerContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OvkDbContext _db;
    private bool _chandlerUserResolved;
    private bool _profileResolved;
    private User? _chandlerUser;
    private Profile? _profile;

    public ChandlerContext(IHttpContextAccessor httpContextAccessor, OvkDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public bool IsAuthenticated => HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? ChandlerUserId => HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string IpAddress => HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

    public string UserAgent => HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;

    public async Task<User?> GetChandlerUserAsync()
    {
        if (_chandlerUserResolved)
        {
            return _chandlerUser;
        }

        _chandlerUserResolved = true;
        var userId = ChandlerUserId;
        if (!Guid.TryParse(userId, out var id))
        {
            return null;
        }

        _chandlerUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.Deleted);
        return _chandlerUser;
    }

    public async Task<Profile?> GetProfileAsync()
    {
        if (_profileResolved)
        {
            return _profile;
        }

        _profileResolved = true;
        var userId = ChandlerUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        _profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId && p.Deleted == 0);
        return _profile;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;
}
