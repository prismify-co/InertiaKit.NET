namespace InertiaKit.AspNetCore;

/// <summary>
/// Configures the built-in asset-backed HTML shell renderer for non-Inertia requests.
/// Useful for Minimal API or other non-MVC hosts that serve a client bundle directly
/// without using Razor views.
/// </summary>
public sealed class InertiaAssetShellOptions
{
    /// <summary>
    /// Enables the built-in asset shell renderer. When enabled, it takes precedence
    /// over the built-in MVC Razor renderer but not over custom <see cref="IInertiaRenderer"/> implementations.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>The HTML <c>lang</c> attribute. Defaults to <c>en</c>.</summary>
    public string HtmlLanguage { get; set; } = "en";

    /// <summary>Optional document title.</summary>
    public string? DocumentTitle { get; set; }

    /// <summary>The id of the root application element. Defaults to <c>app</c>.</summary>
    public string AppElementId { get; set; } = "app";

    /// <summary>The id of the JSON script element holding the serialized page object. Defaults to <c>app-data</c>.</summary>
    public string PageDataElementId { get; set; } = "app-data";

    /// <summary>Stylesheet URLs to include as <c>&lt;link rel="stylesheet"&gt;</c> tags.</summary>
    public IList<string> StylesheetHrefs { get; } = [];

    /// <summary>Module script URLs to include as <c>&lt;script type="module"&gt;</c> tags.</summary>
    public IList<string> ModuleScriptHrefs { get; } = [];

    /// <summary>
    /// Optional development asset server origin, such as a Vite dev server.
    /// When set together with <see cref="DevelopmentModuleEntrypoints"/>,
    /// the asset shell emits dev-server module scripts instead of the built assets.
    /// </summary>
    public string? DevelopmentServerUrl { get; set; }

    /// <summary>
    /// Module entrypoints served by the development asset server, such as
    /// <c>/src/app.jsx</c> or <c>/src/app.js</c>.
    /// </summary>
    public IList<string> DevelopmentModuleEntrypoints { get; } = [];

    /// <summary>Additional raw head tags to append after generated asset tags.</summary>
    public IList<string> HeadTags { get; } = [];
}