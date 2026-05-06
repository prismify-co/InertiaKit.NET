using InertiaKit.Core;
using InertiaKit.AspNetCore.Internal;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InertiaKit.AspNetCore.Extensions;

public static class HttpContextExtensions
{
    /// <summary>Returns the parsed Inertia request context for the current request.</summary>
    public static InertiaRequest GetInertiaRequest(this HttpContext context) =>
        context.Items.TryGetValue(typeof(InertiaRequest), out var r) && r is InertiaRequest ir
            ? ir
            : InertiaRequest.NonInertia();

    /// <summary>Stores the Inertia result so the middleware can process it.</summary>
    public static void SetInertiaResult(this HttpContext context, InertiaResult result) =>
        context.Items[typeof(InertiaResult)] = result;

    /// <summary>
    /// Writes validation errors to the session so the Inertia middleware can inject
    /// them into <c>props.errors</c> on the next GET request (PRG pattern).
    /// </summary>
    /// <example>
    /// <code>
    /// // In a POST handler after validation fails:
    /// context.FlashErrors(new Dictionary&lt;string, string[]&gt; {
    ///     ["email"] = ["Email is required", "Must be a valid address"],
    ///     ["name"]  = ["Name is required"],
    /// });
    /// return Results.Redirect("/form");
    /// </code>
    /// </example>
    public static void FlashErrors(this HttpContext context, Dictionary<string, string[]> errors)
    {
        var maxBytes = context.RequestServices
            .GetService<IOptions<InertiaOptions>>()
            ?.Value.MaxSessionPayloadBytes ?? 64 * 1024;
        InertiaMiddleware.WriteErrorsToSession(context, errors, maxBytes);
    }

    /// <summary>
    /// Convenience overload accepting a single error per field.
    /// The error is stored as a one-element array.
    /// </summary>
    public static void FlashErrors(this HttpContext context, Dictionary<string, string> errors) =>
        context.FlashErrors(errors.ToDictionary(kvp => kvp.Key, kvp => new[] { kvp.Value }));

    /// <summary>
    /// Writes a client-readable XSRF cookie using the current antiforgery request token.
    /// The Inertia browser client can then mirror this value into the configured header.
    /// </summary>
    public static string SetXsrfTokenCookie(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tokenSet = InertiaAntiforgeryTokenStore.GetOrCreate(context);
        var options = context.RequestServices
            .GetService<IOptions<InertiaAntiforgeryOptions>>()?.Value
            ?? new InertiaAntiforgeryOptions();

        context.Response.Cookies.Append(options.CookieName, tokenSet.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            Path = options.CookiePath,
            SameSite = options.CookieSameSite,
            Secure = options.CookieSecurePolicy switch
            {
                CookieSecurePolicy.Always => true,
                CookieSecurePolicy.None => false,
                _ => context.Request.IsHttps,
            },
        });

        return tokenSet.RequestToken!;
    }
}
