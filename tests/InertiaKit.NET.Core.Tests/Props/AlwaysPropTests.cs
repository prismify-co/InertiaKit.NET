using FluentAssertions;
using InertiaKit.Core.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.Core.Tests.Props;

public class AlwaysPropTests
{
    private static IServiceProvider EmptyProvider =>
        new ServiceCollection().BuildServiceProvider();

    [Fact]
    public void From_static_value_returns_that_value_on_evaluate()
    {
        var prop = AlwaysProp.From("shared-value");

        prop.Evaluate(EmptyProvider).Should().Be("shared-value");
    }

    [Fact]
    public void From_factory_evaluates_closure()
    {
        var prop = AlwaysProp.From(() => new { Count = 3 });

        var result = prop.Evaluate(EmptyProvider);
        result.Should().BeEquivalentTo(new { Count = 3 });
    }

    [Fact]
    public void Implements_IInertiaProperty()
    {
        AlwaysProp.From((object?)null).Should().BeAssignableTo<IInertiaProperty>();
    }
}
