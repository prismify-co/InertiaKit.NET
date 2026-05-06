using Microsoft.AspNetCore.Mvc;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using Mvc.Models;

namespace Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IInertiaService _inertia;

    public HomeController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    public IActionResult Index()
    {
        return _inertia.Render("Home/Index", DemoPages.BuildHomePage(
            greeting: "Launch operations workspace"));
    }
}
