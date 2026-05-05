using InertiaKit.Core.Abstractions;
using InertiaKit.Core.Props;

namespace InertiaKit.Core;

public sealed class InertiaShareBuilder : IInertiaShareBuilder
{
    private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);

    public IInertiaShareBuilder Add(string key, object? value)
    {
        _entries[key] = value;
        return this;
    }

    public IInertiaShareBuilder Add(string key, Func<object?> factory)
    {
        _entries[key] = AlwaysProp.From(factory);
        return this;
    }

    public IInertiaShareBuilder Add(string key, Func<IServiceProvider, object?> factory)
    {
        _entries[key] = new AlwaysProp(factory);
        return this;
    }

    public IInertiaShareBuilder AddOnce(string key, object? value)
    {
        _entries[key] = OnceProp.From(value);
        return this;
    }

    public IInertiaShareBuilder AddOnce(string key, Func<object?> factory)
    {
        _entries[key] = OnceProp.From(factory);
        return this;
    }

    public IReadOnlyDictionary<string, object?> Build() =>
        new Dictionary<string, object?>(_entries, StringComparer.Ordinal);
}
