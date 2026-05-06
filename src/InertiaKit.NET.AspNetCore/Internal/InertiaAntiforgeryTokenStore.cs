using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.AspNetCore.Internal;

internal static class InertiaAntiforgeryTokenStore
{
    private static readonly object TokenSetKey = new();

    internal static AntiforgeryTokenSet GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(TokenSetKey, out var existing)
            && existing is AntiforgeryTokenSet tokenSet)
        {
            return tokenSet;
        }

        var antiforgery = context.RequestServices.GetService<IAntiforgery>()
            ?? throw new InvalidOperationException(
                "IAntiforgery is not registered. Call services.AddAntiforgery() or services.AddInertiaAntiforgery().");

        var created = antiforgery.GetAndStoreTokens(context);
        if (string.IsNullOrWhiteSpace(created.RequestToken))
            throw new InvalidOperationException("ASP.NET Core antiforgery did not return a request token.");

        context.Items[TokenSetKey] = created;
        return created;
    }
}