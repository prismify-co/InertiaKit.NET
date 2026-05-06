using InertiaKit.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace InertiaKit.AspNetCore;

/// <summary>
/// Base class for per-application Inertia configuration.
/// Subclass this and register it via <c>services.AddInertiaHandler&lt;T&gt;()</c>.
/// </summary>
/// <example>
/// <code>
/// sealed class AppInertiaHandler : HandleInertiaRequestsBase
/// {
///     public override string? Version(HttpContext context) =>
///         Environment.GetEnvironmentVariable("ASSET_VERSION");
///
///     public override void Share(IInertiaShareBuilder shared, HttpContext context)
///     {
///         // Share current user on every response
///         shared.Add("auth", new
///         {
///             user = context.User.Identity?.IsAuthenticated == true
///                 ? new { name = context.User.Identity.Name }
///                 : null,
///         });
///
///         // CSRF token — share as a prop so the Inertia client can include it
///         // in AJAX requests. ASP.NET Core's antiforgery middleware validates it.
///         // Requires services.AddAntiforgery() and IAntiforgery injected here.
///         // shared.Add("csrf", antiforgery.GetAndStoreTokens(context).RequestToken);
///     }
/// }
/// </code>
/// </example>
public abstract class HandleInertiaRequestsBase
{
    /// <summary>
    /// Logical root document name used by the active <see cref="IInertiaRenderer"/>.
    /// The built-in MVC renderer treats this as the Razor view name.
    /// </summary>
    public virtual string RootView => "App";

    /// <summary>
    /// Return the current asset version string, or <c>null</c> to skip version checking.
    /// Called once per Inertia GET/HEAD request; the result is cached for the lifetime
    /// of the request so expensive resolvers (e.g. file hash) are called only once.
    /// </summary>
    public virtual string? Version(HttpContext context) => null;

    /// <summary>
    /// Register props shared with every Inertia response.
    /// Called once per Inertia request; use closures for lazy evaluation.
    /// <para>
    /// Typical shared props: current user, auth state, flash messages, CSRF token.
    /// </para>
    /// <para>
    /// CSRF note: ASP.NET Core handles CSRF validation via the antiforgery middleware.
    /// To expose the token to the Inertia client, inject <c>IAntiforgery</c> and share
    /// the request token as a prop (e.g. <c>shared.Add("csrf", tokens.RequestToken)</c>).
    /// </para>
    /// </summary>
    public virtual void Share(IInertiaShareBuilder shared, HttpContext context) { }
}
