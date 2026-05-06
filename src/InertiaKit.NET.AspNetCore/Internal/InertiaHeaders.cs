namespace InertiaKit.AspNetCore.Internal;

internal static class InertiaHeaders
{
    // Request headers (Client → Server)
    public const string Inertia          = "X-Inertia";
    public const string Version          = "X-Inertia-Version";
    public const string PartialComponent = "X-Inertia-Partial-Component";
    public const string PartialData      = "X-Inertia-Partial-Data";
    public const string PartialExcept    = "X-Inertia-Partial-Except";
    public const string ErrorBag         = "X-Inertia-Error-Bag";
    public const string Reset            = "X-Inertia-Reset";
    public const string ExceptOnceProps  = "X-Inertia-Except-Once-Props";
    public const string LegacyOnceProps  = "X-Inertia-Once-Props";
    public const string Purpose          = "Purpose";

    // Response headers (Server → Client)
    public const string Location         = "X-Inertia-Location";
    public const string Vary             = "Vary";
    public const string VaryValue        = "X-Inertia";
}
