using FluentAssertions;
using InertiaKit.Core;

namespace InertiaKit.Core.Tests;

public class InertiaRequestTests
{
    [Fact]
    public void NonInertia_returns_request_with_IsInertiaRequest_false()
    {
        var request = InertiaRequest.NonInertia();

        request.IsInertiaRequest.Should().BeFalse();
        request.IsPartialReload.Should().BeFalse();
    }

    [Fact]
    public void Parse_sets_IsInertiaRequest_true_when_header_present()
    {
        var request = InertiaRequest.Parse(
            isInertia: true, version: "abc", partialComponent: null,
            partialData: null, partialExcept: null, errorBag: null,
            resetProps: null, onceProps: null, isPrefetch: false);

        request.IsInertiaRequest.Should().BeTrue();
        request.Version.Should().Be("abc");
    }

    [Fact]
    public void Parse_detects_partial_reload_from_partial_component_header()
    {
        var request = InertiaRequest.Parse(
            isInertia: true, version: "v1", partialComponent: "Users/Index",
            partialData: "users,count", partialExcept: null, errorBag: null,
            resetProps: null, onceProps: null, isPrefetch: false);

        request.IsPartialReload.Should().BeTrue();
        request.PartialComponent.Should().Be("Users/Index");
        request.PartialOnly.Should().BeEquivalentTo(new[] { "users", "count" });
    }

    [Fact]
    public void Parse_handles_partial_except_header()
    {
        var request = InertiaRequest.Parse(
            isInertia: true, version: "v1", partialComponent: "Users/Index",
            partialData: null, partialExcept: "comments,threads", errorBag: null,
            resetProps: null, onceProps: null, isPrefetch: false);

        request.PartialExcept.Should().BeEquivalentTo(new[] { "comments", "threads" });
        request.PartialOnly.Should().BeNull();
    }

    [Fact]
    public void Parse_trims_whitespace_in_comma_lists()
    {
        var request = InertiaRequest.Parse(
            isInertia: true, version: "v1", partialComponent: "P",
            partialData: " users , count ", partialExcept: null, errorBag: null,
            resetProps: null, onceProps: null, isPrefetch: false);

        request.PartialOnly.Should().BeEquivalentTo(new[] { "users", "count" });
    }

    [Fact]
    public void Parse_with_isInertia_false_returns_NonInertia()
    {
        var request = InertiaRequest.Parse(
            isInertia: false, version: "v1", partialComponent: null,
            partialData: null, partialExcept: null, errorBag: null,
            resetProps: null, onceProps: null, isPrefetch: false);

        request.IsInertiaRequest.Should().BeFalse();
    }

    [Fact]
    public void IsPrefetch_is_set_from_parse()
    {
        var request = InertiaRequest.Parse(
            isInertia: true, version: "v1", partialComponent: null,
            partialData: null, partialExcept: null, errorBag: null,
            resetProps: null, onceProps: null, isPrefetch: true);

        request.IsPrefetch.Should().BeTrue();
    }
}
