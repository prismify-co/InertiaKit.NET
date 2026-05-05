using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InertiaKit.E2E.MinimalApi.Tests;

/// <summary>
/// End-to-end tests for the MinimalApi example.
/// Each test spins up the full application via WebApplicationFactory and
/// verifies observable HTTP behaviour — status codes, headers, and page object shape.
/// </summary>
public class MinimalApiE2ETests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private InertiaE2EClient Client =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    // ── Initial HTML render (no X-Inertia header) ─────────────────────────────

    [Fact]
    public async Task GET_root_without_inertia_header_returns_html_with_embedded_page_object()
    {
        var response = await Client.GetHtml("/");
        var page = await InertiaE2EClient.ExtractEmbeddedPageObject(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        response.Headers.Vary.Should().Contain("X-Inertia");
        response.Headers.Contains("X-Inertia").Should().BeFalse("X-Inertia must not be set on HTML responses");

        page.RootElement.GetProperty("component").GetString().Should().Be("Home/Index");
        page.RootElement.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    [Fact]
    public async Task Initial_html_embeds_shared_props_in_page_object()
    {
        var response = await Client.GetHtml("/users");
        var page = await InertiaE2EClient.ExtractEmbeddedPageObject(response);
        var props = page.RootElement.GetProperty("props");

        // Shared: auth prop
        props.TryGetProperty("auth", out _).Should().BeTrue();
        // Shared once: appConfig in onceProps list and value in props on first visit
        props.TryGetProperty("appConfig", out _).Should().BeTrue();
        page.RootElement.TryGetProperty("onceProps", out var once).Should().BeTrue();
        once.EnumerateArray().Select(e => e.GetString()).Should().Contain("appConfig");
    }

    // ── Inertia JSON responses ────────────────────────────────────────────────

    [Fact]
    public async Task GET_root_with_inertia_header_returns_json_page_object()
    {
        var response = await Client.GetInertia("/");
        var page = await InertiaE2EClient.ParsePageObject(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Contains("X-Inertia").Should().BeTrue();
        response.Headers.Vary.Should().Contain("X-Inertia");

        page.RootElement.GetProperty("component").GetString().Should().Be("Home/Index");
        page.RootElement.GetProperty("url").GetString().Should().Be("/");
        page.RootElement.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    [Fact]
    public async Task GET_users_includes_eager_users_prop()
    {
        var response = await Client.GetInertia("/users");
        var page = await InertiaE2EClient.ParsePageObject(response);
        var props = page.RootElement.GetProperty("props");

        props.TryGetProperty("users", out var users).Should().BeTrue();
        users.GetArrayLength().Should().Be(2);
        users[0].GetProperty("name").GetString().Should().Be("Alice");
        props.GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GET_users_includes_shared_auth_prop()
    {
        var response = await Client.GetInertia("/users");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("auth", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_users_includes_appConfig_as_once_prop_on_first_request()
    {
        var response = await Client.GetInertia("/users");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("appConfig", out _).Should().BeTrue();
        page.RootElement.TryGetProperty("onceProps", out var once).Should().BeTrue();
        once.EnumerateArray().Select(e => e.GetString()).Should().Contain("appConfig");
    }

    // ── Dashboard — deferred, optional, merge props ───────────────────────────

    [Fact]
    public async Task GET_dashboard_excludes_deferred_monthlyChart_from_initial_props()
    {
        var response = await Client.GetInertia("/dashboard");
        var page = await InertiaE2EClient.ParsePageObject(response);
        var props = page.RootElement.GetProperty("props");

        // Eager: summary present
        props.TryGetProperty("summary", out _).Should().BeTrue();
        // Deferred: monthlyChart absent from props, present in deferredProps
        props.TryGetProperty("monthlyChart", out _).Should().BeFalse();
        page.RootElement.TryGetProperty("deferredProps", out var deferred).Should().BeTrue();
        deferred.GetProperty("charts").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("monthlyChart");
    }

    [Fact]
    public async Task GET_dashboard_partial_reload_fetches_deferred_monthlyChart()
    {
        var response = await Client.GetInertia("/dashboard",
            partialComponent: "Dashboard/Index",
            partialData: "monthlyChart");
        var page = await InertiaE2EClient.ParsePageObject(response);
        var props = page.RootElement.GetProperty("props");

        props.TryGetProperty("monthlyChart", out _).Should().BeTrue();
        // Summary not requested — must be absent
        props.TryGetProperty("summary", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GET_dashboard_includes_optional_topUsers_on_full_load()
    {
        // OptionalProp is evaluated and included on full page loads.
        // It is only excluded during PARTIAL reloads when not explicitly requested.
        var response = await Client.GetInertia("/dashboard");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("topUsers", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_dashboard_partial_reload_excludes_optional_topUsers_when_not_requested()
    {
        // On a partial reload that only requests "summary", topUsers must be absent.
        var response = await Client.GetInertia("/dashboard",
            partialComponent: "Dashboard/Index",
            partialData: "summary");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("topUsers", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GET_dashboard_partial_reload_evaluates_optional_topUsers_when_requested()
    {
        var response = await Client.GetInertia("/dashboard",
            partialComponent: "Dashboard/Index",
            partialData: "topUsers");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("topUsers", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_dashboard_annotates_recentActivity_as_merge_prop()
    {
        var response = await Client.GetInertia("/dashboard");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.TryGetProperty("mergeProps", out var mergeProps).Should().BeTrue();
        mergeProps.EnumerateArray().Select(e => e.GetString()).Should().Contain("recentActivity");
    }

    // ── Version mismatch ──────────────────────────────────────────────────────

    [Fact]
    public async Task GET_with_stale_version_returns_409_conflict()
    {
        var response = await Client.GetInertia("/", version: "old-version");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.TryGetValues("X-Inertia-Location", out var loc).Should().BeTrue();
        loc!.First().Should().Contain("/");
    }

    [Fact]
    public async Task GET_with_matching_version_returns_200()
    {
        var response = await Client.GetInertia("/", version: "1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Validation errors — 422 ───────────────────────────────────────────────

    [Fact]
    public async Task POST_users_with_empty_name_returns_422_with_errors_in_props()
    {
        var response = await Client.PostInertia("/users",
            JsonContent.Create(new { Name = "", Email = "test@example.com" }));
        var page = await InertiaE2EClient.ParsePageObject(response);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        page.RootElement.GetProperty("props")
            .GetProperty("errors")
            .TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_users_with_empty_name_keeps_component_as_Users_Create()
    {
        var response = await Client.PostInertia("/users",
            JsonContent.Create(new { Name = "", Email = "test@example.com" }));
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("component").GetString().Should().Be("Users/Create");
    }

    // ── PRG — successful POST redirects with 303 ──────────────────────────────

    [Fact]
    public async Task POST_users_with_valid_name_returns_303()
    {
        var response = await Client.PostInertia("/users",
            JsonContent.Create(new { Name = "Charlie", Email = "charlie@example.com" }));

        ((int)response.StatusCode).Should().Be(303);
        response.Headers.Location?.ToString().Should().Be("/users");
    }

    // ── External redirect — 409 ───────────────────────────────────────────────

    [Fact]
    public async Task GET_auth_github_returns_409_with_external_inertia_location()
    {
        var response = await Client.GetInertia("/auth/github");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.TryGetValues("X-Inertia-Location", out var loc).Should().BeTrue();
        loc!.First().Should().StartWith("https://github.com");
    }
}
