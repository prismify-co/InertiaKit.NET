using System.Text;
using Microsoft.AspNetCore.Http;

namespace InertiaKit.AspNetCore.Internal;

internal sealed class DefaultHtmlShellInertiaRenderer : IInertiaRenderer
{
    public int Priority => -1000;

    public bool CanRender(InertiaRenderContext context) => true;

    public Task RenderAsync(InertiaRenderContext context)
    {
        context.HttpContext.Response.ContentType = "text/html";
        return context.HttpContext.Response.WriteAsync(
            BuildHtmlShell(context.PageJson, context.SsrHtml, context.SsrHead),
            Encoding.UTF8);
    }

    internal static string BuildHtmlShell(
        string pageJson,
        string? ssrHtml = null,
        IReadOnlyList<string>? ssrHead = null)
    {
        var headTags = ssrHead is { Count: > 0 }
            ? string.Join('\n', ssrHead)
            : string.Empty;

        var appContent = ssrHtml ?? string.Empty;

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8" />
        {{headTags}}
        </head>
        <body>
        <div id="app">{{appContent}}</div>
        <script type="application/json" id="app-data">{{pageJson}}</script>
        </body>
        </html>
        """;
    }
}