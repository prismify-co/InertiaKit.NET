namespace Inertia.NET.Core.Abstractions;

public interface IInertiaShareBuilder
{
    IInertiaShareBuilder Add(string key, object? value);
    IInertiaShareBuilder Add(string key, Func<object?> factory);
    IInertiaShareBuilder Add(string key, Func<IServiceProvider, object?> factory);
    IInertiaShareBuilder AddOnce(string key, object? value);
    IInertiaShareBuilder AddOnce(string key, Func<object?> factory);

    IReadOnlyDictionary<string, object?> Build();
}
