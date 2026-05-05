namespace InertiaKit.Core.Props;

/// <summary>
/// Annotates a prop for client-side merging. The client combines the new value
/// with the existing one using the specified <see cref="MergeStrategy"/> rather
/// than replacing it outright.
/// </summary>
public sealed class MergeProp(object? value, MergeStrategy strategy = MergeStrategy.Append)
    : IInertiaProperty
{
    public object? Value { get; } = value;
    public MergeStrategy Strategy { get; } = strategy;

    /// <summary>
    /// Field name used to match existing items when merging arrays (e.g. "id").
    /// Null means simple append/prepend without identity matching.
    /// </summary>
    public string? MatchOnField { get; private init; }

    public MergeProp MatchOn(string field) =>
        new(Value, Strategy) { MatchOnField = field };

    public static MergeProp Append(object? value) => new(value, MergeStrategy.Append);
    public static MergeProp Prepend(object? value) => new(value, MergeStrategy.Prepend);
    public static MergeProp DeepMerge(object? value) => new(value, MergeStrategy.DeepMerge);
}
