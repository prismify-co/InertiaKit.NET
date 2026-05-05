using InertiaKit.FastEndpoints;
namespace FastEndpointsExample.Endpoints;

/// <summary>
/// GET /users — demonstrates eager, optional, and deferred props
/// from a FastEndpoints handler.
/// </summary>
public class UsersEndpoint : InertiaEndpoint
{
    public override void Configure()
    {
        Get("/users");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        RenderAsync("Users/Index", new Dictionary<string, object?>
        {
            // Eager: resolved on every request
            ["users"] = UserRepository.All(),

            // Optional: only evaluated when the client requests it via partial reload
            ["permissions"] = Inertia.Optional(() => new[] { "users.read", "users.write" }),

            // Deferred: loaded after initial render by the client
            ["stats"] = Inertia.Defer(() => UserRepository.Stats(), "default"),

            // Once: evaluated once, cached client-side thereafter
            ["countries"] = Inertia.Once(() => CountryRepository.All()),
        }, ct);
}

/// <summary>POST /users with typed request body.</summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserEndpoint : InertiaEndpoint<CreateUserRequest>
{
    public override void Configure()
    {
        Post("/users");
        AllowAnonymous();
    }

    public override Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return RenderAsync("Users/Create", new
            {
                errors = new { name = "Name is required." },
            }, ct);
        }

        // PRG: redirect after successful creation
        return SeeOtherAsync("/users", ct);
    }
}

/// <summary>
/// GET /users/dashboard — merge props for infinite scroll pagination.
/// </summary>
public class UsersDashboardEndpoint : InertiaEndpoint
{
    public override void Configure()
    {
        Get("/users/dashboard");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        RenderAsync("Users/Dashboard", new Dictionary<string, object?>
        {
            ["summary"]    = new { total = UserRepository.All().Count },
            ["activities"] = Inertia.Merge(ActivityFeed.Page(1)).MatchOn("id"),
        }, ct);
}

// ── Stub domain ───────────────────────────────────────────────────────────────

static class UserRepository
{
    public static List<object> All() =>
    [
        new { id = 1, name = "Alice", email = "alice@example.com" },
        new { id = 2, name = "Bob",   email = "bob@example.com" },
    ];

    public static object Stats() => new { total = 2, activeToday = 1 };
}

static class CountryRepository
{
    public static List<object> All() =>
    [
        new { code = "US", name = "United States" },
        new { code = "CA", name = "Canada" },
    ];
}

static class ActivityFeed
{
    public static List<object> Page(int p) =>
    [
        new { id = 1, action = "Signed up",       at = "2025-05-01" },
        new { id = 2, action = "Updated profile", at = "2025-05-02" },
    ];
}
