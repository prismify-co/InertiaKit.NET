using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using InertiaKit.Core.Abstractions;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.AspNetCore;
using Mvc.Models;

namespace Mvc.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly IInertiaService _inertia;

    public AuthController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    [HttpPost("demo-sign-in")]
    public async Task<IActionResult> DemoSignIn()
    {
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            DemoAuth.CreatePrincipal());

        Response.StatusCode = StatusCodes.Status303SeeOther;
        Response.Headers.Location = "/me";
        return new EmptyResult();
    }

    [HttpPost("demo-sign-out")]
    public async Task<IActionResult> DemoSignOut()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.StatusCode = StatusCodes.Status303SeeOther;
        Response.Headers.Location = "/signed-out";
        return new EmptyResult();
    }

    [HttpGet("github")]
    public IActionResult Github()
    {
        return _inertia.Location("https://github.com/login/oauth/authorize?client_id=demo");
    }
}

[Route("signed-out")]
public class SignedOutController : Controller
{
    private readonly IInertiaService _inertia;

    public SignedOutController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    [HttpGet("")]
    [ClearHistory]
    public IActionResult Index()
    {
        return _inertia.Render("Home/Index", DemoPages.BuildHomePage(
            greeting: "You have signed out of the workspace",
            statusNote: "Encrypted history was cleared for this page. Use this flow after logout or session expiry."));
    }
}
