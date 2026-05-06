using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InertiaKit.E2E.FastEndpoints.Tests;

/// <summary>
/// End-to-end tests for the FastEndpoints example.
/// Verifies the Inertia protocol over the FastEndpoints handler pipeline.
/// </summary>
public class FastEndpointsE2ETests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient InertiaClient(string version = "1.0.0")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", version);
        return client;
    }

    private HttpClient PartialClient(string component, string partialData)
    {
        var client = InertiaClient();
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Component", component);
        client.DefaultRequestHeaders.Add("X-Inertia-Partial-Data", partialData);
        return client;
    }

    private static async Task<JsonDocument> Page(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync());

    // ── Home (no props) ───────────────────────────────────────────────────────

    [Fact]
    public async Task GET_root_returns_Home_Index_component()
    {
        var response = await InertiaClient().GetAsync("/");
        var page = await Page(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Contains("X-Inertia").Should().BeTrue();
        page.RootElement.GetProperty("component").GetString().Should().Be("Home/Index");
        page.RootElement.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    [Fact]
    public async Task GET_root_vary_header_present()
    {
        var response = await InertiaClient().GetAsync("/");

        response.Headers.Vary.Should().Contain("X-Inertia");
    }

    // ── Users — eager, optional, deferred, once props ─────────────────────────

    [Fact]
    public async Task GET_users_includes_eager_users_array()
    {
        var response = await InertiaClient().GetAsync("/users");
        var page = await Page(response);
        var props = page.RootElement.GetProperty("props");

        props.TryGetProperty("users", out var users).Should().BeTrue();
        users.GetArrayLength().Should().Be(2);
        users[0].GetProperty("name").GetString().Should().Be("Alice");
    }

    [Fact]
    public async Task GET_users_includes_optional_permissions_on_full_load()
    {
        // OptionalProp is evaluated and included on full loads
        var response = await InertiaClient().GetAsync("/users");
        var page = await Page(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("permissions", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_users_partial_reload_excludes_optional_permissions_when_not_requested()
    {
        var response = await PartialClient("Users/Index", "users").GetAsync("/users");
        var page = await Page(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("permissions", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GET_users_places_stats_in_deferredProps_on_full_load()
    {
        var response = await InertiaClient().GetAsync("/users");
        var page = await Page(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("stats", out _).Should().BeFalse("stats is deferred");
        page.RootElement.TryGetProperty("deferredProps", out var deferred).Should().BeTrue();
        deferred.GetProperty("default").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("stats");
    }

    [Fact]
    public async Task GET_users_partial_reload_evaluates_deferred_stats_when_requested()
    {
        var response = await PartialClient("Users/Index", "stats").GetAsync("/users");
        var page = await Page(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("stats", out var stats).Should().BeTrue();
        stats.TryGetProperty("total", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_users_includes_countries_in_onceProps_on_first_visit()
    {
        var response = await InertiaClient().GetAsync("/users");
        var page = await Page(response);

        // OnceProp value present on first visit
        page.RootElement.GetProperty("props")
            .TryGetProperty("countries", out _).Should().BeTrue();
        // Key advertised in onceProps with client metadata
        page.RootElement.TryGetProperty("onceProps", out var once).Should().BeTrue();
        once.ValueKind.Should().Be(JsonValueKind.Object);
        once.GetProperty("countries").GetProperty("prop").GetString().Should().Be("countries");
    }

    [Fact]
    public async Task GET_users_omits_countries_value_when_client_already_has_it()
    {
        var client = InertiaClient();
        client.DefaultRequestHeaders.Add("X-Inertia-Except-Once-Props", "countries");

        var response = await client.GetAsync("/users");
        var page = await Page(response);

        // Value absent (client cached it)
        page.RootElement.GetProperty("props")
            .TryGetProperty("countries", out _).Should().BeFalse();
        // Key still in onceProps so client keeps its cached copy
        page.RootElement.TryGetProperty("onceProps", out var once).Should().BeTrue();
        once.ValueKind.Should().Be(JsonValueKind.Object);
        once.GetProperty("countries").GetProperty("prop").GetString().Should().Be("countries");
    }

    // ── POST /users — validation (422) + success (303) ───────────────────────

    [Fact]
    public async Task POST_users_with_empty_name_returns_422_with_errors()
    {
        var response = await InertiaClient().PostAsync("/users",
            JsonContent.Create(new { Name = "", Email = "test@example.com" }));
        var page = await Page(response);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        page.RootElement.GetProperty("component").GetString().Should().Be("Users/Create");
        page.RootElement.GetProperty("props")
            .GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_users_with_valid_name_returns_303()
    {
        var response = await InertiaClient().PostAsync("/users",
            JsonContent.Create(new { Name = "Charlie", Email = "charlie@example.com" }));

        ((int)response.StatusCode).Should().Be(303);
        response.Headers.Location?.ToString().Should().Be("/users");
    }

    // ── Users/Dashboard — merge props with matchPropsOn ───────────────────────

    [Fact]
    public async Task GET_users_dashboard_returns_summary_prop()
    {
        var response = await InertiaClient().GetAsync("/users/dashboard");
        var page = await Page(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("summary", out var summary).Should().BeTrue();
        summary.GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GET_users_dashboard_annotates_activities_with_mergeProps_and_matchPropsOn()
    {
        var response = await InertiaClient().GetAsync("/users/dashboard");
        var page = await Page(response);

        page.RootElement.TryGetProperty("mergeProps", out var mergeProps).Should().BeTrue();
        mergeProps.EnumerateArray().Select(e => e.GetString()).Should().Contain("activities");

        page.RootElement.TryGetProperty("matchPropsOn", out var matchOn).Should().BeTrue();
        matchOn.EnumerateArray().Select(e => e.GetString()).Should().Contain("id");
    }

    // ── Version mismatch ──────────────────────────────────────────────────────

    [Fact]
    public async Task GET_with_stale_version_returns_409()
    {
        var response = await InertiaClient(version: "stale").GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.TryGetValues("X-Inertia-Location", out _).Should().BeTrue();
    }

    // ── Initial HTML render ───────────────────────────────────────────────────

    [Fact]
    public async Task GET_root_without_inertia_header_returns_html_with_embedded_json()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Vary.Should().Contain("X-Inertia");
        response.Headers.Contains("X-Inertia").Should().BeFalse();
        html.Should().Contain("/fastendpoints/app.css");
        html.Should().Contain("/fastendpoints/app.js");
        html.Should().Contain("app-data");
        html.Should().Contain("Home/Index");
    }
}
