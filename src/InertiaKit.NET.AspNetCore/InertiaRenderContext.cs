using Microsoft.AspNetCore.Http;

namespace InertiaKit.AspNetCore;

/// <summary>
/// Context supplied to <see cref="IInertiaRenderer"/> implementations when rendering
/// the initial HTML document for a non-Inertia request.
/// </summary>
public sealed class InertiaRenderContext
{
    public required HttpContext HttpContext { get; init; }
    public required string RootView { get; init; }
    public required string PageJson { get; init; }
    public string? SsrHtml { get; init; }
    public IReadOnlyList<string>? SsrHead { get; init; }
}