namespace Inertia.NET.Core.Props;

/// <summary>
/// Sent to the client on the first visit. Subsequent responses mark the key in
/// <c>onceProps</c> but omit the value — the client uses its cached copy.
/// </summary>
public sealed class OnceProp(Func<IServiceProvider, object?> factory) : IInertiaProperty
{
    public object? Evaluate(IServiceProvider services) => factory(services);

    public static OnceProp From(object? value) =>
        new(_ => value);

    public static OnceProp From(Func<object?> factory) =>
        new(_ => factory());
}
