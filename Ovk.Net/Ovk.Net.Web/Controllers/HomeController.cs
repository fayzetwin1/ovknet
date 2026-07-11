using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ovk.Net.Web.Models;
using Ovk.Net.Web.Services;

namespace Ovk.Net.Web.Controllers;

public class HomeController : Controller
{
    private readonly IChandlerContext _currentUser;

    public HomeController(IChandlerContext currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var profile = await _currentUser.GetProfileAsync();
        if (profile is not null) return Redirect($"/id{profile.Id}");
        return View();
    }

    public IActionResult Privacy()
    {
        return Redirect("/privacy");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
