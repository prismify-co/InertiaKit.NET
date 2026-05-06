using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Mvc.Models;

public record CreateUserRequest(string Name, string Email);

public static class UserRepository
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
            name = "Jordan Ellis",
            email = "jordan.ellis@northstar.example",
            role = "Implementation Lead",
            region = "EMEA",
            status = "Needs review",
            activeLaunches = 3,
            focus = "Migration planning",
        },
        new
        {
            id = 3,
            name = "Priya Nair",
            email = "priya.nair@northstar.example",
            role = "Technical Architect",
            region = "APAC",
            status = "On track",
            activeLaunches = 2,
            focus = "Integration readiness",
        },
        new
        {
            id = 4,
            name = "Sam Torres",
            email = "sam.torres@northstar.example",
            role = "Customer Enablement",
            region = "North America",
            status = "Preparing handoff",
            activeLaunches = 3,
            focus = "Training and change management",
        },
    ];

    public static List<object> Top(int count) => All().Take(count).ToList();
}

public static class Analytics
{
    public static object MonthlyData() => new
    {
        labels = new[] { "Jan", "Feb", "Mar", "Apr" },
        values = new[] { 4, 6, 8, 9 },
    };
}

public static class ActivityFeed
{
    public static List<object> Latest(int page) =>
    [
        new { id = 1, action = "Meridian Health kickoff deck approved", at = "08:45" },
        new { id = 2, action = "Northwind Foods integration risk escalated", at = "09:10" },
        new { id = 3, action = "Enablement session booked for Solstice Energy", at = "10:30" },
        new { id = 4, action = "Launch command room updated after stakeholder review", at = "11:20" },
    ];
}

public static class DemoAuth
{
    public static ClaimsPrincipal CreatePrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Maya Chen"),
            new Claim(ClaimTypes.Email, "maya.chen@northstar.example"),
            new Claim(ClaimTypes.Role, "Launch Director")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static object? BuildUserViewModel(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return new
        {
            name = user.Identity.Name ?? "guest",
            email = user.FindFirst(ClaimTypes.Email)?.Value ?? "guest",
            role = user.FindFirst(ClaimTypes.Role)?.Value ?? "guest"
        };
    }
}

public static class DemoPages
{
    public static object BuildHomePage(string greeting, string? statusNote = null)
    {
        return new
        {
            greeting,
            statusNote,
            systemSpecs = new[]
            {
                new { name = "Framework", value = "ASP.NET Core 9.0" },
                new { name = "Frontend", value = "Vue 3" },
                new { name = "Protocol", value = "Inertia v1.2.0" },
                new { name = "Server", value = "Kestrel" },
            },
        };
    }
}

public static class DocsCatalog
{
    public record DocsEntry(string Title, string Summary, string[] Highlights);

    public static readonly Dictionary<string, DocsEntry> Entries = new()
    {
        ["guides/getting-started"] = new(
            "Launch readiness runbook",
            "A step-by-step guide to preparing your first launch.",
            [
                "One optional catch-all file owns the whole runbook branch",
                "The route slug changes underneath it without a full page reload",
                "Shared props keep the workspace identity available on every visit",
            ]
        ),
        ["guides/deployment"] = new(
            "Deployment runbook",
            "How to deploy your launch to production.",
            [
                "Server-side routing maps to a single Inertia component",
                "The catch-all pattern handles nested paths gracefully",
            ]
        ),
        ["reference/api"] = new(
            "API reference",
            "Complete API documentation for the launch platform.",
            [
                "Every route resolves through the same Inertia adapter",
                "Props are serialized once and cached on the client",
            ]
        ),
    };

    public static object BuildArticle()
    {
        return new
        {
            title = "Launch Playbook",
            subtitle = "Enterprise rollout standard operating procedures",
            content = @"
<p>This playbook outlines the critical path for managing an enterprise rollout from contract sign to active utilization.</p>
<h4>Phase 1: Discovery & Audit</h4>
<ul>
<li>Identify executive sponsors and functional leads.</li>
<li>Audit existing shadow IT and unstructured datasets.</li>
<li>Establish data residency boundary requirements.</li>
</ul>
<h4>Phase 2: Configuration & Testing</h4>
<ul>
<li>Provision secure tenant spaces.</li>
<li>Deploy custom identity provider integrations.</li>
<li>Run simulated load and security validations.</li>
</ul>
",
            toc = new[] { "Phase 1: Discovery & Audit", "Phase 2: Configuration & Testing" }
        };
    }

    public static object[] BuildBreadcrumbs()
    {
        return
        [
            new { title = "Runbooks", url = "/docs" },
            new { title = "Enterprise", url = "/docs/enterprise" },
            new { title = "Launch Playbook", url = "/docs/enterprise/launch-playbook" },
        ];
    }

    public static object Resolve(string? page)
    {
        return new
        {
            article = BuildArticle(),
            breadcrumbs = BuildBreadcrumbs(),
        };
    }
}
