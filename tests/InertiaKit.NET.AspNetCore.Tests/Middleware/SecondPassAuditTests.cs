using System.Net;
using System.Text.Json;
using FluentAssertions;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InertiaKit.AspNetCore.Tests.Middleware;

/// <summary>
/// TDD tests written BEFORE the fixes identified in the second audit pass.
/// Each test targets one specific gap.
/// </summary>
public class SecondPassAuditTests
{
    // ── Component mismatch forces full reload ─────────────────────────────────

    [Fact]
    public async Task Partial_reload_with_mismatched_component_returns_full_page_object()
    {
        // Arrange: endpoint renders "Dashboard" component
        using var host = SimpleHost("/page", HttpMethods.Get, ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            ctx.SetInertiaResult(inertia.Render("Dashboard", new Dictionary<string, object?>
            {
                ["users"] = new[] { "Alice" },
                ["stats"] = new { total = 5 },
            }));
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        // Client sends partial reload for a DIFFERENT component
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Component", "OldComponent");
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Data", "users");

        var response = await client.GetAsync("/page");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var props = doc.RootElement.GetProperty("props");

        // Both props must be present — component mismatch triggers full reload
        props.TryGetProperty("users", out _).Should().BeTrue();
        props.TryGetProperty("stats", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Partial_reload_with_matching_component_applies_filter_normally()
    {
        using var host = SimpleHost("/page", HttpMethods.Get, ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            ctx.SetInertiaResult(inertia.Render("Dashboard", new Dictionary<string, object?>
            {
                ["users"] = new[] { "Alice" },
                ["stats"] = new { total = 5 },
            }));
        });
        await host.StartAsync();
        var client = InertiaClient(host);
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Component", "Dashboard");
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Data", "users");

        var response = await client.GetAsync("/page");
        var props = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("props");

        props.TryGetProperty("users", out _).Should().BeTrue();
        props.TryGetProperty("stats", out _).Should().BeFalse(); // filtered out
    }

    // ── Fragment detection on URL-encoded %23 ─────────────────────────────────

    [Fact]
    public async Task Redirect_with_percent_encoded_hash_in_query_does_not_trigger_409()
    {
        // /search?q=%23tag has %23 (encoded #) in the query — NOT a real fragment
        using var host = SimpleHost("/go", HttpMethods.Get, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/search?q=%23tag";
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/go");

        // Must NOT be treated as a fragment redirect
        ((int)response.StatusCode).Should().NotBe(StatusCodes.Status409Conflict);
        response.StatusCode.Should().Be(HttpStatusCode.Found); // plain 302
    }

    [Fact]
    public async Task Redirect_with_real_fragment_still_triggers_409()
    {
        using var host = SimpleHost("/go", HttpMethods.Get, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/page#section";
        });
        await host.StartAsync();
        var client = InertiaClient(host);

        var response = await client.GetAsync("/go");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.TryGetValues("X-Inertia-Location", out var loc).Should().BeTrue();
        loc!.First().Should().Be("/page#section");
    }

    // ── Error bag name sanitization ───────────────────────────────────────────

    [Fact]
    public async Task Invalid_error_bag_name_is_rejected_and_errors_stored_ungrouped()
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
                            InertiaMiddleware.WriteErrorsToSession(ctx,
                                new Dictionary<string, string[]> { ["email"] = ["Required"] });
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
        // Inject a malicious error bag name — must be rejected
        client.DefaultRequestHeaders.Add("X-Inertia-Error-Bag", "__proto__");

        var response = await client.GetAsync("/form");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var props = doc.RootElement.GetProperty("props");

        // "__proto__" is not a valid bag name — errors fall back to ungrouped
        props.TryGetProperty("errors", out var errors).Should().BeTrue();
        // Must not be nested under "__proto__"
        errors.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        // Errors object should NOT have "__proto__" as a key
        if (errors.ValueKind == JsonValueKind.Object)
            errors.TryGetProperty("__proto__", out _).Should().BeFalse();
    }

    // ── Back() same-origin Referer validation ─────────────────────────────────

    [Fact]
    public async Task Back_uses_fallback_when_referer_is_cross_origin()
    {
        using var host = SimpleHost("/action", HttpMethods.Post, ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var redirect = inertia.Back(ctx, fallback: "/safe");
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = redirect.Url;
        });
        await host.StartAsync();
        var client = InertiaClient(host);
        // Spoofed cross-origin Referer
        client.DefaultRequestHeaders.Add("Referer", "https://evil.example.com/phish");

        var response = await client.PostAsync("/action", null);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        location.Should().NotContain("evil.example.com");
        location.Should().Be("/safe");
    }

