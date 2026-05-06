using System.Net;

namespace InertiaKit.AspNetCore;

/// <summary>
/// Builds the asset tags used by the built-in asset shell renderer and Razor-based root views.
/// </summary>
public static class InertiaAssetShellMarkup
{
    public static bool UsesDevelopmentServer(InertiaAssetShellOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return !string.IsNullOrWhiteSpace(options.DevelopmentServerUrl)
            && options.DevelopmentModuleEntrypoints.Count > 0;
    }

    public static string BuildHeadAssetTags(InertiaAssetShellOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (UsesDevelopmentServer(options))
        {
            return BuildModuleScriptTag(CombineUrl(options.DevelopmentServerUrl!, "/@vite/client"));
        }

        return string.Join('\n', options.StylesheetHrefs.Select(BuildStylesheetTag));
    }

    public static string BuildBodyAssetTags(InertiaAssetShellOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (UsesDevelopmentServer(options))
        {
            var parts = new List<string>();

            // @vitejs/plugin-react requires a preamble script to set up Fast Refresh
            // before any JSX module is evaluated. Vite injects this automatically when
            // serving HTML itself; we must do it here since we own the HTML response.
            if (options.DevelopmentModuleEntrypoints.Any(e => e.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)))
            {
                var refreshUrl = WebUtility.HtmlEncode(CombineUrl(options.DevelopmentServerUrl!, "/@react-refresh"));
                parts.Add($$"""
                    <script type="module">
                      import RefreshRuntime from "{{refreshUrl}}"
                      RefreshRuntime.injectIntoGlobalHook(window)
                      window.$RefreshReg$ = () => {}
                      window.$RefreshSig$ = () => (type) => type
                      window.__vite_plugin_react_preamble_installed__ = true
                    </script>
                    """);
            }

            parts.AddRange(options.DevelopmentModuleEntrypoints.Select(entrypoint =>
                BuildModuleScriptTag(CombineUrl(options.DevelopmentServerUrl!, entrypoint))));

            return string.Join('\n', parts);
        }

        return string.Join('\n', options.ModuleScriptHrefs.Select(BuildModuleScriptTag));
    }

    private static string BuildStylesheetTag(string href) =>
        $"<link rel=\"stylesheet\" href=\"{WebUtility.HtmlEncode(href)}\" />";

    private static string BuildModuleScriptTag(string src) =>
        $"<script type=\"module\" src=\"{WebUtility.HtmlEncode(src)}\"></script>";

    private static string CombineUrl(string baseUrl, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri)
            && absoluteUri.Scheme is "http" or "https")
        {
            return absoluteUri.ToString();
        }

        var trimmedBaseUrl = baseUrl.TrimEnd('/');
        var normalizedPath = string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : $"/{path.TrimStart('/')}";

        return $"{trimmedBaseUrl}{normalizedPath}";
    }
}