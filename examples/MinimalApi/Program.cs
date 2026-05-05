using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;

// ── Application Setup ────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInertia(options =>
{
    options.RootView = "App";
    options.VersionResolver = () => "1.0.0";
});

builder.Services.AddInertiaHandler<AppInertiaHandler>();

var app = builder.Build();

app.UseInertia();
app.UseRouting();

// ── Routes ───────────────────────────────────────────────────────────────────

app.MapGet("/", (IInertiaService inertia, HttpContext ctx) =>
{
    ctx.SetInertiaResult(inertia.Render("Home/Index"));
});

// Eager props
app.MapGet("/users", (IInertiaService inertia, HttpContext ctx) =>
{
    var users = UserRepository.All();
    ctx.SetInertiaResult(inertia.Render("Users/Index", new { users, total = users.Count }));
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

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
