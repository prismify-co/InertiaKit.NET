namespace InertiaKit.Core;

public sealed record OncePropMetadata(string Prop)
{
    public long? ExpiresAt { get; init; }
}

/// <summary>
/// Immutable representation of the Inertia page object.
/// Embedded as JSON in the root HTML template on first visit;
/// returned as the JSON body on every subsequent Inertia request.
/// </summary>
public sealed record PageObject
{
    public required string Component { get; init; }
    public required IReadOnlyDictionary<string, object?> Props { get; init; }
    public required string Url { get; init; }
    public required string Version { get; init; }

    // Merge hints
    public IReadOnlyList<string>? MergeProps { get; init; }
    public IReadOnlyList<string>? PrependProps { get; init; }
    public IReadOnlyList<string>? DeepMergeProps { get; init; }

    /// <summary>
    /// Array of field names used for identity-based client-side merging.
    /// Serialises to a JSON array (e.g. <c>["id"]</c>) per the Inertia protocol.
    /// The internal per-prop tracking is in <see cref="ResolvedProps.MatchOnFields"/>;
    /// this field is the wire-format projection of that map's distinct values.
    /// </summary>
    public IReadOnlyList<string>? MatchPropsOn { get; init; }

    // Loading hints
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? DeferredProps { get; init; }
    public IReadOnlyDictionary<string, OncePropMetadata>? OnceProps { get; init; }

    // Scroll / history
    public IReadOnlyList<ScrollRegion>? ScrollRegions { get; init; }
    public IReadOnlyDictionary<string, object?>? RememberedState { get; init; }
    public bool? EncryptHistory { get; init; }
    public bool? ClearHistory { get; init; }
    public bool? PreserveFragment { get; init; }
}
