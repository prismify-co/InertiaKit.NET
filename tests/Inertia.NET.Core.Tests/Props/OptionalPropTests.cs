using FluentAssertions;
using Inertia.NET.Core.Props;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.NET.Core.Tests.Props;

public class OptionalPropTests
{
    private static IServiceProvider EmptyProvider =>
        new ServiceCollection().BuildServiceProvider();

    [Fact]
    public void Evaluate_executes_factory_and_returns_value()
    {
        var prop = OptionalProp.From(() => 42);

        prop.Evaluate(EmptyProvider).Should().Be(42);
    }

    [Fact]
    public void Evaluate_with_service_provider_factory_resolves_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton("hello");
        var provider = services.BuildServiceProvider();

        var prop = new OptionalProp(sp => sp.GetRequiredService<string>());

        prop.Evaluate(provider).Should().Be("hello");
    }

    [Fact]
    public void Evaluate_factory_is_called_each_time()
    {
        var callCount = 0;
        var prop = OptionalProp.From(() => ++callCount);

        prop.Evaluate(EmptyProvider);
        prop.Evaluate(EmptyProvider);

        callCount.Should().Be(2);
    }

    [Fact]
    public void Implements_IInertiaProperty()
    {
        var prop = OptionalProp.From(() => null);

        prop.Should().BeAssignableTo<IInertiaProperty>();
    }
}
