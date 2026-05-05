using System.Net;
using System.Text.Json;
using FluentAssertions;
using Inertia.NET.AspNetCore;
using Inertia.NET.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inertia.NET.AspNetCore.Tests.Middleware;

/// <summary>Tests that verify protocol-compliance fixes from the audit.</summary>
public class ProtocolComplianceTests
{
    // ── 422 on validation errors ───────────────────────────────────────────────

    [Fact]
    public async Task Post_with_errors_in_props_returns_422()
    {
        using var host = BuildHost("/submit", HttpMethods.Post, ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var result = inertia.Render("Form", new Dictionary<string, object?>
            {
                ["errors"] = new { name = "Required" },
            });
            ctx.SetInertiaResult(result);
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.PostAsync("/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Get_with_errors_in_props_returns_200_not_422()
    {
        using var host = BuildHost("/page", HttpMethods.Get, ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var result = inertia.Render("Page", new Dictionary<string, object?>
            {
                ["errors"] = new { name = "Whatever" },
            });
            ctx.SetInertiaResult(result);
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/page");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 302→303 promotion ─────────────────────────────────────────────────────

    [Fact]
    public async Task Post_302_redirect_is_promoted_to_303()
    {
        using var host = BuildHost("/create", HttpMethods.Post, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/users";
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.PostAsync("/create", null);

        ((int)response.StatusCode).Should().Be(StatusCodes.Status303SeeOther);
    }

    [Fact]
    public async Task Get_302_redirect_is_not_promoted()
    {
        using var host = BuildHost("/old", HttpMethods.Get, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/new";
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/old");

        ((int)response.StatusCode).Should().Be(StatusCodes.Status302Found);
    }

    // ── Fragment redirect → 409 ───────────────────────────────────────────────

    [Fact]
    public async Task Redirect_with_fragment_returns_409_with_inertia_location()
    {
        using var host = BuildHost("/go", HttpMethods.Get, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/page#section";
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/go");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.TryGetValues("X-Inertia-Location", out var values).Should().BeTrue();
        values!.First().Should().Be("/page#section");
    }

    // ── Version mismatch (StringComparison.Ordinal) ───────────────────────────

    [Fact]
    public async Task Version_mismatch_uses_exact_ordinal_comparison()
    {
        // "V1" ≠ "v1" under ordinal — must trigger 409
        using var host = BuildHost("/page", HttpMethods.Get, _ => { },
            o => o.VersionResolver = () => "V1");
        await host.StartAsync();
        var client = InertiaClient(host, version: "v1");

        var response = await client.GetAsync("/page");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Prefetch returns 204 ──────────────────────────────────────────────────

    [Fact]
    public async Task Prefetch_request_returns_204_without_running_action()
    {
        var actionRan = false;
        using var host = BuildHost("/page", HttpMethods.Get, _ => { actionRan = true; });
        await host.StartAsync();
        var client = InertiaClient(host);
        client.DefaultRequestHeaders.Add("Purpose", "prefetch");

        var response = await client.GetAsync("/page");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        actionRan.Should().BeFalse();
    }

    // ── Error bag scoping ─────────────────────────────────────────────────────

    [Fact]
    public async Task Session_errors_are_scoped_under_error_bag_name()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddDistributedMemoryCache();
                    s.AddSession();
                    s.AddInertia();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseSession();
                    app.UseInertia();
                    app.UseEndpoints(e =>
                    {
                        e.MapGet("/form", async ctx =>
                        {
                            // Write errors to session (simulating prior POST)
                            InertiaMiddleware.WriteErrorsToSession(ctx,
                                new Dictionary<string, string[]>
                                {
                                    ["email"] = ["Email required"],
                                });
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            ctx.SetInertiaResult(inertia.Render("Form"));
                            await Task.CompletedTask;
                        });
                    });
                });
            }).Build();

        await host.StartAsync();
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Error-Bag", "loginForm");

        var response = await client.GetAsync("/form");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var errors = doc.RootElement.GetProperty("props").GetProperty("errors");

        // Errors must be nested under the bag name
        errors.TryGetProperty("loginForm", out var bag).Should().BeTrue();
        bag.TryGetProperty("email", out _).Should().BeTrue();
    }

    // ── Empty response → redirect to Referer ──────────────────────────────────

    [Fact]
    public async Task Inertia_request_with_no_result_redirects_to_referer()
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
                    app.UseEndpoints(e =>
                        e.MapGet("/empty", async ctx =>
                        {
                            // Intentionally no SetInertiaResult call
                            await Task.CompletedTask;
                        }));
                });
            }).Build();

        await host.StartAsync();
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("Referer", "/dashboard");

        var response = await client.GetAsync("/empty");

        ((int)response.StatusCode).Should().Be(StatusCodes.Status303SeeOther);
        response.Headers.Location?.ToString().Should().Be("/dashboard");
    }

    // ── Once-props client header wired through ────────────────────────────────

    [Fact]
    public async Task Once_props_excluded_when_client_sends_once_props_header()
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
                    app.UseEndpoints(e =>
                        e.MapGet("/page", async ctx =>
                        {
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            var result = inertia.Render("Page", new Dictionary<string, object?>
                            {
                                ["countries"] = inertia.Once(new[] { "US", "CA" }),
                            });
                            ctx.SetInertiaResult(result);
                            await Task.CompletedTask;
                        }));
                });
            }).Build();

        await host.StartAsync();
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Once-Props", "countries");

        var response = await client.GetAsync("/page");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        // Value must NOT be in props (client already has it)
        doc.RootElement.GetProperty("props").TryGetProperty("countries", out _).Should().BeFalse();
        // But key must appear in onceProps
        doc.RootElement.TryGetProperty("onceProps", out var once).Should().BeTrue();
        once.EnumerateArray().Select(e => e.GetString()).Should().Contain("countries");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IHost BuildHost(
        string path,
        string method,
        Action<HttpContext> handler,
        Action<InertiaOptions>? configure = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s => { s.AddRouting(); s.AddInertia(configure); });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseInertia();
                    app.UseEndpoints(e =>
                    {
                        if (method == HttpMethods.Post)
                            e.MapPost(path, async ctx => { handler(ctx); await Task.CompletedTask; });
                        else
                            e.MapGet(path, async ctx => { handler(ctx); await Task.CompletedTask; });
                    });
                });
            }).Build();
    }

    private static HttpClient InertiaClient(IHost host, string version = "v1")
    {
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        if (!string.IsNullOrEmpty(version))
            client.DefaultRequestHeaders.Add("X-Inertia-Version", version);
        return client;
    }
}
