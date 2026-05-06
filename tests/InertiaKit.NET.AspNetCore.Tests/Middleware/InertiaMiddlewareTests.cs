using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InertiaKit.AspNetCore.Tests.Middleware;

public class InertiaMiddlewareTests
{
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
                            var result = inertia.Render("Users/Index", new { count = 42 });
                            ctx.SetInertiaResult(result);
                            await Task.CompletedTask;
                        });
                    });
                });
            })
            .Build();
    }

    // ── Vary header ────────────────────────────────────────────────────────

    [Fact]
    public async Task Vary_header_is_set_on_non_inertia_requests()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/users");

        response.Headers.Vary.Should().Contain("X-Inertia");
    }

    [Fact]
    public async Task Vary_header_is_set_on_inertia_requests()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "v1");

        var response = await client.GetAsync("/users");

        response.Headers.Vary.Should().Contain("X-Inertia");
    }

    // ── Inertia JSON response ──────────────────────────────────────────────

    [Fact]
    public async Task Inertia_request_returns_json_with_page_object()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "");

        var response = await client.GetAsync("/users");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        doc.RootElement.GetProperty("component").GetString().Should().Be("Users/Index");
        doc.RootElement.GetProperty("props").GetProperty("count").GetInt32().Should().Be(42);
        doc.RootElement.GetProperty("url").GetString().Should().Be("/users");
    }

    [Fact]
    public async Task Response_includes_X_Inertia_header_on_inertia_requests()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");

        var response = await client.GetAsync("/users");

        response.Headers.Contains("X-Inertia").Should().BeTrue();
    }

    [Fact]
    public async Task Non_inertia_request_uses_default_html_shell_renderer()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/users");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        body.Should().Contain("id=\"app-data\"");
        body.Should().Contain("<div id=\"app\"></div>");
    }

    [Fact]
    public async Task Non_inertia_request_uses_custom_renderer_when_registered()
    {
        using var host = BuildHost(configureServices: services =>
            services.AddInertiaRenderer<TestInertiaRenderer>());
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/users");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        body.Should().Contain("data-testid=\"custom-inertia-renderer\"");
        body.Should().Contain("custom-app-data");
        body.Should().Contain("Users/Index");
    }

    [Fact]
    public async Task Non_inertia_request_uses_asset_shell_renderer_when_enabled()
    {
        using var host = BuildHost(configure: options =>
        {
            options.AssetShell.Enabled = true;
            options.AssetShell.DocumentTitle = "Asset Shell";
            options.AssetShell.StylesheetHrefs.Add("/build/app.css");
            options.AssetShell.ModuleScriptHrefs.Add("/build/app.js");
        });
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/users");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        body.Should().Contain("<title>Asset Shell</title>");
        body.Should().Contain("href=\"/build/app.css\"");
        body.Should().Contain("type=\"module\" src=\"/build/app.js\"");
        body.Should().Contain("id=\"app-data\"");
    }

    [Fact]
    public async Task Asset_shell_renderer_takes_precedence_over_mvc_renderer_when_enabled()
    {
        using var host = BuildHost(
            configure: options =>
            {
                options.AssetShell.Enabled = true;
                options.AssetShell.ModuleScriptHrefs.Add("/build/app.js");
            },
            configureServices: services => services.AddControllersWithViews());
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/users");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("type=\"module\" src=\"/build/app.js\"");
    }

    [Fact]
    public async Task Direct_IResult_return_writes_the_inertia_page()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddInertia();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseInertia();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/direct", (HttpContext ctx) =>
                        {
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            return inertia.Render("Users/Index", new { count = 42 });
                        });
                    });
                });
            })
            .Build();

        await host.StartAsync();
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "");

        var response = await client.GetAsync("/direct");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Contains("X-Inertia").Should().BeTrue();
        doc.RootElement.GetProperty("component").GetString().Should().Be("Users/Index");
        doc.RootElement.GetProperty("props").GetProperty("count").GetInt32().Should().Be(42);
    }

    // ── Version mismatch ───────────────────────────────────────────────────

    [Fact]
    public async Task Version_mismatch_on_GET_returns_409_with_location()
    {
        using var host = BuildHost(configure: o => o.VersionResolver = () => "new-version");
        await host.StartAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "old-version");

        // Prevent automatic redirect following
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var manualClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        manualClient.DefaultRequestHeaders.Add("X-Inertia", "true");
        manualClient.DefaultRequestHeaders.Add("X-Inertia-Version", "old-version");

        var testServer = host.GetTestServer();
        using var testClient = testServer.CreateClient();
        testClient.DefaultRequestHeaders.Add("X-Inertia", "true");
        testClient.DefaultRequestHeaders.Add("X-Inertia-Version", "old-version");

        var response = await testClient.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.Contains("X-Inertia-Location").Should().BeTrue();
    }

    [Fact]
    public async Task Matching_version_does_not_trigger_409()
    {
        using var host = BuildHost(configure: o => o.VersionResolver = () => "v1");
        await host.StartAsync();
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "v1");

        var response = await client.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Partial reload ─────────────────────────────────────────────────────

    [Fact]
    public async Task Partial_reload_returns_only_requested_props()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s => { s.AddRouting(); s.AddInertia(); });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseInertia();
                    app.UseEndpoints(e => e.MapGet("/dashboard", async ctx =>
                    {
                        var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                        var result = inertia.Render("Dashboard", new Dictionary<string, object?>
                        {
                            ["users"] = new[] { "Alice" },
                            ["posts"] = new[] { "Post1" },
                        });
                        ctx.SetInertiaResult(result);
                        await Task.CompletedTask;
                    }));
                });
            }).Build();

        await host.StartAsync();
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Component", "Dashboard");
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Data", "users");

        var response = await client.GetAsync("/dashboard");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var props = doc.RootElement.GetProperty("props");

        props.TryGetProperty("users", out _).Should().BeTrue();
        props.TryGetProperty("posts", out _).Should().BeFalse();
    }

    private sealed class TestInertiaRenderer : IInertiaRenderer
    {
        public bool CanRender(InertiaRenderContext context) => true;

        public Task RenderAsync(InertiaRenderContext context)
        {
            context.HttpContext.Response.ContentType = "text/html";

            var html = $$"""
            <!DOCTYPE html>
            <html>
            <body data-testid="custom-inertia-renderer">
            <div id="app">custom</div>
            <script type="application/json" id="custom-app-data">{{context.PageJson}}</script>
            </body>
            </html>
            """;

            return context.HttpContext.Response.WriteAsync(html);
        }
    }
}
