using System.Net;
using System.Text.Json;
using FluentAssertions;
using Inertia.NET.AspNetCore;
using Inertia.NET.AspNetCore.Extensions;
using Inertia.NET.AspNetCore.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inertia.NET.AspNetCore.Tests.Middleware;

/// <summary>
/// TDD tests written BEFORE the fourth-pass audit fixes — RED phase.
/// </summary>
public class FourthPassAuditTests
{
    // ── 1. IsSameOriginReferer — explicit default port normalisation ──────────
    // Integration tests use the TestServer's own origin (http://localhost).
    // Port-normalisation correctness is verified at the unit level below.

    [Fact]
    public async Task Back_accepts_same_origin_referer_with_explicit_default_port_via_http_80()
    {
        // TestServer runs on http://localhost — Referer with explicit :80 is same-origin.
        using var host = PostHost("/action", ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var r = inertia.Back(ctx, fallback: "/safe");
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = r.Url;
        });
        await host.StartAsync();
        var client = InertiaClient(host);
        // TestServer host is "localhost", scheme is "http" — explicit port 80 is the default
        client.DefaultRequestHeaders.Add("Referer", "http://localhost:80/previous-page");

        var response = await client.PostAsync("/action", null);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        location.Should().NotBe("/safe",
            because: "http://localhost:80 is same-origin as http://localhost (80 is the default http port)");
    }

    [Fact]
    public async Task Back_rejects_cross_origin_referer_with_different_host()
    {
        using var host = PostHost("/action", ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var r = inertia.Back(ctx, fallback: "/safe");
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = r.Url;
        });
        await host.StartAsync();
        var client = InertiaClient(host);
        client.DefaultRequestHeaders.Add("Referer", "http://evil.example.com/steal");

        var response = await client.PostAsync("/action", null);

        response.Headers.Location?.ToString().Should().Be("/safe");
    }

    // Unit-level tests for the method itself — no HTTP overhead, precise host control

    [Fact]
    public void IsSameOriginReferer_accepts_https_with_explicit_443()
    {
        var ctx = BuildFakeContext(scheme: "https", host: "app.example.com");

        InertiaService.IsSameOriginReferer("https://app.example.com:443/page", ctx)
            .Should().BeTrue();
    }

    [Fact]
    public void IsSameOriginReferer_accepts_http_with_explicit_80()
    {
        var ctx = BuildFakeContext(scheme: "http", host: "app.example.com");

        InertiaService.IsSameOriginReferer("http://app.example.com:80/page", ctx)
            .Should().BeTrue();
    }

    [Fact]
    public void IsSameOriginReferer_rejects_non_default_port_on_different_host()
    {
        var ctx = BuildFakeContext(scheme: "https", host: "app.example.com");

        InertiaService.IsSameOriginReferer("https://evil.com:443/page", ctx)
            .Should().BeFalse();
    }

    // ── 2. WriteFlashToSession in redirect respects configured MaxBytes ────────

    [Fact]
    public async Task Flash_in_redirect_respects_configured_MaxSessionPayloadBytes()
    {
        // Configure a tiny limit so even a small flash payload is dropped
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddDistributedMemoryCache();
                    s.AddSession();
                    // Set a 10-byte limit — any flash JSON will exceed this
                    s.AddInertia(o => o.MaxSessionPayloadBytes = 10);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseSession();
                    app.UseInertia();
                    app.UseEndpoints(e =>
                    {
                        // POST: render with flash then redirect
                        e.MapPost("/submit", async ctx =>
                        {
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            var result = inertia.Render("Form")
                                .WithFlash("message", "This flash data is longer than 10 bytes");
                            ctx.SetInertiaResult(result);
                            ctx.Response.StatusCode = StatusCodes.Status302Found;
                            ctx.Response.Headers.Location = "/form";
                            await Task.CompletedTask;
                        });

                        // GET: check session for flash
                        e.MapGet("/form", async ctx =>
                        {
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            ctx.SetInertiaResult(inertia.Render("Form"));
                            await Task.CompletedTask;
                        });
                    });
                });
            }).Build();

        await host.StartAsync();
        var server = host.GetTestServer();

        var cookieHandler = new CookieContainerHandler(server.CreateHandler());
        using var client = new HttpClient(cookieHandler) { BaseAddress = server.BaseAddress };
        client.DefaultRequestHeaders.Add("X-Inertia", "true");

        // POST to trigger flash + redirect
        await client.PostAsync("/submit", null);

        // GET to check — flash must have been DROPPED because payload > 10 bytes
        var response = await client.GetAsync("/form");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // flash should NOT appear (was dropped due to size limit)
        doc.RootElement.GetProperty("props").TryGetProperty("flash", out _)
            .Should().BeFalse("flash payload exceeded MaxSessionPayloadBytes and must be dropped");
    }

    // ── 3. Initial HTML render: 200 even when session errors present ──────────

    [Fact]
    public async Task Initial_html_render_returns_200_even_with_session_errors()
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
                        e.MapGet("/form", async ctx =>
                        {
                            // Pre-load session errors (as if a prior POST flashed them)
                            InertiaMiddleware.WriteErrorsToSession(ctx, new Dictionary<string, string[]>
                            {
                                ["name"] = ["Required"],
                            });
                            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
                            ctx.SetInertiaResult(inertia.Render("Form"));
                            await Task.CompletedTask;
                        }));
                });
            }).Build();

        await host.StartAsync();
        // NON-Inertia client (no X-Inertia header) → initial HTML render
        var client = host.GetTestServer().CreateClient();

        var response = await client.GetAsync("/form");

        // Must be 200 (not 422) even though errors are in props
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 4. Initial HTML render: Vary header present ───────────────────────────

    [Fact]
    public async Task Initial_html_render_includes_vary_x_inertia_header()
    {
        using var host = GetHost("/page", ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            ctx.SetInertiaResult(inertia.Render("Page", new { count = 1 }));
        });
        await host.StartAsync();
        var client = host.GetTestServer().CreateClient(); // no X-Inertia header

        var response = await client.GetAsync("/page");

        response.Headers.Vary.Should().Contain("X-Inertia");
    }

    // ── 5. Initial HTML render: X-Inertia NOT set on HTML response ────────────

    [Fact]
    public async Task Initial_html_render_does_not_set_x_inertia_response_header()
    {
        using var host = GetHost("/page", ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            ctx.SetInertiaResult(inertia.Render("Page"));
        });
        await host.StartAsync();
        var client = host.GetTestServer().CreateClient(); // no X-Inertia header

        var response = await client.GetAsync("/page");

        response.Headers.Contains("X-Inertia").Should().BeFalse(
            because: "X-Inertia response header is only for JSON responses, not initial HTML renders");
    }

    // ── 6. IsValidErrorBagName rejects excessively long names ────────────────

    [Fact]
    public void IsValidErrorBagName_rejects_name_longer_than_128_chars()
    {
        var longName = new string('a', 129);

        InertiaMiddleware.IsValidErrorBagName(longName).Should().BeFalse();
    }

    [Fact]
    public void IsValidErrorBagName_accepts_name_of_exactly_128_chars()
    {
        var maxName = new string('a', 128);

        InertiaMiddleware.IsValidErrorBagName(maxName).Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IHost GetHost(string path, Action<HttpContext> handler) =>
        new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(s => { s.AddRouting(); s.AddInertia(); });
            web.Configure(app =>
            {
                app.UseRouting(); app.UseInertia();
                app.UseEndpoints(e =>
                    e.MapGet(path, async ctx => { handler(ctx); await Task.CompletedTask; }));
            });
        }).Build();

    private static IHost PostHost(string path, Action<HttpContext> handler) =>
        new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(s => { s.AddRouting(); s.AddInertia(); });
            web.Configure(app =>
            {
                app.UseRouting(); app.UseInertia();
                app.UseEndpoints(e =>
                    e.MapPost(path, async ctx => { handler(ctx); await Task.CompletedTask; }));
            });
        }).Build();

    private static HttpClient InertiaClient(IHost host)
    {
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        return client;
    }

    /// <summary>Builds a minimal fake HttpContext for unit-testing IsSameOriginReferer.</summary>
    private static HttpContext BuildFakeContext(string scheme, string host)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = new HostString(host);
        return ctx;
    }
}
