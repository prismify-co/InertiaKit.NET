namespace InertiaKit.AspNetCore;

public sealed class InertiaHistoryOptions
{
    /// <summary>
    /// When enabled, Inertia responses default to encrypted browser history
    /// unless a route or response overrides the setting explicitly.
    /// </summary>
    public bool Encrypt { get; set; } = false;
}