    [Fact]
    public async Task Back_uses_same_origin_referer_when_valid()
    {
        using var host = SimpleHost("/action", HttpMethods.Post, ctx =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var redirect = inertia.Back(ctx, fallback: "/safe");
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = redirect.Url;
        });
        await host.StartAsync();
        var client = InertiaClient(host);
        // Same-origin Referer (relative path is always safe)
        client.DefaultRequestHeaders.Add("Referer", "/dashboard");

        var response = await client.PostAsync("/action", null);

        response.Headers.Location?.ToString().Should().Be("/dashboard");
    }

    // ── FlashErrors public API ────────────────────────────────────────────────

    [Fact]
    public async Task FlashErrors_extension_writes_errors_to_session_for_next_request()
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
                        // POST: flash errors then redirect
                        e.MapPost("/submit", async ctx =>
                        {
                            ctx.FlashErrors(new Dictionary<string, string[]>
                            {
                                ["name"] = ["Name is required"],
                            });
                            ctx.Response.StatusCode = StatusCodes.Status303SeeOther;
                            ctx.Response.Headers.Location = "/form";
                            await Task.CompletedTask;
                        });

                        // GET: read errors from session (via middleware)
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

        // Use a cookie-aware client so session persists across requests
        var cookieHandler = new CookieContainerHandler(server.CreateHandler());
        using var client = new HttpClient(cookieHandler) { BaseAddress = server.BaseAddress };
        client.DefaultRequestHeaders.Add("X-Inertia", "true");

        // POST to trigger error flash
        await client.PostAsync("/submit", null);

        // GET to read the flashed errors
        var response = await client.GetAsync("/form");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var props = doc.RootElement.GetProperty("props");

        props.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("name", out _).Should().BeTrue();
    }

    // ── ReturnAllErrors format flag ───────────────────────────────────────────

    [Fact]
    public async Task ReturnAllErrors_true_returns_array_of_errors_per_field()
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
                    s.AddInertia(o => o.ReturnAllErrors = true);
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
                            InertiaMiddleware.WriteErrorsToSession(ctx, new Dictionary<string, string[]>
                            {
                                ["email"] = ["Required", "Must be valid"],
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

        var response = await client.GetAsync("/form");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var emailErrors = doc.RootElement
            .GetProperty("props")
            .GetProperty("errors")
            .GetProperty("email");

        emailErrors.ValueKind.Should().Be(JsonValueKind.Array);
        emailErrors.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ReturnAllErrors_false_returns_single_string_per_field()
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
                    s.AddInertia(o => o.ReturnAllErrors = false);
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
                            InertiaMiddleware.WriteErrorsToSession(ctx, new Dictionary<string, string[]>
                            {
                                ["email"] = ["Required", "Must be valid"],
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

        var response = await client.GetAsync("/form");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var emailError = doc.RootElement
            .GetProperty("props")
            .GetProperty("errors")
            .GetProperty("email");

        emailError.ValueKind.Should().Be(JsonValueKind.String);
        emailError.GetString().Should().Be("Required");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IHost SimpleHost(string path, string method, Action<HttpContext> handler,
        Action<InertiaOptions>? configure = null)
    {
        return new HostBuilder().ConfigureWebHost(web =>
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

    private static HttpClient InertiaClient(IHost host)
    {
        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        return client;
    }
}

/// <summary>Wraps TestServer's handler to persist cookies across requests.</summary>
internal sealed class CookieContainerHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly System.Net.CookieContainer _cookies = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri!;
        var cookieHeader = _cookies.GetCookieHeader(uri);
        if (!string.IsNullOrEmpty(cookieHeader))
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

        var response = await base.SendAsync(request, ct);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            foreach (var c in setCookies)
                _cookies.SetCookies(uri, c);

        return response;
    }
}
