using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;

// ── Application Setup ────────────────────────────────────────────────────────

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

builder.Services.AddInertia(options =>
{
    options.VersionResolver = () => "1.2.0";
    ConfigureReactAssetShell(options.AssetShell, useViteDevServer);
});
builder.Services.AddInertiaHandler<AppInertiaHandler>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseInertia();

// ── Routes ───────────────────────────────────────────────────────────────────

app.MapGet("/", (IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Home/Index", DemoPages.BuildHomePage(
        greeting: "Launch operations workspace")));
});

app.MapGet("/docs", (IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Docs/[[...page]]", DocsCatalog.Resolve(null)));
});

app.MapGet("/docs/{**page}", (string page, IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Docs/[[...page]]", DocsCatalog.Resolve(page)));
});

// Eager props
app.MapGet("/users", (IInertiaService inertia, HttpContext ctx) =>
{
    var users = UserRepository.All();
    ctx.SetInertiaResult(inertia.Render("Users/Index", new
    {
        users,
        total = users.Count,
        overview = new[]
        {
            new { label = "Active launches", value = "12", detail = "Four specialists covering three regions" },
            new { label = "At-risk accounts", value = "2", detail = "Escalations already assigned" },
            new { label = "Utilization", value = "82%", detail = "Healthy bandwidth for one more launch" },
        },
    }));
});

app.MapGet("/users/create", (IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Users/Create"));
});

// Optional + Deferred + Merge props
app.MapGet("/dashboard", (IInertiaService inertia, HttpContext ctx) =>
{
    var result = inertia.Render("Dashboard/Index", new Dictionary<string, object?>
    {
        ["summary"]        = new
        {
            activeLaunches = 12,
            atRiskAccounts = 2,
            handoffsToday = 5,
            teamUtilization = "82%",
        },
        ["topUsers"]       = inertia.Optional(() => UserRepository.Top(3)),
        ["monthlyChart"]   = inertia.Defer(() => Analytics.MonthlyData(), "charts"),
        ["recentActivity"] = inertia.Merge(ActivityFeed.Latest(page: 1)),
    });
    ctx.SetInertiaResult(result);
}).WithEncryptHistory();

app.MapGet("/me", (IInertiaService inertia, HttpContext ctx) =>
{
    var profile = DemoAuth.BuildUserViewModel(ctx.User);
    ctx.SetInertiaResult(inertia.Render("Account/Show", new
    {
        profile,
        workspaceAccess = new[]
        {
            "Launch approvals",
            "Executive risk review",
            "Escalation routing",
        },
        secureFeatures = new[]
        {
            "ASP.NET Core cookie authentication protects the route before the page renders.",
            "Shared auth.user data stays available on every Inertia response.",
            "Encrypted history keeps sensitive workspace screens out of plain browser history.",
            "Clear history on sign-out resets the secure browsing trail.",
        },
        recentEvents = new[]
        {
            new { id = 1, action = "Approved the Meridian Health launch plan", at = "2 minutes ago" },
            new { id = 2, action = "Reviewed the at-risk launch queue", at = "14 minutes ago" },
            new { id = 3, action = "Opened the protected access profile", at = "Just now" },
        },
    }));
}).RequireAuthorization().WithEncryptHistory();

app.MapPost("/auth/demo-sign-in", async (HttpContext ctx) =>
{
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        DemoAuth.CreatePrincipal());

    ctx.Response.StatusCode = StatusCodes.Status303SeeOther;
    ctx.Response.Headers.Location = "/me";
});

app.MapPost("/auth/demo-sign-out", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.StatusCode = StatusCodes.Status303SeeOther;
    ctx.Response.Headers.Location = "/signed-out";
});

app.MapGet("/signed-out", (IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Home/Index", DemoPages.BuildHomePage(
        greeting: "You have signed out of the workspace",
        statusNote: "Encrypted history was cleared for this page. Use this flow after logout or session expiry.")));
}).WithClearHistory();

// POST — validation error (inline 422, no redirect)
app.MapPost("/users", (CreateUserRequest req, IInertiaService inertia, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(req.Name))
    {
        ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        ctx.SetInertiaResult(inertia.Render("Users/Create", new
        {
            errors = new { name = "Name is required." },
        }));
        return;
    }

    var encodedMessage = Uri.EscapeDataString($"Invitation queued for {req.Name}. The redirect kept the experience inside the Inertia flow.");
    ctx.Response.StatusCode = StatusCodes.Status303SeeOther;
    ctx.Response.Headers.Location = $"/users?success={encodedMessage}";
});

