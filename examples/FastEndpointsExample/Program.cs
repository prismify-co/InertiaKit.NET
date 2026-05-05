using FastEndpoints;
using Inertia.NET.FastEndpoints.Extensions;
using FastEndpointsExample.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register FastEndpoints with explicit assembly scanning
builder.Services.AddFastEndpoints(cfg =>
{
    cfg.Assemblies = new[] { typeof(HomeEndpoint).Assembly };
});
builder.Services.AddInertiaForFastEndpoints(options =>
{
    options.RootView = "App";
    options.VersionResolver = () => "1.0.0";
});

var app = builder.Build();

app.UseInertiaWithFastEndpoints();
app.UseFastEndpoints();

app.Run();

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
