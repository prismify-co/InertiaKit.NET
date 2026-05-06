using System.Net;
using System.Text.Json;
using FluentAssertions;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InertiaKit.AspNetCore.Tests.Middleware;

public class SecuritySupportTests
{
    [Fact]
    public async Task Global_history_encryption_default_is_applied_to_inertia_responses()
    {
        using var host = BuildHost(configure: options => options.History.Encrypt = true);
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/users");
        var page = await ReadPageObject(response);

        page.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Route_metadata_encrypts_history_for_minimal_api_endpoints()
    {
        using var host = BuildHost(routes: endpoints =>
        {
            endpoints.MapGet("/secure", async ctx =>
            {
                var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                ctx.SetInertiaResult(inertia.Render("Users/Index", new { count = 7 }));
                await Task.CompletedTask;
            }).WithEncryptHistory();
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/secure");
        var page = await ReadPageObject(response);

        page.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Route_metadata_can_disable_global_history_encryption()
    {
        using var host = BuildHost(
            routes: endpoints =>
            {
                endpoints.MapGet("/public", async ctx =>
                {
                    var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                    ctx.SetInertiaResult(inertia.Render("Users/Index", new { count = 3 }));
                    await Task.CompletedTask;
                }).WithEncryptHistory(false);
            },
            configure: options => options.History.Encrypt = true);
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/public");
        var page = await ReadPageObject(response);

        page.GetProperty("encryptHistory").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Explicit_result_history_setting_overrides_route_and_global_defaults()
    {
        using var host = BuildHost(
            routes: endpoints =>
            {
                endpoints.MapGet("/override", async ctx =>
                {
                    var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                    ctx.SetInertiaResult(inertia.Render("Users/Index", new { count = 9 }).WithEncryptHistory(false));
                    await Task.CompletedTask;
                }).WithEncryptHistory();
            },
            configure: options => options.History.Encrypt = true);
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/override");
        var page = await ReadPageObject(response);

        page.GetProperty("encryptHistory").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Mvc_encrypt_history_attribute_applies_route_metadata()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddControllers().AddApplicationPart(typeof(MvcEncryptHistoryController).Assembly);
                    services.AddInertia();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseInertia();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            })
            .Build();
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/mvc-history");
        var page = await ReadPageObject(response);

        page.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Route_metadata_clears_history_for_minimal_api_endpoints()
    {
        using var host = BuildHost(routes: endpoints =>
        {
            endpoints.MapGet("/signed-out", async ctx =>
            {
                var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                ctx.SetInertiaResult(inertia.Render("Home/Index", new { message = "Signed out" }));
                await Task.CompletedTask;
            }).WithClearHistory();
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/signed-out");
        var page = await ReadPageObject(response);

        page.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Explicit_result_clear_history_setting_overrides_route_defaults()
    {
        using var host = BuildHost(routes: endpoints =>
        {
            endpoints.MapGet("/signed-out", async ctx =>
            {
                var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                ctx.SetInertiaResult(inertia.Render("Home/Index", new { message = "Signed out" }).WithClearHistory(false));
                await Task.CompletedTask;
            }).WithClearHistory();
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/signed-out");
        var page = await ReadPageObject(response);

        page.GetProperty("clearHistory").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Mvc_clear_history_attribute_applies_route_metadata()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddControllers().AddApplicationPart(typeof(MvcEncryptHistoryController).Assembly);
                    services.AddInertia();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseInertia();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            })
            .Build();
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/mvc-signed-out");
        var page = await ReadPageObject(response);

        page.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Explicit_result_clear_history_is_emitted_for_logout_flows()
    {
        using var host = BuildHost(routes: endpoints =>
        {
            endpoints.MapGet("/expired", async ctx =>
            {
                var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                ctx.SetInertiaResult(inertia.Render("Home/Index", new { message = "Session expired" }).WithClearHistory());
                await Task.CompletedTask;
            });
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/expired");
        var page = await ReadPageObject(response);

        page.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void AddInertiaAntiforgery_sets_inertia_friendly_header_defaults()
    {
        using var host = BuildHost(configureServices: services => services.AddInertiaAntiforgery());

        var options = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        options.HeaderName.Should().Be("X-XSRF-TOKEN");
    }

    [Fact]
    public async Task Antiforgery_helpers_emit_matching_csrf_prop_and_xsrf_cookie()
    {
        using var host = BuildHost(
            configureServices: services =>
            {
                services.AddInertiaAntiforgery();
                services.AddInertiaHandler<AntiforgeryTestHandler>();
            });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/users");
        var page = await ReadPageObject(response);
        var token = page.GetProperty("props").GetProperty("csrf").GetString();
        var xsrfCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));

        token.Should().NotBeNullOrWhiteSpace();
        ReadCookieValue(xsrfCookie).Should().Be(token);
    }

    private static IHost BuildHost(
        Action<IEndpointRouteBuilder>? routes = null,
        Action<InertiaOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddInertia(configure);
                    configureServices?.Invoke(services);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseInertia();
                    app.UseEndpoints(endpoints =>
                    {
                        routes?.Invoke(endpoints);
                        endpoints.MapGet("/users", async ctx =>
                        {
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            ctx.SetInertiaResult(inertia.Render("Users/Index", new { count = 42 }));
                            await Task.CompletedTask;
                        });
                    });
                });
            })
            .Build();
    }

    private static HttpClient InertiaClient(IHost host)
    {
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", string.Empty);
        return client;
    }

    private static async Task<JsonElement> ReadPageObject(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private static string ReadCookieValue(string setCookieHeader)
    {
        var cookie = setCookieHeader.Split(';', 2)[0];
        var value = cookie.Split('=', 2)[1];
        return Uri.UnescapeDataString(value);
    }

    private sealed class AntiforgeryTestHandler : HandleInertiaRequestsBase
    {
        public override void Share(InertiaKit.Core.Abstractions.IInertiaShareBuilder shared, HttpContext context)
        {
            context.SetXsrfTokenCookie();
            shared.AddCsrfToken(context);
        }
    }
}

[ApiController]
public sealed class MvcEncryptHistoryController : ControllerBase
{
    [HttpGet("/mvc-history")]
    [EncryptHistory]
    public IActionResult Get([FromServices] IInertiaService inertia) =>
        inertia.Render("Users/Index", new { count = 11 });

    [HttpGet("/mvc-signed-out")]
    [ClearHistory]
    public IActionResult SignedOut([FromServices] IInertiaService inertia) =>
        inertia.Render("Home/Index", new { message = "Signed out" });
}