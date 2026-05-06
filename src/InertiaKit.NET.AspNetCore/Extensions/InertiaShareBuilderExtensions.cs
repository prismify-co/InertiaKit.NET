using InertiaKit.AspNetCore.Internal;
using InertiaKit.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace InertiaKit.AspNetCore.Extensions;

public static class InertiaShareBuilderExtensions
{
    /// <summary>
    /// Shares the current antiforgery request token as an Inertia prop.
    /// Call this from <see cref="HandleInertiaRequestsBase.Share"/> when your
    /// client reads the token from props instead of the XSRF cookie.
    /// </summary>
    public static IInertiaShareBuilder AddCsrfToken(
        this IInertiaShareBuilder shared,
        HttpContext context,
        string propName = "csrf")
    {
        ArgumentNullException.ThrowIfNull(shared);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(propName);

        var tokenSet = InertiaAntiforgeryTokenStore.GetOrCreate(context);
        shared.Add(propName, tokenSet.RequestToken);
        return shared;
    }
}