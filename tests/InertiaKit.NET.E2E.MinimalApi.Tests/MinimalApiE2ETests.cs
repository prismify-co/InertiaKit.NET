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
    private const string CurrentVersion = "1.2.0";

    private static void AssertGuestAuth(JsonElement auth)
    {
        if (auth.TryGetProperty("user", out var user))
        {
            user.ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

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
        page.RootElement.GetProperty("version").GetString().Should().Be(CurrentVersion);
    }

    [Fact]
    public async Task Initial_html_embeds_shared_props_in_page_object()
    {
        var response = await Client.GetHtml("/users");
        var page = await InertiaE2EClient.ExtractEmbeddedPageObject(response);
        var props = page.RootElement.GetProperty("props");

        // Shared: auth prop
        props.TryGetProperty("auth", out _).Should().BeTrue();
        // Shared: appConfig available in props for the first visit
        props.TryGetProperty("appConfig", out _).Should().BeTrue();
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
        page.RootElement.GetProperty("version").GetString().Should().Be(CurrentVersion);
    }

    [Fact]
    public async Task GET_users_includes_eager_users_prop()
    {
        var response = await Client.GetInertia("/users");
        var page = await InertiaE2EClient.ParsePageObject(response);
        var props = page.RootElement.GetProperty("props");

        props.TryGetProperty("users", out var users).Should().BeTrue();
        users.GetArrayLength().Should().Be(4);
        users[0].GetProperty("name").GetString().Should().Be("Maya Chen");
        props.GetProperty("total").GetInt32().Should().Be(4);
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
    public async Task POST_demo_sign_in_redirects_to_authenticated_page_and_sets_shared_user()
    {
        var client = Client;

        var signIn = await client.PostInertia("/auth/demo-sign-in");

        ((int)signIn.StatusCode).Should().Be(303);
        signIn.Headers.Location?.ToString().Should().Be("/me");

        var accountResponse = await client.GetInertia("/me");
        var page = await InertiaE2EClient.ParsePageObject(accountResponse);
        var user = page.RootElement.GetProperty("props").GetProperty("auth").GetProperty("user");

        page.RootElement.GetProperty("component").GetString().Should().Be("Account/Show");
        page.RootElement.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        user.GetProperty("name").GetString().Should().Be("Maya Chen");
        user.GetProperty("email").GetString().Should().Be("maya.chen@northstar.example");
        user.GetProperty("role").GetString().Should().Be("Launch Director");
    }

    [Fact]
    public async Task POST_demo_sign_out_clears_authenticated_user_for_follow_up_requests()
    {
        var client = Client;

        await client.PostInertia("/auth/demo-sign-in");
        var signOut = await client.PostInertia("/auth/demo-sign-out");

        ((int)signOut.StatusCode).Should().Be(303);
        signOut.Headers.Location?.ToString().Should().Be("/signed-out");

        var response = await client.GetInertia("/");
        var page = await InertiaE2EClient.ParsePageObject(response);
        var auth = page.RootElement.GetProperty("props").GetProperty("auth");

        AssertGuestAuth(auth);
    }

    [Fact]
    public async Task GET_users_includes_appConfig_once_prop_on_first_visit()
    {
        var response = await Client.GetInertia("/users");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("appConfig", out _).Should().BeTrue();
        page.RootElement.GetProperty("onceProps")
            .GetProperty("appConfig")
            .GetProperty("prop")
            .GetString().Should().Be("appConfig");
    }

    [Fact]
    public async Task GET_nested_docs_route_uses_optional_catch_all_component()
    {
        var response = await Client.GetInertia("/docs/guides/getting-started");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("component").GetString().Should().Be("Docs/[[...page]]");
        page.RootElement.GetProperty("props").GetProperty("slug").GetString().Should().Be("guides/getting-started");
        page.RootElement.GetProperty("props").GetProperty("segments").EnumerateArray()
            .Select(item => item.GetString())
            .Should().Equal("guides", "getting-started");
    }

    [Fact]
    public async Task GET_users_omits_appConfig_value_when_client_already_has_once_prop()
    {
        var response = await Client.GetInertia("/users", exceptOnceProps: "appConfig");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("appConfig", out _).Should().BeFalse();
        page.RootElement.GetProperty("onceProps")
            .GetProperty("appConfig")
            .GetProperty("prop")
            .GetString().Should().Be("appConfig");
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

    [Fact]
    public async Task GET_signed_out_rotates_history_for_logout_flow()
    {
        var response = await Client.GetInertia("/signed-out");
        var page = await InertiaE2EClient.ParsePageObject(response);

        page.RootElement.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
        page.RootElement.GetProperty("props").GetProperty("greeting").GetString().Should().Be("You have signed out of the workspace");
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
        var response = await Client.GetInertia("/", version: CurrentVersion);

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
        response.Headers.Location?.ToString().Should().StartWith("/users?success=");
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
