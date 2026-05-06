using System.Text.Json;
using FluentAssertions;
using InertiaKit.Core;
using InertiaKit.Core.Props;
using InertiaKit.Core.Serialization;

namespace InertiaKit.Core.Tests;

public class PageObjectOncePropsTests
{
    [Fact]
    public void PageObjectBuilder_produces_onceProps_metadata_object()
    {
        var resolved = new ResolvedProps(
            Resolved: new Dictionary<string, object?>
            {
                ["countries"] = new[] { "US", "CA" },
            },
            DeferredGroups: new Dictionary<string, IReadOnlyList<string>>(),
            OnceKeys: new HashSet<string> { "countries" },
            MergeKeys: new Dictionary<MergeStrategy, IReadOnlyList<string>>
            {
                [MergeStrategy.Append] = [],
                [MergeStrategy.Prepend] = [],
                [MergeStrategy.DeepMerge] = [],
            },
            MatchOnFields: new Dictionary<string, string>());

        var page = PageObjectBuilder.Build("Users/Index", resolved, "/users", "1.0.0");

        page.OnceProps.Should().NotBeNull();
        page.OnceProps!.Should().ContainKey("countries");
        page.OnceProps["countries"].Prop.Should().Be("countries");
        page.OnceProps["countries"].ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void OnceProps_serialise_to_json_object_not_array()
    {
        var page = new PageObject
        {
            Component = "Users/Index",
            Props = new Dictionary<string, object?>
            {
                ["countries"] = new[] { "US", "CA" },
            },
            Url = "/users",
            Version = "1.0.0",
            OnceProps = new Dictionary<string, OncePropMetadata>
            {
                ["countries"] = new("countries"),
            },
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(page, options);
        var doc = JsonDocument.Parse(json);

        var onceProps = doc.RootElement.GetProperty("onceProps");

        onceProps.ValueKind.Should().Be(JsonValueKind.Object);
        onceProps.GetProperty("countries").GetProperty("prop").GetString().Should().Be("countries");
    }
}