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
///         // If you want Inertia's built-in XSRF header handling, issue the
///         // client-readable XSRF cookie on each page response.
///         // Requires services.AddInertiaAntiforgery() or services.AddAntiforgery().
///         // context.SetXsrfTokenCookie();
///
///         // If your client reads the request token from props instead, share it explicitly.
///         // shared.AddCsrfToken(context);
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
    /// CSRF note: ASP.NET Core handles validation via its antiforgery services.
    /// Use <c>context.SetXsrfTokenCookie()</c> for Inertia's built-in XSRF cookie flow,
    /// or <c>shared.AddCsrfToken(context)</c> when your client expects the request token as a prop.
    /// </para>
    /// </summary>
    public virtual void Share(IInertiaShareBuilder shared, HttpContext context) { }
}
