using System.Diagnostics;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.Controllers;

public class HomeController(IInertiaService inertia) : Controller
{
    public IActionResult Index()
    {
        HttpContext.SetInertiaResult(inertia.Render("Home/Index", new
        {
            greeting = "Welcome to InertiaKit + MVC",
        }));
        return new EmptyResult();
    }

    public IActionResult Privacy()
    {
        HttpContext.SetInertiaResult(inertia.Render("Home/Privacy"));
        return new EmptyResult();
    }
}
