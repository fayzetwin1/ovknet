using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ovk.Net.Web.Services;
using Ovk.Net.Web.Models;
using Ovk.Net.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ovk.Net.Web.Components;

public class SidebarViewComponent : ViewComponent
{
    private readonly IChandlerContext _currentUser;
    private readonly OvkDbContext _db;

    public SidebarViewComponent(IChandlerContext currentUser, OvkDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var profile = await _currentUser.GetProfileAsync();
        var unread = profile is null ? 0 : await _db.Messages.CountAsync(message =>
            !message.Deleted && message.RecipientId == profile.Id && message.Unread);
        return View(new SidebarViewModel { Profile = profile, UnreadMessages = unread });
    }
}
