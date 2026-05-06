using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.E2E.Mvc.Tests;

/// <summary>
/// End-to-end tests for the MVC example.
/// Spins up the full ASP.NET Core MVC application and verifies Inertia protocol
/// behaviour through HTTP assertions on status codes, headers, and page objects.
/// </summary>
public class MvcE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MvcE2ETests(WebApplicationFactory<Program> factory)
    {
        // Override environment so the app doesn't try HTTPS redirection
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));
    }

    private HttpClient InertiaClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            // Use HTTP — the test server does not enforce HTTPS
            BaseAddress = new Uri("http://localhost"),
        });

    private HttpClient PlainClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

    private static void AddInertiaHeaders(HttpRequestMessage req, string version = "1.0.0",
        string? partialComponent = null, string? partialData = null, string? exceptOnceProps = null)
    {
        req.Headers.Add("X-Inertia", "true");
        req.Headers.Add("X-Inertia-Version", version);
        if (partialComponent != null) req.Headers.Add("X-Inertia-Partial-Component", partialComponent);
        if (partialData     != null) req.Headers.Add("X-Inertia-Partial-Data",      partialData);
        if (exceptOnceProps != null) req.Headers.Add("X-Inertia-Except-Once-Props", exceptOnceProps);
    }

    private static async Task<JsonDocument> ParsePage(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync());

    private static async Task<string> ReadHtml(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync();

    // ── Initial HTML render ─────────────────────────────────────────────────

    [Fact]
    public async Task GET_root_without_inertia_header_returns_html_with_embedded_page_object()
    {
        var client = PlainClient();

        var response = await client.GetAsync("/");
        var html = await ReadHtml(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        response.Headers.Vary.Should().Contain("X-Inertia");
        response.Headers.Contains("X-Inertia").Should().BeFalse();
        html.Should().Contain("id=\"app-data\"");
        html.Should().Contain("Home/Index");
    }

    // ── Users/Index ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_users_returns_json_with_correct_component()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        page.RootElement.GetProperty("component").GetString().Should().Be("Users/Index");
        page.RootElement.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    [Fact]
    public async Task GET_users_includes_eager_users_prop()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);
        var props = page.RootElement.GetProperty("props");

        props.TryGetProperty("users", out var users).Should().BeTrue();
        users.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GET_users_includes_always_errors_prop_even_on_partial_reload()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req, partialComponent: "Users/Index", partialData: "users");

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);
        var props = page.RootElement.GetProperty("props");

        // AlwaysProp: errors present even though not in partialData
        props.TryGetProperty("errors", out _).Should().BeTrue();
        // users requested — present
        props.TryGetProperty("users", out _).Should().BeTrue();
        // permissions not requested and Optional — absent
        props.TryGetProperty("permissions", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GET_users_includes_shared_auth_prop()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("auth", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_users_includes_appConfig_once_prop_on_first_visit()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("appConfig", out _).Should().BeTrue();
        page.RootElement.GetProperty("onceProps")
            .GetProperty("appConfig")
            .GetProperty("prop")
            .GetString().Should().Be("appConfig");
    }

    [Fact]
    public async Task GET_users_omits_appConfig_value_when_client_already_has_once_prop()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req, exceptOnceProps: "appConfig");

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("appConfig", out _).Should().BeFalse();
        page.RootElement.GetProperty("onceProps")
            .GetProperty("appConfig")
            .GetProperty("prop")
            .GetString().Should().Be("appConfig");
    }

    // ── Users/Create ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_users_create_returns_Users_Create_component()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users/create");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("component").GetString().Should().Be("Users/Create");
    }

    // ── POST /users — PRG pattern ─────────────────────────────────────────────

    [Fact]
    public async Task POST_users_with_empty_name_redirects_303_back_to_create()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Post, "/users");
        AddInertiaHeaders(req);
        req.Content = new FormUrlEncodedContent(
        [
            new("Name", ""),
            new("Email", "test@example.com"),
        ]);

        var response = await client.SendAsync(req);

        // MVC example uses PRG: redirect back to the create form
        ((int)response.StatusCode).Should().BeOneOf(302, 303);
        response.Headers.Location?.ToString().Should().Contain("create");
    }

    [Fact]
    public async Task POST_users_with_valid_name_redirects_to_index()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Post, "/users");
        AddInertiaHeaders(req);
        req.Content = new FormUrlEncodedContent(
        [
            new("Name", "Charlie"),
            new("Email", "charlie@example.com"),
        ]);

        var response = await client.SendAsync(req);

        ((int)response.StatusCode).Should().BeOneOf(302, 303);
        response.Headers.Location?.ToString().Should()
            .ContainEquivalentOf("users", Exactly.Once());
    }

    // ── Dashboard — deferred + merge props ───────────────────────────────────

    [Fact]
    public async Task GET_users_dashboard_places_recentUsers_in_deferredProps()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users/dashboard");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("recentUsers", out _).Should().BeFalse("recentUsers is deferred");
        page.RootElement.TryGetProperty("deferredProps", out var deferred).Should().BeTrue();
        deferred.GetProperty("sidebar").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("recentUsers");
    }

    [Fact]
    public async Task GET_users_dashboard_annotates_feed_as_merge_prop()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users/dashboard");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.TryGetProperty("mergeProps", out var mergeProps).Should().BeTrue();
        mergeProps.EnumerateArray().Select(e => e.GetString()).Should().Contain("feed");
    }

    // ── Version mismatch ──────────────────────────────────────────────────────

    [Fact]
    public async Task GET_with_stale_version_returns_409()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req, version: "stale");

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.TryGetValues("X-Inertia-Location", out _).Should().BeTrue();
    }

    // ── Vary header ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Vary_x_inertia_present_on_all_responses()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);

        response.Headers.Vary.Should().Contain("X-Inertia");
    }
}
