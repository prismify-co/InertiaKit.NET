using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace InertiaKit.AspNetCore.Internal;

internal sealed class SystemTextJsonInertiaSerializer(ILogger<SystemTextJsonInertiaSerializer> logger)
    : IInertiaSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        // Prevent infinite loops from circular object graphs (e.g. EF Core nav properties)
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        // Avoid emitting sensitive reflection-based data on unknown types
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        // JavaScriptEncoder.Default encodes < > & ' " so that the JSON is safe to embed
        // directly inside an HTML <script> tag without </script> breakout risk.
        Encoder = JavaScriptEncoder.Default,
    };

    public string Serialize(PageObject page)
    {
        try
        {
            return JsonSerializer.Serialize(page, Options);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to serialize Inertia page object for component '{Component}'", page.Component);
            var fallback = new PageObject
            {
                Component = page.Component,
                Props = new Dictionary<string, object?> { ["_serializationError"] = true },
                Url = page.Url,
                Version = page.Version,
            };
            return JsonSerializer.Serialize(fallback, Options);
        }
    }
}
