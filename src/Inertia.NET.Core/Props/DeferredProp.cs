namespace Inertia.NET.Core.Props;

/// <summary>
/// Not included in the initial response. The prop key is placed in <c>deferredProps</c>
/// so the client issues a follow-up partial request to load it after first render.
/// </summary>
public sealed class DeferredProp(Func<IServiceProvider, object?> factory, string group = "default")
    : IInertiaProperty
{
    public string Group { get; } = group;

    public object? Evaluate(IServiceProvider services) => factory(services);

    public static DeferredProp From(Func<object?> factory, string group = "default") =>
        new(_ => factory(), group);
}