// External redirect — forces full browser navigation (409 + X-Inertia-Location)
app.MapGet("/auth/github", (IInertiaService inertia) =>
    inertia.Location("https://github.com/login/oauth/authorize?client_id=demo"));

app.Run();

static void ConfigureReactAssetShell(InertiaAssetShellOptions assetShell, bool useDevelopmentServer)
{
    assetShell.Enabled = true;
    assetShell.DocumentTitle = "InertiaKit React MinimalApi";
    assetShell.StylesheetHrefs.Add("/build/app.css");
    assetShell.ModuleScriptHrefs.Add("/build/app.js");

    if (!useDevelopmentServer)
    {
        return;
    }

    assetShell.DevelopmentServerUrl = "http://127.0.0.1:5173";
    assetShell.DevelopmentModuleEntrypoints.Add("/src/app.jsx");
}

// ── Shared props handler ──────────────────────────────────────────────────────

sealed class AppInertiaHandler : HandleInertiaRequestsBase
{
    public override string RootView => "App";

    public override string? Version(HttpContext context) => "1.2.0";

    public override void Share(IInertiaShareBuilder shared, HttpContext context)
    {
        shared
            .Add("auth", new
            {
                user = DemoAuth.BuildUserViewModel(context.User),
            })
            .Add("flash", new
            {
                success = context.Request.Query["success"].ToString(),
            })
            .AddOnce("appConfig", new
            {
                name = "Northstar Launch Ops",
                version = "1.2.0",
                environment = "Practical demo",
            });
    }
}

// ── Stub domain types ─────────────────────────────────────────────────────────

record CreateUserRequest(string Name, string Email);

static class UserRepository
{
    public static List<object> All() =>
    [
        new
        {
            id = 1,
            name = "Maya Chen",
            email = "maya.chen@northstar.example",
            role = "Launch Director",
            region = "North America",
            status = "On track",
            activeLaunches = 4,
            focus = "Executive launches",
        },
        new
        {
            id = 2,
            name = "Omar Haddad",
            email = "omar.haddad@northstar.example",
            role = "Implementation Lead",
            region = "EMEA",
            status = "Needs review",
            activeLaunches = 3,
            focus = "Migration planning",
        },
        new
        {
            id = 3,
            name = "Priya Raman",
            email = "priya.raman@northstar.example",
            role = "Technical Architect",
            region = "APAC",
            status = "On track",
            activeLaunches = 2,
            focus = "Integration readiness",
        },
        new
        {
            id = 4,
            name = "Leo Martinez",
            email = "leo.martinez@northstar.example",
            role = "Customer Enablement",
            region = "North America",
            status = "Preparing handoff",
            activeLaunches = 3,
            focus = "Training and change management",
        },
    ];

    public static List<object> Top(int count) => All().Take(count).ToList();
}

static class Analytics
{
    public static object MonthlyData() => new
    {
        labels = new[] { "Jan", "Feb", "Mar", "Apr" },
        values = new[] { 4, 6, 8, 9 },
    };
}

static class ActivityFeed
{
    public static List<object> Latest(int page) =>
    [
        new { id = 1, action = "Meridian Health kickoff deck approved", at = "08:45" },
        new { id = 2, action = "Northwind Foods integration risk escalated", at = "09:10" },
        new { id = 3, action = "Enablement session booked for Solstice Energy", at = "10:30" },
        new { id = 4, action = "Launch command room updated after stakeholder review", at = "11:20" },
    ];
}

static class DemoAuth
{
    public static ClaimsPrincipal CreatePrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Maya Chen"),
            new Claim(ClaimTypes.Email, "maya.chen@northstar.example"),
            new Claim(ClaimTypes.Role, "Launch Director"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static object? BuildUserViewModel(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return null;

        return new
        {
            name = user.Identity?.Name ?? "Maya Chen",
            email = user.FindFirst(ClaimTypes.Email)?.Value ?? "maya.chen@northstar.example",
            role = user.FindFirst(ClaimTypes.Role)?.Value ?? "Launch Director",
        };
    }
}

static class DocsCatalog
{
    private sealed record DocsEntry(string Title, string Summary, string[] Highlights);

