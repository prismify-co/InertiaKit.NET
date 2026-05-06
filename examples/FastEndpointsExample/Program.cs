using FastEndpoints;
using InertiaKit.FastEndpoints.Extensions;
using FastEndpointsExample.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register FastEndpoints with explicit assembly scanning
builder.Services.AddFastEndpoints(cfg =>
{
    cfg.Assemblies = new[] { typeof(HomeEndpoint).Assembly };
});
builder.Services.AddInertiaForFastEndpoints(options =>
{
    options.VersionResolver = () => "1.0.0";
    options.AssetShell.Enabled = true;
    options.AssetShell.DocumentTitle = "InertiaKit FastEndpoints";
    options.AssetShell.StylesheetHrefs.Add("/fastendpoints/app.css");
    options.AssetShell.ModuleScriptHrefs.Add("/fastendpoints/app.js");
});

var app = builder.Build();

app.UseStaticFiles();
app.UseInertiaWithFastEndpoints();
app.UseFastEndpoints();

app.Run();

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
