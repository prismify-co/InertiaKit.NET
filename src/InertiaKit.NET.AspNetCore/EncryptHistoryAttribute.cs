namespace InertiaKit.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class EncryptHistoryAttribute(bool encrypt = true) : Attribute
{
    public bool Encrypt { get; } = encrypt;
}