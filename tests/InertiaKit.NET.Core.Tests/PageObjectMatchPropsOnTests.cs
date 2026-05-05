using System.Text.Json;
using FluentAssertions;
using InertiaKit.Core;
using InertiaKit.Core.Props;
using InertiaKit.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.Core.Tests;

/// <summary>
/// TDD tests written BEFORE the fix for:
/// - matchPropsOn must serialise as a JSON array (string[]) per the Inertia protocol,
///   not as a dictionary object.
/// </summary>
public class PageObjectMatchPropsOnTests
{
    private static IServiceProvider Services =>
        new ServiceCollection().BuildServiceProvider();

    [Fact]
    public void MatchPropsOn_is_IReadOnlyList_not_dictionary()
    {
        var page = new PageObject
        {
            Component = "Test",
            Props = new Dictionary<string, object?>(),
            Url = "/",
            Version = "1",
            MatchPropsOn = new[] { "id" },
        };

        page.MatchPropsOn.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void PageObjectBuilder_produces_matchPropsOn_as_array_of_field_names()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = MergeProp.Append(new[] { "u" }).MatchOn("id"),
            ["posts"] = MergeProp.Append(new[] { "p" }).MatchOn("slug"),
        };

        var resolver = new PropResolver(Services);
        var resolved = resolver.Resolve(raw, false, null, null);
        var page = PageObjectBuilder.Build("Test", resolved, "/", "1");

        // Must be a list, not a dictionary
        page.MatchPropsOn.Should().NotBeNull();
        page.MatchPropsOn.Should().BeAssignableTo<IReadOnlyList<string>>();
        page.MatchPropsOn!.Should().Contain("id");
        page.MatchPropsOn!.Should().Contain("slug");
    }

    [Fact]
    public void MatchPropsOn_serialises_to_json_array_not_object()
    {
        var page = new PageObject
        {
            Component = "Test",
            Props = new Dictionary<string, object?>(),
            Url = "/",
            Version = "1",
            MergeProps = ["users"],
            MatchPropsOn = new[] { "id" },
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var json = JsonSerializer.Serialize(page, options);
        var doc = JsonDocument.Parse(json);

        var matchPropsOn = doc.RootElement.GetProperty("matchPropsOn");

        // Must be an array, not an object
        matchPropsOn.ValueKind.Should().Be(JsonValueKind.Array);
        matchPropsOn.EnumerateArray()
            .Select(e => e.GetString())
            .Should().Contain("id");
    }

    [Fact]
    public void MatchPropsOn_is_null_when_no_merge_props_use_MatchOn()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = MergeProp.Append(new[] { "u" }), // no MatchOn
        };

        var resolver = new PropResolver(Services);
        var resolved = resolver.Resolve(raw, false, null, null);
        var page = PageObjectBuilder.Build("Test", resolved, "/", "1");

        page.MatchPropsOn.Should().BeNull();
    }
}
