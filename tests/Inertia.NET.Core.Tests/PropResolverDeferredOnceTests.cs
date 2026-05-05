using FluentAssertions;
using Inertia.NET.Core.Props;
using Inertia.NET.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.NET.Core.Tests;

/// <summary>
/// TDD tests written BEFORE the fixes for:
/// - DeferredProp evaluation on follow-up partial reload (Bug #1)
/// - OnceProp onceKeys guard on partial reload (Bug #4)
/// </summary>
public class PropResolverDeferredOnceTests
{
    private static IServiceProvider Services =>
        new ServiceCollection().BuildServiceProvider();

    private static PropResolver Resolver => new(Services);

    // ── DeferredProp — initial load ───────────────────────────────────────────

    [Fact]
    public void Deferred_prop_is_excluded_from_resolved_on_initial_full_load()
    {
        var raw = new Dictionary<string, object?>
        {
            ["summary"] = "ok",
            ["report"]  = DeferredProp.From(() => new[] { 1, 2 }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false, null, null);

        result.Resolved.Should().ContainKey("summary");
        result.Resolved.Should().NotContainKey("report");
        result.DeferredGroups["default"].Should().Contain("report");
    }

    // ── DeferredProp — follow-up partial reload ───────────────────────────────

    [Fact]
    public void Deferred_prop_is_evaluated_and_included_when_requested_via_partial_reload()
    {
        var raw = new Dictionary<string, object?>
        {
            ["summary"] = "ok",
            ["report"]  = DeferredProp.From(() => new[] { 1, 2 }),
        };

        // Client fires follow-up: X-Inertia-Partial-Data: report
        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "report" }, partialExcept: null);

        result.Resolved.Should().ContainKey("report");
        result.Resolved["report"].Should().BeEquivalentTo(new[] { 1, 2 });
        // When the prop is delivered, it must NOT appear in deferredGroups
        result.DeferredGroups.Should().NotContainKey("default");
    }

    [Fact]
    public void Deferred_prop_is_omitted_entirely_when_not_requested_in_partial_reload()
    {
        // On a partial reload the client already knows which props are deferred from
        // the initial response. If it doesn't request a deferred key, the server
        // omits it entirely — no re-advertising in deferredGroups needed.
        var raw = new Dictionary<string, object?>
        {
            ["summary"] = "ok",
            ["report"]  = DeferredProp.From(() => new[] { 1, 2 }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "summary" }, partialExcept: null);

        result.Resolved.Should().NotContainKey("report");
        result.DeferredGroups.Should().NotContainKey("default");
    }

    [Fact]
    public void Multiple_deferred_groups_evaluated_independently()
    {
        var raw = new Dictionary<string, object?>
        {
            ["chartA"] = DeferredProp.From(() => "dataA", "charts"),
            ["chartB"] = DeferredProp.From(() => "dataB", "charts"),
            ["sidebar"] = DeferredProp.From(() => "sideData", "sidebar"),
        };

        // Client requests only the charts group
        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "chartA", "chartB" }, partialExcept: null);

        result.Resolved["chartA"].Should().Be("dataA");
        result.Resolved["chartB"].Should().Be("dataB");
        // "sidebar" was not requested in this partial reload — omitted entirely
        result.Resolved.Should().NotContainKey("sidebar");
        result.DeferredGroups.Should().NotContainKey("sidebar");
    }

    // ── OnceProp — onceKeys guard on partial reload ───────────────────────────

    [Fact]
    public void OnceProp_onceKeys_not_populated_when_key_excluded_by_partial_reload()
    {
        var raw = new Dictionary<string, object?>
        {
            ["users"]     = new[] { "Alice" },
            ["countries"] = OnceProp.From(() => new[] { "US", "CA" }),
        };

        // Partial reload only requests "users"; "countries" not included
        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "users" }, partialExcept: null);

        result.Resolved.Should().NotContainKey("countries");
        // onceKeys should NOT contain "countries" because it wasn't part of this response
        result.OnceKeys.Should().NotContain("countries");
    }

    [Fact]
    public void OnceProp_onceKeys_populated_when_key_included_in_partial_reload()
    {
        var raw = new Dictionary<string, object?>
        {
            ["countries"] = OnceProp.From(() => new[] { "US" }),
        };

        // Client doesn't have countries yet — requests it
        var result = Resolver.Resolve(raw, isPartialReload: true,
            partialOnly: new HashSet<string> { "countries" }, partialExcept: null);

        result.Resolved.Should().ContainKey("countries");
        result.OnceKeys.Should().Contain("countries");
    }

    [Fact]
    public void OnceProp_value_excluded_but_key_in_onceKeys_on_full_load_when_client_has_it()
    {
        var raw = new Dictionary<string, object?>
        {
            ["countries"] = OnceProp.From(() => new[] { "US" }),
        };

        var result = Resolver.Resolve(raw, isPartialReload: false,
            partialOnly: null, partialExcept: null,
            clientOnceProps: new HashSet<string> { "countries" });

        result.Resolved.Should().NotContainKey("countries");
        // Key must still appear in onceKeys so client knows to keep its cached value
        result.OnceKeys.Should().Contain("countries");
    }
}
