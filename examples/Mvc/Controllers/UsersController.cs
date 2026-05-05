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
    public IActionResult Store(CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            // Store errors in session for the next GET (session-based error flash)
            TempData["errors"] = System.Text.Json.JsonSerializer.Serialize(
                new { name = "Name is required." });

            // Redirect back to the form (303)
            return RedirectToAction("Create");
        }

        TempData["success"] = "User created!";
        return RedirectToAction("Index");
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
