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

    public override async Task<IResult> HandleAsync(CancellationToken ct) =>
        RenderAsync("Home/Index");
}
