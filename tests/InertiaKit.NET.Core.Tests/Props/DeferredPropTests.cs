using FluentAssertions;
using InertiaKit.Core.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.Core.Tests.Props;

public class DeferredPropTests
{
    private static IServiceProvider EmptyProvider =>
        new ServiceCollection().BuildServiceProvider();

    [Fact]
    public void Default_group_is_default()
    {
        var prop = DeferredProp.From(() => null);

        prop.Group.Should().Be("default");
    }

    [Fact]
    public void Custom_group_is_stored()
    {
        var prop = DeferredProp.From(() => null, "sidebar");

        prop.Group.Should().Be("sidebar");
    }

    [Fact]
    public void Evaluate_returns_factory_value()
    {
        var prop = DeferredProp.From(() => new[] { 1, 2, 3 });

        prop.Evaluate(EmptyProvider).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Implements_IInertiaProperty()
    {
        DeferredProp.From(() => null).Should().BeAssignableTo<IInertiaProperty>();
    }
}
