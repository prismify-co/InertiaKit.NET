using Inertia.NET.FastEndpoints;

namespace FastEndpointsExample.Endpoints;

/// <summary>Simple page — no props.</summary>
public class HomeEndpoint : InertiaEndpoint
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await RenderAsync("Home/Index", ct);
}
