using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Ovk.Net.Infrastructure.Data;
using Ovk.Net.Web.Services;
using Ovk.Net.Core.Security;

var builder = WebApplication.CreateBuilder(args);

// The Windows EventLog provider can turn harmless diagnostics into request
// failures for non-elevated development accounts. Console/debug are sufficient here.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=openvk;User=root;Password=;";

builder.Services.AddDbContext<OvkDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var keysPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Ovk.Net");

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.Cookie.Name = "Ovk.Net.Auth";
        options.Events.OnValidatePrincipal = async context =>
        {
            var token = context.Principal?.FindFirst(ChandlerClaims.Token)?.Value;
            var userIdText = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(token) || !Guid.TryParse(userIdText, out var userId))
            {
                context.RejectPrincipal();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<OvkDbContext>();
            var valid = await db.Tokens.AsNoTracking()
                .AnyAsync(x => x.TokenId == token && x.UserId == userId);
            if (!valid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChandlerContext, ChandlerContext>();
builder.Services.AddScoped<IAvatarStorage, AvatarStorage>();
builder.Services.AddScoped<LegacySchemaMigrator>();

var app = builder.Build();

// Create DB if it does not exist. Do not drop the legacy OpenVK database on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OvkDbContext>();
    db.Database.EnsureCreated();
    var migrator = scope.ServiceProvider.GetRequiredService<LegacySchemaMigrator>();
    await migrator.ApplyAsync();
}

if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
{
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
