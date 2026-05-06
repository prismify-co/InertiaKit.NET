using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core.Props;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.Controllers;

[Route("[controller]")]
public class UsersController(IInertiaService inertia) : Controller
{
    // GET /users — renders Users/Index with partial-reload support
    [HttpGet(""), HttpGet("index")]
    public IActionResult Index()
    {
        HttpContext.SetInertiaResult(inertia.Render("Users/Index", new Dictionary<string, object?>
        {
            // Eager: always included
            ["users"] = UserRepository.All(),

            // Optional: only evaluated when partial reload requests it
            ["permissions"] = inertia.Optional(() => new[] { "users.read", "users.write" }),

            // Always: included even during partial reloads (e.g. for validation errors)
            ["errors"] = inertia.Always(new Dictionary<string, string>()),
        }));
        return new EmptyResult();
    }

    // GET /users/create
    [HttpGet("create")]
    public IActionResult Create()
    {
        HttpContext.SetInertiaResult(inertia.Render("Users/Create"));
        return new EmptyResult();
    }

    // POST /users — validation + PRG
    [HttpPost("")]
    public async Task<IActionResult> Store()
    {
        var req = await BindCreateUserRequest();

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            HttpContext.FlashErrors(new Dictionary<string, string>
            {
                ["name"] = "Name is required.",
            });

            // Redirect back to the form (303)
            return RedirectToAction("Create");
        }

        return RedirectToAction("Index", new { success = "User created!" });
    }

    // GET /users/dashboard — deferred + merge props
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        HttpContext.SetInertiaResult(inertia.Render("Users/Dashboard", new Dictionary<string, object?>
        {
            ["summary"] = new { total = UserRepository.All().Count },
            ["recentUsers"] = inertia.Defer(() => UserRepository.Recent(), "sidebar"),
            ["feed"] = inertia.Merge(ActivityFeed.Page(1)),
        }));
        return new EmptyResult();
    }

    private async Task<CreateUserRequest> BindCreateUserRequest()
    {
        if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            var body = await Request.ReadFromJsonAsync<CreateUserRequest>();
            return body ?? new CreateUserRequest(string.Empty, string.Empty);
        }

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            return new CreateUserRequest(
                form["Name"].ToString(),
                form["Email"].ToString());
        }

        return new CreateUserRequest(string.Empty, string.Empty);
    }
}

// ── Stub domain ───────────────────────────────────────────────────────────────

public record CreateUserRequest(string Name, string Email);

static class UserRepository
{
    public static List<object> All() =>
    [
        new { id = 1, name = "Alice", email = "alice@example.com" },
        new { id = 2, name = "Bob",   email = "bob@example.com" },
    ];

    public static List<object> Recent() => All().TakeLast(5).ToList();
}

static class ActivityFeed
{
    public static List<object> Page(int p) =>
    [
        new { id = 1, action = "Signed up" },
        new { id = 2, action = "Updated profile" },
    ];
}
