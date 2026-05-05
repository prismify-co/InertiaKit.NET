namespace InertiaKit.Core.Props;

/// <summary>
/// Always evaluated and included in the page object, even during partial reloads
/// where the key is not listed in X-Inertia-Partial-Data.
/// </summary>
public sealed class AlwaysProp(Func<IServiceProvider, object?> factory) : IInertiaProperty
{
    public object? Evaluate(IServiceProvider services) => factory(services);

    public static AlwaysProp From(object? value) =>
        new(_ => value);

    public static AlwaysProp From(Func<object?> factory) =>
        new(_ => factory());
}
