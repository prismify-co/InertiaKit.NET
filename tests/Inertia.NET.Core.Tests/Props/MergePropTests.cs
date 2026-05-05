using FluentAssertions;
using Inertia.NET.Core.Props;

namespace Inertia.NET.Core.Tests.Props;

public class MergePropTests
{
    [Fact]
    public void Append_factory_sets_Append_strategy()
    {
        var prop = MergeProp.Append(new[] { 1, 2 });

        prop.Strategy.Should().Be(MergeStrategy.Append);
    }

    [Fact]
    public void Prepend_factory_sets_Prepend_strategy()
    {
        var prop = MergeProp.Prepend(new[] { 1, 2 });

        prop.Strategy.Should().Be(MergeStrategy.Prepend);
    }

    [Fact]
    public void DeepMerge_factory_sets_DeepMerge_strategy()
    {
        var prop = MergeProp.DeepMerge(new { theme = "dark" });

        prop.Strategy.Should().Be(MergeStrategy.DeepMerge);
    }

    [Fact]
    public void MatchOnField_is_null_by_default()
    {
        var prop = MergeProp.Append(null);

        prop.MatchOnField.Should().BeNull();
    }

    [Fact]
    public void MatchOn_returns_new_instance_with_field_set()
    {
        var original = MergeProp.Append(new[] { 1 });
        var matched = original.MatchOn("id");

        matched.MatchOnField.Should().Be("id");
        original.MatchOnField.Should().BeNull(); // original unchanged
    }

    [Fact]
    public void Value_is_accessible()
    {
        var data = new[] { "a", "b" };
        var prop = MergeProp.Append(data);

        prop.Value.Should().BeSameAs(data);
    }

    [Fact]
    public void Implements_IInertiaProperty()
    {
        MergeProp.Append(null).Should().BeAssignableTo<IInertiaProperty>();
    }
}
