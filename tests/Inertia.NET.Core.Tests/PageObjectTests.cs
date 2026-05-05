using FluentAssertions;
using Inertia.NET.Core;

namespace Inertia.NET.Core.Tests;

public class PageObjectTests
{
    private static PageObject Minimal() => new()
    {
        Component = "Users/Index",
        Props = new Dictionary<string, object?> { ["count"] = 5 },
        Url = "/users",
        Version = "abc123",
    };

    [Fact]
    public void Required_fields_are_accessible()
    {
        var page = Minimal();

        page.Component.Should().Be("Users/Index");
        page.Url.Should().Be("/users");
        page.Version.Should().Be("abc123");
        page.Props["count"].Should().Be(5);
    }

    [Fact]
    public void Optional_fields_default_to_null()
    {
        var page = Minimal();

        page.MergeProps.Should().BeNull();
        page.PrependProps.Should().BeNull();
        page.DeepMergeProps.Should().BeNull();
        page.DeferredProps.Should().BeNull();
        page.OnceProps.Should().BeNull();
        page.EncryptHistory.Should().BeNull();
        page.ClearHistory.Should().BeNull();
    }

    [Fact]
    public void With_expression_produces_modified_copy_leaving_original_intact()
    {
        var original = Minimal();
        var copy = original with { Component = "Users/Show" };

        copy.Component.Should().Be("Users/Show");
        original.Component.Should().Be("Users/Index");
    }

    [Fact]
    public void MergeProps_can_be_set()
    {
        var page = Minimal() with { MergeProps = ["users"] };

        page.MergeProps.Should().ContainSingle().Which.Should().Be("users");
    }

    [Fact]
    public void DeferredProps_groups_are_preserved()
    {
        var deferred = new Dictionary<string, IReadOnlyList<string>>
        {
            ["default"] = ["heavyReport"],
            ["sidebar"] = ["categories"],
        };
        var page = Minimal() with { DeferredProps = deferred };

        page.DeferredProps!["sidebar"].Should().ContainSingle().Which.Should().Be("categories");
    }

    [Fact]
    public void EncryptHistory_flag_is_preserved()
    {
        var page = Minimal() with { EncryptHistory = true };

        page.EncryptHistory.Should().BeTrue();
    }
}
