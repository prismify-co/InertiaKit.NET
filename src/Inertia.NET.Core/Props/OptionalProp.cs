namespace Inertia.NET.Core.Props;

/// <summary>
/// Evaluated only when the prop key is explicitly requested (partial reloads)
/// or on the initial full-page load. Skipped on partial reloads where its key
/// is absent from X-Inertia-Partial-Data.
/// </summary>
public sealed class OptionalProp(Func<IServiceProvider, object?> factory) : IInertiaProperty
{
    public object? Evaluate(IServiceProvider services) => factory(services);

    public static OptionalProp From(Func<object?> factory) =>
        new(_ => factory());
}
