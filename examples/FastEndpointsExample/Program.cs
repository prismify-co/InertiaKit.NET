using FastEndpoints;
using InertiaKit.AspNetCore;
using InertiaKit.FastEndpoints.Extensions;
using FastEndpointsExample.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var useViteDevServer = builder.Configuration.GetValue<bool>("INERTIA_USE_VITE_DEV_SERVER");

// Register FastEndpoints with explicit assembly scanning
builder.Services.AddFastEndpoints(cfg =>
{
    cfg.Assemblies = new[] { typeof(HomeEndpoint).Assembly };
});
builder.Services.AddInertiaForFastEndpoints(options =>
{
    options.VersionResolver = () => "1.2.0";
    ConfigureFastEndpointsAssetShell(options.AssetShell, useViteDevServer);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseInertiaWithFastEndpoints();
app.UseFastEndpoints();

app.Run();

static void ConfigureFastEndpointsAssetShell(InertiaAssetShellOptions assetShell, bool useDevelopmentServer)
{
    assetShell.Enabled = true;
    assetShell.DocumentTitle = "InertiaKit FastEndpoints";
    assetShell.StylesheetHrefs.Add("/fastendpoints/app.css");
    assetShell.ModuleScriptHrefs.Add("/fastendpoints/app.js");

    if (!useDevelopmentServer)
    {
        return;
    }

    assetShell.DevelopmentServerUrl = "http://127.0.0.1:5175";
    assetShell.DevelopmentModuleEntrypoints.Add("/src/app.js");
}

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
