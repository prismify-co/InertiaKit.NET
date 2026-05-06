using Microsoft.AspNetCore.Mvc;
using InertiaKit.AspNetCore;
using Mvc.Models;

namespace Mvc.Controllers;

[Route("docs")]
public class DocsController : Controller
{
    private readonly IInertiaService _inertia;

    public DocsController(IInertiaService inertia)
    {
        _inertia = inertia;
    }

    [HttpGet("")]
    [HttpGet("{**page}")]
    public IActionResult Index(string? page)
    {
        var segments = page?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var found = page is not null && DocsCatalog.Entries.ContainsKey(page);

        var knownPages = DocsCatalog.Entries.Keys
            .Select(p => new { path = p, href = $"/docs/{p}" })
            .ToList<object>();

        var entry = found ? DocsCatalog.Entries[page!] : null;

        return _inertia.Render("Docs/[[...page]]", new
        {
            article = DocsCatalog.BuildArticle(),
            breadcrumbs = DocsCatalog.BuildBreadcrumbs(),
            componentPattern = "Docs/[[...page]]",
            slug = page is not null ? $"/docs/{page}" : "/docs",
            segments,
            title = entry?.Title ?? "Launch Playbook",
            summary = entry?.Summary ?? "One optional catch-all file owns the whole runbook branch while the route slug changes underneath it.",
            highlights = entry?.Highlights ?? [],
            matchedExistingArticle = found,
            knownPages,
        });
    }
}
