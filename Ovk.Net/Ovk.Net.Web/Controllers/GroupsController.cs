using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Infrastructure.Data;

namespace Ovk.Net.Web.Controllers;

public class GroupsController : Controller
{
    private readonly OvkDbContext _db;
    private const string ClubModel = "openvk\\Web\\Models\\Entities\\Club";

    public GroupsController(OvkDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [Route("groups{id}")]
    public async Task<IActionResult> Index(ulong id)
    {
        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile == null) return NotFound();

        // Get club IDs this user is subscribed to
        var clubIds = await _db.Subscriptions
            .Where(s => s.FollowerId == id && s.TargetModel == ClubModel)
            .Select(s => s.TargetId)
            .ToListAsync();

        var clubs = await _db.Clubs
            .Where(c => clubIds.Contains(c.Id))
            .ToListAsync();

        ViewBag.TargetProfile = profile;
        return View(clubs);
    }

    [Route("club{id}")]
    public async Task<IActionResult> ViewClub(ulong id)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == id);
        if (club == null) return NotFound();

        // Wall uses WallId = -id for groups in OpenVK usually, but let's just fetch by owner or something.
        // In Chandler, Wall IDs for groups are their own generated IDs or negative IDs. Let's skip Wall for clubs in this POC or just use a dummy wall ID.

        return View(club);
    }
}
