namespace InertiaKit.Core;

/// <summary>
/// Parsed representation of the Inertia-specific HTTP request headers.
/// Framework adapters create this from the underlying HTTP request.
/// </summary>
public sealed record InertiaRequest
{
    public bool IsInertiaRequest { get; init; }
    public bool IsPartialReload => PartialComponent is not null;
    public bool IsPrefetch { get; init; }

    public string? Version { get; init; }
    public string? PartialComponent { get; init; }
    public string? ErrorBag { get; init; }

    public IReadOnlySet<string>? PartialOnly { get; init; }
    public IReadOnlySet<string>? PartialExcept { get; init; }
    public IReadOnlySet<string>? ResetProps { get; init; }
    public IReadOnlySet<string>? ClientOnceProps { get; init; }

    public static InertiaRequest NonInertia() => new() { IsInertiaRequest = false };

    public static InertiaRequest Parse(
        bool isInertia,
        string? version,
        string? partialComponent,
        string? partialData,
        string? partialExcept,
        string? errorBag,
        string? resetProps,
        string? onceProps,
        bool isPrefetch)
    {
        if (!isInertia) return NonInertia();

        return new InertiaRequest
        {
            IsInertiaRequest = true,
            IsPrefetch = isPrefetch,
            Version = version,
            PartialComponent = partialComponent,
            ErrorBag = errorBag,
            PartialOnly = ParseCommaList(partialData),
            PartialExcept = ParseCommaList(partialExcept),
            ResetProps = ParseCommaList(resetProps),
            ClientOnceProps = ParseCommaList(onceProps),
        };
    }

    private static IReadOnlySet<string>? ParseCommaList(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? null
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .ToHashSet(StringComparer.Ordinal);
}
