namespace InertiaKit.AspNetCore;

/// <summary>
/// Renders the initial HTML document for non-Inertia requests.
/// Implementations can render via Razor, raw HTML, another templating system,
/// or any other host-specific mechanism.
/// </summary>
public interface IInertiaRenderer
{
    /// <summary>
    /// Higher values take precedence when multiple renderers can handle the same request.
    /// Custom renderers can usually rely on the default value.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Returns <c>true</c> when this renderer can render the supplied context.
    /// </summary>
    bool CanRender(InertiaRenderContext context);

    /// <summary>
    /// Writes the HTML response for the supplied Inertia page.
    /// </summary>
    Task RenderAsync(InertiaRenderContext context);
}