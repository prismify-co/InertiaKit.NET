using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InertiaKit.Core.Abstractions;
using InertiaKit.AspNetCore;
using Mvc.Models;

namespace Mvc.Controllers;

[Route("me")]
[Authorize]
public class AccountController : Controller
{
    private readonly IInertiaService _inertia;

    public AccountController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    [HttpGet("")]
    [EncryptHistory]
    public IActionResult Show()
    {
        var profile = DemoAuth.BuildUserViewModel(User);
        return _inertia.Render("Account/Show", new
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
        });
    }
}
