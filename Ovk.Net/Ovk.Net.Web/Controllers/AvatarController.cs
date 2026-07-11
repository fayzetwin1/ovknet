using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers;

public partial class AvatarController : Controller
{
    private const long MaxAvatarBytes = 10 * 1024 * 1024;
    private readonly IAvatarStorage _avatars;
    private readonly IChandlerContext _currentUser;

    public AvatarController(IAvatarStorage avatars, IChandlerContext currentUser)
    {
        _avatars = avatars;
        _currentUser = currentUser;
    }

    [HttpGet("avatars/{profileId:long}")]
    public async Task<IActionResult> Get(ulong profileId, CancellationToken cancellationToken)
    {
        var photo = await _avatars.GetAvatarAsync(profileId, cancellationToken);
        if (photo is null) return Redirect("/assets/packages/static/openvk/img/camera_200.png");
        return Redirect($"/blob_{photo.Hash[..2]}/{photo.Hash}.jpeg");
    }

    [HttpGet("blob_{prefix}/{fileName}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public IActionResult Blob(string prefix, string fileName)
    {
        var match = BlobNameRegex().Match(fileName);
        if (!match.Success || !string.Equals(prefix, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var hash = match.Groups[1].Value + match.Groups[2].Value;
        var path = _avatars.GetPath(hash.ToLowerInvariant());
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, DetectContentType(path));
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("al_avatars")]
    public async Task<IActionResult> Upload(IFormFile? avatar, CancellationToken cancellationToken)
    {
        var profile = await _currentUser.GetProfileAsync();
        if (profile is null) return Unauthorized();
        if (avatar is null || avatar.Length is <= 0 or > MaxAvatarBytes)
            return Redirect($"/id{profile.Id}?avatarError=size");

        await using var stream = avatar.OpenReadStream();
        var signature = new byte[12];
        var read = await stream.ReadAsync(signature, cancellationToken);
        stream.Position = 0;
        if (read < 4 || !IsSupportedImage(signature))
            return Redirect($"/id{profile.Id}?avatarError=format");

        await _avatars.SaveAvatarAsync(profile.Id, stream, cancellationToken);
        return Redirect($"/id{profile.Id}");
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("delete_avatar")]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        var profile = await _currentUser.GetProfileAsync();
        if (profile is null) return Unauthorized();
        await _avatars.DeleteAvatarAsync(profile.Id, cancellationToken);
        return Redirect($"/id{profile.Id}");
    }

    private static bool IsSupportedImage(byte[] b) =>
        b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ||
        b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 ||
        b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38 ||
        b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50;

    private static string DetectContentType(string path)
    {
        Span<byte> b = stackalloc byte[12];
        using var stream = System.IO.File.OpenRead(path);
        var read = stream.Read(b);
        if (read >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (read >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (read >= 4 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return "image/gif";
        if (read >= 12 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
        return "application/octet-stream";
    }

    [GeneratedRegex("^([a-fA-F0-9]{2})([a-fA-F0-9]{126})\\.jpeg$")]
    private static partial Regex BlobNameRegex();
}
