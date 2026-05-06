using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;

// ── Application Setup ────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInertia(options =>
{
    options.VersionResolver = () => "1.0.0";
    options.AssetShell.Enabled = true;
    options.AssetShell.DocumentTitle = "InertiaKit React MinimalApi";
    options.AssetShell.StylesheetHrefs.Add("/build/app.css");
    options.AssetShell.ModuleScriptHrefs.Add("/build/app.js");
});
builder.Services.AddInertiaHandler<AppInertiaHandler>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseInertia();

// ── Routes ───────────────────────────────────────────────────────────────────

app.MapGet("/", (IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Home/Index", new
    {
        greeting = "React + Minimal API, wired through a real Inertia client",
        highlights = new[]
        {
            "Client-side page navigation",
            "Inline validation on mutation",
            "Deferred dashboard data",
            "Optional catch-all docs routes",
        },
    }));
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
    ctx.SetInertiaResult(inertia.Render("Users/Index", new { users, total = users.Count }));
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
        ["summary"]        = new { users = 120, revenue = 45_000 },
        ["topUsers"]       = inertia.Optional(() => UserRepository.Top(10)),
        ["monthlyChart"]   = inertia.Defer(() => Analytics.MonthlyData(), "charts"),
        ["recentActivity"] = inertia.Merge(ActivityFeed.Latest(page: 1)),
    });
    ctx.SetInertiaResult(result);
});

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
    ctx.Response.StatusCode = StatusCodes.Status303SeeOther;
    ctx.Response.Headers.Location = "/users";
});

// External redirect — forces full browser navigation (409 + X-Inertia-Location)
app.MapGet("/auth/github", (IInertiaService inertia) =>
    inertia.Location("https://github.com/login/oauth/authorize?client_id=demo"));

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
            .Add("flash", new
            {
                success = context.Request.Query["success"].ToString(),
            })
            .AddOnce("appConfig", new { name = "InertiaKit Demo", version = "1.0" });
    }
}

// ── Stub domain types ─────────────────────────────────────────────────────────

record CreateUserRequest(string Name, string Email);

static class UserRepository
{
    public static List<object> All() =>
    [
        new { id = 1, name = "Alice", email = "alice@example.com" },
        new { id = 2, name = "Bob",   email = "bob@example.com" },
    ];

    public static List<object> Top(int count) => All().Take(count).ToList();
}

static class Analytics
{
    public static object MonthlyData() => new
    {
        labels = new[] { "Jan", "Feb", "Mar" },
        values = new[] { 100, 120, 140 },
    };
}

static class ActivityFeed
{
    public static List<object> Latest(int page) =>
    [
        new { id = 1, action = "User created",   at = "2025-05-01" },
        new { id = 2, action = "Post published", at = "2025-05-02" },
    ];
}

static class DocsCatalog
{
    private sealed record DocsEntry(string Title, string Summary, string[] Highlights);

    private static readonly Dictionary<string, DocsEntry> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["guides/getting-started"] = new(
            "Getting started",
            "Shows how an optional catch-all component name can back multiple nested documentation routes.",
            [
                "Server sends Docs/[[...page]] as the Inertia component name.",
                "React resolves that name to a single optional catch-all page file.",
                "Nested docs URLs still hydrate through the same mounted app shell.",
            ]),
        ["guides/testing/playwright"] = new(
            "Playwright testing",
            "Uses the same browser suite approach as the repo's React and Vue examples.",
            [
                "Client-side visits send X-Inertia requests.",
                "Initial HTML still embeds the page object for first load hydration.",
                "Deferred props stay available on the rest of the sample routes.",
            ]),
        ["reference/protocol/headers"] = new(
            "Protocol headers",
            "Maps nested docs slugs to practical Inertia protocol concepts without adding a separate page file for each article.",
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
            "Documentation index",
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

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
