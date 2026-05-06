using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InertiaKit.E2E.Mvc.Tests;

/// <summary>
/// End-to-end tests for the MVC example.
/// Spins up the full ASP.NET Core MVC application and verifies Inertia protocol
/// behaviour through HTTP assertions on status codes, headers, and page objects.
/// </summary>
public class MvcE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CurrentVersion = "1.2.0";
    private readonly WebApplicationFactory<Program> _factory;

    private static void AssertGuestAuth(JsonElement auth)
    {
        if (auth.TryGetProperty("user", out var user))
        {
            user.ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

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

    private static void AddInertiaHeaders(HttpRequestMessage req, string version = CurrentVersion,
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

    private static HttpRequestMessage CreateInertiaRequest(HttpMethod method, string path,
        HttpContent? content = null, string version = CurrentVersion,
        string? partialComponent = null, string? partialData = null, string? exceptOnceProps = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = content,
        };

        AddInertiaHeaders(request, version, partialComponent, partialData, exceptOnceProps);
        return request;
    }

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
        page.RootElement.GetProperty("version").GetString().Should().Be(CurrentVersion);
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
        users.GetArrayLength().Should().Be(4);
        users[0].GetProperty("name").GetString().Should().Be("Maya Chen");
        props.GetProperty("total").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task GET_users_includes_shared_auth_prop_with_guest_user_on_first_visit()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/users");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        var auth = page.RootElement.GetProperty("props").GetProperty("auth");
        AssertGuestAuth(auth);
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
    public async Task POST_users_with_empty_name_returns_422_with_errors_in_props()
    {
        var client = InertiaClient();
        var req = CreateInertiaRequest(HttpMethod.Post, "/users",
            JsonContent.Create(new { Name = "", Email = "test@example.com" }));

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        page.RootElement.GetProperty("component").GetString().Should().Be("Users/Create");
        page.RootElement.GetProperty("props")
            .GetProperty("errors")
            .GetProperty("name")
            .GetString().Should().Be("Name is required.");
    }

    [Fact]
    public async Task POST_users_with_valid_name_redirects_to_index_with_flash_message()
    {
        var client = InertiaClient();
        var req = CreateInertiaRequest(HttpMethod.Post, "/users",
            JsonContent.Create(new { Name = "Charlie", Email = "charlie@example.com" }));

        var response = await client.SendAsync(req);

        ((int)response.StatusCode).Should().BeOneOf(302, 303);
        response.Headers.Location?.ToString().Should().StartWith("/users?success=");
    }

    // ── Dashboard — deferred + merge props ───────────────────────────────────

    [Fact]
    public async Task GET_dashboard_excludes_deferred_monthlyChart_from_initial_props()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("monthlyChart", out _).Should().BeFalse("monthlyChart is deferred");
        page.RootElement.GetProperty("props")
            .TryGetProperty("topUsers", out var topUsers).Should().BeTrue();
        topUsers.GetArrayLength().Should().Be(3);
        page.RootElement.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        page.RootElement.TryGetProperty("deferredProps", out var deferred).Should().BeTrue();
        deferred.GetProperty("charts").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("monthlyChart");
    }

    [Fact]
    public async Task GET_dashboard_partial_reload_fetches_deferred_monthlyChart()
    {
        var client = InertiaClient();
        var req = CreateInertiaRequest(HttpMethod.Get, "/dashboard",
            partialComponent: "Dashboard/Index",
            partialData: "monthlyChart");

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("monthlyChart", out _).Should().BeTrue();
        page.RootElement.GetProperty("props")
            .TryGetProperty("summary", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GET_dashboard_partial_reload_excludes_optional_topUsers_when_not_requested()
    {
        var client = InertiaClient();
        var req = CreateInertiaRequest(HttpMethod.Get, "/dashboard",
            partialComponent: "Dashboard/Index",
            partialData: "summary");

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.GetProperty("props")
            .TryGetProperty("summary", out _).Should().BeTrue();
        page.RootElement.GetProperty("props")
            .TryGetProperty("topUsers", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GET_dashboard_annotates_recentActivity_as_merge_prop()
    {
        var client = InertiaClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        AddInertiaHeaders(req);

        var response = await client.SendAsync(req);
        var page = await ParsePage(response);

        page.RootElement.TryGetProperty("mergeProps", out var mergeProps).Should().BeTrue();
        mergeProps.EnumerateArray().Select(e => e.GetString()).Should().Contain("recentActivity");
    }

    // ── Auth flows ───────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_demo_sign_in_redirects_to_authenticated_page_and_sets_shared_user()
    {
        var client = InertiaClient();
        var signInRequest = CreateInertiaRequest(HttpMethod.Post, "/auth/demo-sign-in");

        var signIn = await client.SendAsync(signInRequest);

        ((int)signIn.StatusCode).Should().Be(303);
        signIn.Headers.Location?.ToString().Should().Be("/me");

        var accountRequest = CreateInertiaRequest(HttpMethod.Get, "/me");
        var accountResponse = await client.SendAsync(accountRequest);
        var page = await ParsePage(accountResponse);
        var authUser = page.RootElement.GetProperty("props").GetProperty("auth").GetProperty("user");
        var profile = page.RootElement.GetProperty("props").GetProperty("profile");

        page.RootElement.GetProperty("component").GetString().Should().Be("Account/Show");
        page.RootElement.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        authUser.GetProperty("name").GetString().Should().Be("Maya Chen");
        profile.GetProperty("email").GetString().Should().Be("maya.chen@northstar.example");
        profile.GetProperty("role").GetString().Should().Be("Launch Director");
    }

    [Fact]
    public async Task POST_demo_sign_out_clears_shared_user_and_rotates_history()
    {
        var client = InertiaClient();

        await client.SendAsync(CreateInertiaRequest(HttpMethod.Post, "/auth/demo-sign-in"));
        var signOut = await client.SendAsync(CreateInertiaRequest(HttpMethod.Post, "/auth/demo-sign-out"));

        ((int)signOut.StatusCode).Should().Be(303);
        signOut.Headers.Location?.ToString().Should().Be("/signed-out");

        var signedOutResponse = await client.SendAsync(CreateInertiaRequest(HttpMethod.Get, "/signed-out"));
        var page = await ParsePage(signedOutResponse);

        page.RootElement.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
        page.RootElement.GetProperty("props").GetProperty("greeting").GetString()
            .Should().Be("You have signed out of the workspace");
        AssertGuestAuth(page.RootElement.GetProperty("props").GetProperty("auth"));
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
