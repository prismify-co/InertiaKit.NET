namespace InertiaKit.AspNetCore;

public sealed class InertiaOptions
{
    /// <summary>
    /// Logical root document name used by <see cref="IInertiaRenderer"/> implementations.
    /// The built-in MVC renderer treats this as the root Razor view name.
    /// </summary>
    public string RootView { get; set; } = "App";

    /// <summary>
    /// Configures the built-in asset-backed HTML shell renderer for non-Inertia requests.
    /// Enable this for hosts that serve a client bundle directly and do not want to use Razor.
    /// </summary>
    public InertiaAssetShellOptions AssetShell { get; } = new();

    /// <summary>
    /// Configures response-level browser history behavior.
    /// </summary>
    public InertiaHistoryOptions History { get; } = new();

    /// <summary>
    /// Returns the current asset version string. Change this whenever assets
    /// are redeployed. Return null to disable version checking.
    /// </summary>
    public Func<string?> VersionResolver { get; set; } = () => null;

    /// <summary>Whether to return all validation errors per field or just the first.</summary>
    public bool ReturnAllErrors { get; set; } = false;

    /// <summary>
    /// Full URL of the SSR gateway endpoint, including the path.
    /// Example: <c>http://localhost:13714/render</c>.
    /// The gateway receives a POST with the serialised page object and returns
    /// <c>{ "html": "...", "head": ["..."] }</c>.
    /// Null (default) disables SSR.
    /// </summary>
    public string? SsrUrl { get; set; } = null;

    /// <summary>Route prefixes that skip SSR even when <see cref="SsrUrl"/> is set.</summary>
    public IList<string> SsrExcludedPrefixes { get; set; } = [];

    /// <summary>
    /// Maximum byte length of the serialised JSON written to session for flash data
    /// or validation errors. Requests that would exceed this limit are silently
    /// dropped and a warning is logged. Default: 64 KB.
    /// Increase if your error messages or flash values are very large, but prefer
    /// keeping session payloads small to avoid session-storage pressure.
    /// </summary>
    public int MaxSessionPayloadBytes { get; set; } = 64 * 1024;
}
