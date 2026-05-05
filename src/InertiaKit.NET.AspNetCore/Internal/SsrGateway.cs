using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace InertiaKit.AspNetCore.Internal;

/// <summary>
/// Calls the Node.js SSR gateway and returns rendered HTML.
/// Falls back gracefully when the gateway is unavailable.
/// </summary>
/// <remarks>
/// <para><strong>Trust model:</strong> the SSR gateway is fully trusted. The
/// <c>html</c> and <c>head</c> fragments it returns are injected directly into
/// the HTML response without further escaping. Ensure the gateway is reachable
/// only over a trusted loopback or internal network connection (never exposed
/// to the public internet) and that TLS certificate validation is enforced when
/// the connection traverses any untrusted network segment.</para>
/// <para>Configure the gateway URL via <see cref="InertiaOptions.SsrUrl"/> and
/// restrict external access at the network/firewall level.</para>
/// </remarks>
internal sealed class SsrGateway(HttpClient http, ILogger<SsrGateway> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Sends the page object JSON to the SSR gateway and returns the rendered
    /// <c>html</c> and optional <c>head</c> fragments.
    /// Returns <c>null</c> on any failure so the caller falls back to an empty div.
    /// </summary>
    public async Task<SsrResult?> RenderAsync(string pageJson, CancellationToken ct = default)
    {
        try
        {
            using var content = new StringContent(pageJson, System.Text.Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(string.Empty, content, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SsrResult>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SSR gateway unavailable; falling back to client-side rendering");
            return null;
        }
    }
}

internal sealed class SsrResult
{
    [JsonPropertyName("html")]
    public string Html { get; init; } = string.Empty;

    [JsonPropertyName("head")]
    public string[]? Head { get; init; }
}
