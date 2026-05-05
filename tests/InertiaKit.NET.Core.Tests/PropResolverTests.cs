using FluentAssertions;
using InertiaKit.Core.Props;
using InertiaKit.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.Core.Tests;

public class PropResolverTests
{
    private static IServiceProvider Services =>
        new ServiceCollection().BuildServiceProvider();

    private static PropResolver Resolver => new(Services);

    // ── Eager props ───────────────────────────────────────────────────────

    [Fact]
    public void Eager_props_are_always_included()
    {
        var raw = new Dictionary<string, object?> { ["name"] = "Alice" };

        var result = Resolver.Resolve(raw, isPartialReload: false, partialOnly: null, partialExcept: null);

        result.Resolved["name"].Should().Be("Alice");
    }

    // ── Optional props ────────────────────────────────────────────────────

    [Fact]
    public void Optional_props_are_included_on_full_load()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = OptionalProp.From(() => new[] { "Alice" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, partialOnly: null, partialExcept: null);

        result.Resolved["users"].Should().BeEquivalentTo(new[] { "Alice" });
    }

    [Fact]
    public void Optional_props_are_excluded_from_partial_reload_if_not_requested()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = OptionalProp.From(() => new[] { "Alice" }),
            ["count"] = 5,
        };

        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "count" }, partialExcept: null);

        result.Resolved.Should().ContainKey("count");
        result.Resolved.Should().NotContainKey("users");
    }

    [Fact]
    public void Optional_props_are_included_in_partial_reload_when_requested()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = OptionalProp.From(() => new[] { "Bob" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "users" }, partialExcept: null);

        result.Resolved["users"].Should().BeEquivalentTo(new[] { "Bob" });
    }

    // ── Always props ──────────────────────────────────────────────────────

    [Fact]
    public void Always_props_are_included_even_during_partial_reload()
    {
        var raw = new Dictionary<string, object?>
        {
            ["count"] = 5,
            ["errors"] = AlwaysProp.From(new Dictionary<string, string> { ["email"] = "required" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "count" }, partialExcept: null);

        result.Resolved.Should().ContainKey("errors");
        result.Resolved.Should().ContainKey("count");
    }

    // ── Deferred props ────────────────────────────────────────────────────

    [Fact]
    public void Deferred_props_are_excluded_from_resolved_and_placed_in_deferred_groups()
    {
        var raw = new Dictionary<string, object?>
        {
            ["summary"] = "ok",
            ["report"] = DeferredProp.From(() => new[] { 1, 2, 3 }, "default"),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, partialOnly: null, partialExcept: null);

        result.Resolved.Should().ContainKey("summary");
        result.Resolved.Should().NotContainKey("report");
        result.DeferredGroups["default"].Should().Contain("report");
    }

    // ── Once props ────────────────────────────────────────────────────────

    [Fact]
    public void Once_props_are_included_on_first_visit_and_marked_in_once_keys()
    {
        var raw = new Dictionary<string, object?>
        {
            ["countries"] = OnceProp.From(() => new[] { "US", "CA" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false,
            partialOnly: null, partialExcept: null, clientOnceProps: null);

        result.Resolved["countries"].Should().BeEquivalentTo(new[] { "US", "CA" });
        result.OnceKeys.Should().Contain("countries");
    }

    [Fact]
    public void Once_props_are_omitted_when_client_already_has_them()
    {
        var raw = new Dictionary<string, object?>
        {
            ["countries"] = OnceProp.From(() => new[] { "US", "CA" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false,
            partialOnly: null, partialExcept: null,
            clientOnceProps: new HashSet<string> { "countries" });

        result.Resolved.Should().NotContainKey("countries");
        result.OnceKeys.Should().Contain("countries");
    }

    // ── Merge props ───────────────────────────────────────────────────────

    [Fact]
    public void Merge_props_are_resolved_and_recorded_in_merge_keys()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = MergeProp.Append(new[] { "Eve" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, partialOnly: null, partialExcept: null);

        result.Resolved["users"].Should().BeEquivalentTo(new[] { "Eve" });
        result.MergeKeys[MergeStrategy.Append].Should().Contain("users");
    }

    // ── Partial-Except ────────────────────────────────────────────────────

    [Fact]
    public void Partial_except_excludes_listed_props()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = new[] { "Alice" },
            ["posts"] = new[] { "Post1" },
        };

        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: null, partialExcept: new HashSet<string> { "posts" });

        result.Resolved.Should().ContainKey("users");
        result.Resolved.Should().NotContainKey("posts");
    }
}
