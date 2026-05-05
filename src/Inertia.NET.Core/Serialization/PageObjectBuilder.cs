using Inertia.NET.Core.Props;

namespace Inertia.NET.Core.Serialization;

/// <summary>
/// Assembles a <see cref="PageObject"/> from a resolved prop set and
/// the current request context values. Framework adapters call this
/// after running <see cref="PropResolver"/>.
/// </summary>
public static class PageObjectBuilder
{
    public static PageObject Build(
        string component,
        ResolvedProps resolved,
        string url,
        string version,
        bool? encryptHistory = null,
        bool? clearHistory = null,
        bool? preserveFragment = null,
        IReadOnlyList<ScrollRegion>? scrollRegions = null,
        IReadOnlyDictionary<string, object?>? rememberedState = null)
    {
        var mergeProps    = ToNullIfEmpty(resolved.MergeKeys[MergeStrategy.Append]);
        var prependProps  = ToNullIfEmpty(resolved.MergeKeys[MergeStrategy.Prepend]);
        var deepMergeProps = ToNullIfEmpty(resolved.MergeKeys[MergeStrategy.DeepMerge]);

        // matchPropsOn: protocol wire format is a JSON array of field names.
        // We project the per-prop dictionary to its distinct values so that
        // { users→"id", posts→"id" } → ["id"] and
        // { users→"id", posts→"slug" } → ["id", "slug"].
        IReadOnlyList<string>? matchPropsOn = resolved.MatchOnFields.Count > 0
            ? resolved.MatchOnFields.Values.Distinct(StringComparer.Ordinal).ToList()
            : null;

        var deferredProps = resolved.DeferredGroups.Count > 0
            ? (IReadOnlyDictionary<string, IReadOnlyList<string>>?)resolved.DeferredGroups
            : null;

        var onceProps = resolved.OnceKeys.Count > 0
            ? (IReadOnlyList<string>?)resolved.OnceKeys.ToList()
            : null;

        return new PageObject
        {
            Component       = component,
            Props           = resolved.Resolved,
            Url             = url,
            Version         = version,
            MergeProps      = mergeProps,
            PrependProps    = prependProps,
            DeepMergeProps  = deepMergeProps,
            MatchPropsOn    = matchPropsOn,
            DeferredProps   = deferredProps,
            OnceProps       = onceProps,
            EncryptHistory  = encryptHistory,
            ClearHistory    = clearHistory,
            PreserveFragment = preserveFragment,
            ScrollRegions   = scrollRegions,
            RememberedState = rememberedState,
        };
    }

    private static IReadOnlyList<string>? ToNullIfEmpty(IReadOnlyList<string> list) =>
        list.Count > 0 ? list : null;
}
