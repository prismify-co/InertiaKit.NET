using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;
using Mvc.Models;

var builder = WebApplication.CreateBuilder(args);
var useViteDevServer = builder.Configuration.GetValue<bool>("INERTIA_USE_VITE_DEV_SERVER");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddInertiaAntiforgery();

builder.Services.AddInertia(options =>
{
    options.RootView = "App";
    options.VersionResolver = () => "1.2.0";
    ConfigureVueAssetShell(options.AssetShell, useViteDevServer);
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
app.UseAuthentication();
app.UseAuthorization();
app.UseInertia();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void ConfigureVueAssetShell(InertiaAssetShellOptions assetShell, bool useDevelopmentServer)
{
    assetShell.DocumentTitle = "InertiaKit Vue MVC";
    assetShell.StylesheetHrefs.Add("/build/app.css");
    assetShell.ModuleScriptHrefs.Add("/build/app.js");

    if (!useDevelopmentServer)
    {
        return;
    }

    assetShell.DevelopmentServerUrl = "http://127.0.0.1:5174";
    assetShell.DevelopmentModuleEntrypoints.Add("/src/app.js");
}

// ── Shared props handler ──────────────────────────────────────────────────────

sealed class AppInertiaHandler : HandleInertiaRequestsBase
{
    public override string RootView => "App";
    public override string? Version(HttpContext context) => "1.2.0";

    public override void Share(IInertiaShareBuilder shared, HttpContext context)
    {
        context.SetXsrfTokenCookie();

        shared
            .AddCsrfToken(context)
            .Add("auth", new
            {
                user = DemoAuth.BuildUserViewModel(context.User),
            })
            .Add("flash", new
            {
                success = context.Request.Query["success"].ToString(),
            })
            .AddOnce("appConfig", new { name = "Northstar Launch Ops", version = "1.2.0" });
    }
}

public partial class Program { }
