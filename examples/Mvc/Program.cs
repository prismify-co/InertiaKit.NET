using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

builder.Services.AddInertia(options =>
{
    options.RootView = "App";
    // Derive from Vite manifest hash in production:
    // options.VersionResolver = () => ViteManifest.Hash(env.WebRootPath);
    options.VersionResolver = () => "1.0.0";
});

builder.Services.AddInertiaHandler<AppInertiaHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseInertia();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ── Shared props handler ──────────────────────────────────────────────────────

sealed class AppInertiaHandler : HandleInertiaRequestsBase
{
    public override string RootView => "App";
    public override string? Version(HttpContext context) => "1.0.0";

    public override void Share(IInertiaShareBuilder shared, HttpContext context)
    {
        shared
            .Add("auth", new
            {
                user = context.User.Identity?.IsAuthenticated == true
                    ? new { name = context.User.Identity.Name }
                    : null,
            })
            .AddOnce("appConfig", new { name = "InertiaKit MVC Demo" });
    }
}

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