    private static readonly Dictionary<string, DocsEntry> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["guides/getting-started"] = new(
            "Launch readiness runbook",
            "A nested runbook article that still resolves through one optional catch-all Inertia page.",
            [
                "Server sends Docs/[[...page]] as one stable component name for the entire runbook branch.",
                "React resolves that name to a single optional catch-all page file.",
                "Nested docs URLs still hydrate through the same mounted app shell.",
            ]),
        ["guides/testing/playwright"] = new(
            "Launch handoff checklist",
            "Shows how the same nested route can hold practical operator guidance without a dedicated page component per article.",
            [
                "Client-side visits send X-Inertia requests.",
                "Initial HTML still embeds the page object for first load hydration.",
                "Deferred props stay available on the rest of the sample routes.",
            ]),
        ["reference/protocol/headers"] = new(
            "Protocol review",
            "Maps nested runbook slugs to practical Inertia protocol concepts without adding a separate page file for each article.",
            [
                "X-Inertia marks JSON visits.",
                "X-Inertia-Version protects against stale assets.",
                "X-Inertia-Except-Once-Props keeps once props cached client-side.",
            ]),
    };

    public static object Resolve(string? rawPage)
    {
        var normalizedPath = rawPage?.Trim('/');
        var segments = string.IsNullOrWhiteSpace(normalizedPath)
            ? Array.Empty<string>()
            : normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var slug = string.Join('/', segments);
        DocsEntry? entry = null;
        var found = segments.Length > 0 && Entries.TryGetValue(slug, out entry);

        entry ??= new DocsEntry(
            "Runbook index",
            "Start at /docs, then open a nested slug like /docs/guides/getting-started to exercise the optional catch-all page resolver end to end.",
            [
                "The same page file handles /docs and deeper paths.",
                "The resolver also understands single-segment and required catch-all patterns.",
                "Unknown slugs still render through the same page component with fallback copy.",
            ]);

        var knownPages = Entries.Keys
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new
            {
                path,
                href = $"/docs/{path}",
            })
            .ToArray();

        return new
        {
            componentPattern = "Docs/[[...page]]",
            slug = segments.Length == 0 ? null : slug,
            segments,
            title = entry.Title,
            summary = entry.Summary,
            highlights = entry.Highlights,
            matchedExistingArticle = found,
            knownPages,
        };
    }
}

static class DemoPages
{
    public static object BuildHomePage(string greeting, string? statusNote = null)
    {
        return new
        {
            greeting,
            statusNote,
            highlights = new[]
            {
                "Queue an invite, follow a redirect, and read the flash message on the team board.",
                "Open insights to watch deferred analytics hydrate after the first paint.",
                "Browse the runbooks branch to see one optional catch-all file handle nested docs.",
                "Sign in as the launch director to open a protected page with encrypted history.",
            },
            overview = new[]
            {
                new { label = "Active launches", value = "12", detail = "Three regions, one shared workspace" },
                new { label = "Stakeholder reviews", value = "5", detail = "Scheduled before today's handoffs" },
                new { label = "Customer sentiment", value = "94%", detail = "Trailing two-week health score" },
            },
            workflow = new[]
            {
                new { title = "Read the runbook", description = "Nested docs stay on one optional catch-all page while route slugs change.", href = "/docs/guides/getting-started" },
                new { title = "Invite a teammate", description = "Validation stays inline, then a redirect lands you back on the team board with flash feedback.", href = "/users/create" },
                new { title = "Review launch health", description = "The dashboard renders immediately, then reloads only the deferred chart payload.", href = "/dashboard" },
                new { title = "Open secure access", description = "ASP.NET Core auth protects the access profile while Inertia preserves the same app experience.", href = "/me" },
            },
            featuredAccounts = new[]
            {
                new { name = "Meridian Health", stage = "Stakeholder rehearsal", nextStep = "Training handoff at 15:30" },
                new { name = "Northwind Foods", stage = "API migration", nextStep = "Architect review at 13:00" },
                new { name = "Solstice Energy", stage = "Adoption planning", nextStep = "Enablement session tomorrow" },
            },
        };
    }
}

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
