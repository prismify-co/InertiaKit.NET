using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InertiaKit.AspNetCore.Internal;

internal sealed class AssetShellInertiaRenderer(IOptions<InertiaOptions> optionsAccessor) : IInertiaRenderer
{
    public int Priority => -50;

    public bool CanRender(InertiaRenderContext context) => optionsAccessor.Value.AssetShell.Enabled;

    public Task RenderAsync(InertiaRenderContext context)
    {
        var options = optionsAccessor.Value.AssetShell;
        var titleTag = string.IsNullOrWhiteSpace(options.DocumentTitle)
            ? string.Empty
            : $"<title>{WebUtility.HtmlEncode(options.DocumentTitle)}</title>";

        var stylesheetTags = string.Join('\n', options.StylesheetHrefs.Select(href =>
            $"<link rel=\"stylesheet\" href=\"{WebUtility.HtmlEncode(href)}\" />"));

        var moduleScriptTags = string.Join('\n', options.ModuleScriptHrefs.Select(src =>
            $"<script type=\"module\" src=\"{WebUtility.HtmlEncode(src)}\"></script>"));

        var configuredHeadTags = options.HeadTags.Count > 0
            ? string.Join('\n', options.HeadTags)
            : string.Empty;

        var ssrHead = context.SsrHead is { Count: > 0 }
            ? string.Join('\n', context.SsrHead)
            : string.Empty;

        var appContent = context.SsrHtml ?? string.Empty;
        var appElementId = WebUtility.HtmlEncode(options.AppElementId);
        var pageDataElementId = WebUtility.HtmlEncode(options.PageDataElementId);
        var htmlLanguage = WebUtility.HtmlEncode(options.HtmlLanguage);

        var html = $$"""
        <!DOCTYPE html>
        <html lang="{{htmlLanguage}}">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            {{titleTag}}
            {{stylesheetTags}}
            {{configuredHeadTags}}
            {{ssrHead}}
        </head>
        <body>
            <div id="{{appElementId}}">{{appContent}}</div>
            <script type="application/json" id="{{pageDataElementId}}">{{context.PageJson}}</script>
            {{moduleScriptTags}}
        </body>
        </html>
        """;

        context.HttpContext.Response.ContentType = "text/html";
        return context.HttpContext.Response.WriteAsync(html, Encoding.UTF8);
    }
}