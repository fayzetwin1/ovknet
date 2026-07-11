using Ovk.Net.Core.Models;

namespace Ovk.Net.Web.Services;

public interface IChandlerContext
{
    bool IsAuthenticated { get; }
    string? ChandlerUserId { get; }
    string IpAddress { get; }
    string UserAgent { get; }

    Task<User?> GetChandlerUserAsync();
    Task<Profile?> GetProfileAsync();
}
