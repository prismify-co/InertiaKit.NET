using FluentAssertions;
using InertiaKit.Core.Props;
using InertiaKit.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.Core.Tests;

/// <summary>
/// Tests covering security and correctness fixes: factory exception recovery,
/// resetProps stripping merge annotation, matchPropsOn per-key structure.
/// </summary>
public class PropResolverSecurityTests
{
    private static IServiceProvider Services =>
        new ServiceCollection().BuildServiceProvider();

    private static PropResolver Resolver => new(Services);

    // ── Factory exception recovery ────────────────────────────────────────────

    [Fact]
    public void Faulting_optional_prop_factory_returns_null_and_does_not_throw()
    {
        var raw = new Dictionary<string, object?>
        {
            ["good"] = "ok",
            ["bad"]  = OptionalProp.From(() => throw new InvalidOperationException("db down")),
        };

        var act = () => Resolver.Resolve(raw, isPartialReload: false, null, null);

        act.Should().NotThrow();
        var result = act();
        result.Resolved["good"].Should().Be("ok");
        result.Resolved["bad"].Should().BeNull();
    }

    [Fact]
    public void Faulting_always_prop_factory_returns_null_and_does_not_throw()
    {
        var raw = new Dictionary<string, object?>
        {
            ["errors"] = AlwaysProp.From(() => throw new Exception("session gone")),
        };

        var act = () => Resolver.Resolve(raw, isPartialReload: false, null, null);
        act.Should().NotThrow();
        act().Resolved["errors"].Should().BeNull();
    }

    // ── ResetProps strips merge annotation ────────────────────────────────────

    [Fact]
    public void ResetProps_strips_merge_annotation_so_client_replaces_not_merges()
    {
        var raw = new Dictionary<string, object?>
        {
            ["items"] = MergeProp.Append(new[] { "a", "b" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, null, null,
            resetProps: new HashSet<string> { "items" });

        // Value still present
        result.Resolved.Should().ContainKey("items");
        // But NOT in merge keys — client will replace, not append
        result.MergeKeys[MergeStrategy.Append].Should().NotContain("items");
    }

    [Fact]
    public void ResetProps_without_the_key_leaves_merge_annotation_intact()
    {
        var raw = new Dictionary<string, object?>
        {
            ["items"] = MergeProp.Append(new[] { "a" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, null, null,
            resetProps: new HashSet<string> { "other" });

        result.MergeKeys[MergeStrategy.Append].Should().Contain("items");
    }

    // ── matchPropsOn per-key structure ────────────────────────────────────────

    [Fact]
    public void MatchOnFields_maps_each_prop_key_to_its_field_independently()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"] = MergeProp.Append(new[] { "u" }).MatchOn("id"),
            ["posts"] = MergeProp.Append(new[] { "p" }).MatchOn("slug"),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, null, null);

        result.MatchOnFields["users"].Should().Be("id");
        result.MatchOnFields["posts"].Should().Be("slug");
    }

    // ── Once props ────────────────────────────────────────────────────────────

    [Fact]
    public void Once_props_excluded_when_client_already_has_them_but_still_in_once_keys()
    {
        var raw = new Dictionary<string, object?>
        {
            ["countries"] = OnceProp.From(() => new[] { "US" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, null, null,
            clientOnceProps: new HashSet<string> { "countries" });

        result.Resolved.Should().NotContainKey("countries");
        result.OnceKeys.Should().Contain("countries"); // still advertised so client knows to keep its cache
    }
}
