using System.Diagnostics;
using Inertia.NET.AspNetCore;
using Inertia.NET.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.Controllers;

public class HomeController(IInertiaService inertia) : Controller
{
    public IActionResult Index()
    {
        HttpContext.SetInertiaResult(inertia.Render("Home/Index", new
        {
            greeting = "Welcome to Inertia.NET + MVC",
        }));
        return new EmptyResult();
    }

    public IActionResult Privacy()
    {
        HttpContext.SetInertiaResult(inertia.Render("Home/Privacy"));
        return new EmptyResult();
    }
}
