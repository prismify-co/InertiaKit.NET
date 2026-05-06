using Microsoft.AspNetCore.Mvc;
using InertiaKit.Core.Abstractions;
using InertiaKit.AspNetCore;
using Mvc.Models;

namespace Mvc.Controllers;

[Route("dashboard")]
public class DashboardController : Controller
{
    private readonly IInertiaService _inertia;

    public DashboardController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    [HttpGet("")]
    [EncryptHistory]
    public IActionResult Index()
    {
        return _inertia.Render("Dashboard/Index", new Dictionary<string, object?>
        {
            ["summary"]        = new
            {
                activeLaunches = 12,
                atRiskAccounts = 2,
                handoffsToday = 5,
                teamUtilization = "82%",
            },
            ["topUsers"]       = _inertia.Optional(() => UserRepository.Top(3)),
            ["monthlyChart"]   = _inertia.Defer(() => Analytics.MonthlyData(), "charts"),
            ["recentActivity"] = _inertia.Merge(ActivityFeed.Latest(page: 1)),
        });
    }
}
