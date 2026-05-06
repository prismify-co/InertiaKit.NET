using System.Text.Json;

namespace InertiaKit.E2E.MinimalApi.Tests;

/// <summary>
/// Thin HTTP client wrapper for Inertia E2E scenarios.
/// Sends requests with the correct Inertia headers and parses page objects.
/// </summary>
internal sealed class InertiaE2EClient(HttpClient http)
{
    private const string CurrentVersion = "1.2.0";

    private static readonly JsonDocumentOptions JsonOpts = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
    };

    // ── Request helpers ───────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetInertia(string url, string version = CurrentVersion,
        string? partialComponent = null, string? partialData = null, string? partialExcept = null,
        string? exceptOnceProps = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Inertia", "true");
        req.Headers.Add("X-Inertia-Version", version);
        if (partialComponent is not null) req.Headers.Add("X-Inertia-Partial-Component", partialComponent);
        if (partialData    is not null) req.Headers.Add("X-Inertia-Partial-Data",      partialData);
        if (partialExcept  is not null) req.Headers.Add("X-Inertia-Partial-Except",    partialExcept);
        if (exceptOnceProps is not null) req.Headers.Add("X-Inertia-Except-Once-Props", exceptOnceProps);
        return http.SendAsync(req);
    }

    public Task<HttpResponseMessage> PostInertia(string url, HttpContent? body = null, string version = CurrentVersion)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = body };
        req.Headers.Add("X-Inertia", "true");
        req.Headers.Add("X-Inertia-Version", version);
        return http.SendAsync(req);
    }

    public Task<HttpResponseMessage> GetHtml(string url) =>
        http.GetAsync(url); // no X-Inertia header → initial HTML render

    // ── Response helpers ──────────────────────────────────────────────────────

    public static async Task<JsonDocument> ParsePageObject(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body, JsonOpts);
    }

    /// <summary>
    /// Extracts the page object from an initial HTML response by reading the
    /// embedded <c>&lt;script type="application/json" id="app-data"&gt;</c> tag.
    /// </summary>
    public static async Task<JsonDocument> ExtractEmbeddedPageObject(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        const string start = @"id=""app-data"">";
        const string end   = "</script>";
        var startIdx = html.IndexOf(start, StringComparison.Ordinal);
        if (startIdx < 0) throw new InvalidOperationException("No app-data script tag found in HTML.");
        startIdx += start.Length;
        var endIdx = html.IndexOf(end, startIdx, StringComparison.Ordinal);
        var json = html[startIdx..endIdx];
        return JsonDocument.Parse(json, JsonOpts);
    }
}
