namespace InertiaKit.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ClearHistoryAttribute(bool clear = true) : Attribute
{
    public bool Clear { get; } = clear;
}