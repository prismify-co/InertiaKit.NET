using Microsoft.AspNetCore.Http;

namespace InertiaKit.AspNetCore;

public sealed class InertiaAntiforgeryOptions
{
    /// <summary>
    /// Cookie name used by the Inertia browser client when copying the request
    /// token into the configured header.
    /// </summary>
    public string CookieName { get; set; } = "XSRF-TOKEN";

    /// <summary>
    /// Header name accepted by ASP.NET Core antiforgery validation.
    /// </summary>
    public string HeaderName { get; set; } = "X-XSRF-TOKEN";

    /// <summary>
    /// Cookie path used for the client-readable XSRF token cookie.
    /// </summary>
    public string CookiePath { get; set; } = "/";

    /// <summary>
    /// SameSite mode applied to the client-readable XSRF token cookie.
    /// </summary>
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>
    /// Secure-cookie policy for the client-readable XSRF token cookie.
    /// </summary>
    public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.SameAsRequest;
}