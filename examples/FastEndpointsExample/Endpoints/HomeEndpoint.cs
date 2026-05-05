using InertiaKit.FastEndpoints;

namespace FastEndpointsExample.Endpoints;

/// <summary>Simple page — no props.</summary>
public class HomeEndpoint : InertiaEndpoint
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        RenderAsync("Home/Index", ct);
}
