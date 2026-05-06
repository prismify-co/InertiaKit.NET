using Microsoft.AspNetCore.Mvc;
using InertiaKit.AspNetCore;
using InertiaKit.Core;
using Mvc.Models;

namespace Mvc.Controllers;

[Route("users")]
public class UsersController : Controller
{
    private readonly IInertiaService _inertia;

    public UsersController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var users = UserRepository.All();
        return _inertia.Render("Users/Index", new
        {
            users,
            total = users.Count,
            overview = new[]
            {
                new { label = "Active launches", value = "12", detail = "Four specialists covering three regions" },
                new { label = "At-risk accounts", value = "2", detail = "Escalations already assigned" },
                new { label = "Utilization", value = "82%", detail = "Healthy bandwidth for one more launch" },
            },
        });
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return _inertia.Render("Users/Create");
    }

    [HttpPost("")]
    public IActionResult Store([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return _inertia.Render("Users/Create", new
            {
                errors = new { name = "Name is required." },
            });
        }

        var encodedMessage = Uri.EscapeDataString($"Invitation queued for {req.Name}. The redirect kept the experience inside the Inertia flow.");
        return Redirect($"/users?success={encodedMessage}");
    }
}
