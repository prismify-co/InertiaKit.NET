using InertiaKit.Core.Props;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InertiaKit.Core.Serialization;

/// <summary>
/// Evaluates a raw props dictionary — which may contain <see cref="IInertiaProperty"/>
/// wrappers — into a resolved set of concrete values, deferred groups, once keys,
/// and merge hints, respecting partial-reload inclusion rules.
/// </summary>
public sealed class PropResolver(IServiceProvider services, ILogger<PropResolver>? logger = null)
{
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    public ResolvedProps Resolve(
        IReadOnlyDictionary<string, object?> raw,
        bool isPartialReload,
        IReadOnlySet<string>? partialOnly,
        IReadOnlySet<string>? partialExcept,
        IReadOnlySet<string>? clientOnceProps = null,
        IReadOnlySet<string>? resetProps = null)
    {
        var resolved       = new Dictionary<string, object?>();
        var deferredGroups = new Dictionary<string, List<string>>();
        var onceKeys       = new HashSet<string>();
        var mergeKeys      = new Dictionary<MergeStrategy, List<string>>
        {
            [MergeStrategy.Append]    = [],
            [MergeStrategy.Prepend]   = [],
            [MergeStrategy.DeepMerge] = [],
        };
        var matchOnFields = new Dictionary<string, string>();

        foreach (var (key, value) in raw)
        {
            switch (value)
            {
                // ── DeferredProp ──────────────────────────────────────────────
                // On initial load: place in deferredGroups so client fetches later.
                // On follow-up partial reload requesting this key: evaluate now and include.
                case DeferredProp deferred:
                    if (isPartialReload && ShouldInclude(key, isPartialReload, partialOnly, partialExcept))
                    {
                        // Client is explicitly asking for this deferred prop — evaluate it
                        resolved[key] = SafeEvaluate(key, () => deferred.Evaluate(services));
                    }
                    else if (!isPartialReload)
                    {
                        // Initial full load — advertise as deferred
                        if (!deferredGroups.TryGetValue(deferred.Group, out var groupList))
                        {
                            groupList = [];
                            deferredGroups[deferred.Group] = groupList;
                        }
                        groupList.Add(key);
                    }
                    // On a partial reload that doesn't request this key: omit entirely (not deferred again)
                    break;

                // ── OnceProp ──────────────────────────────────────────────────
                // Always track onceKeys ONLY when the key is actually being sent this response.
                case OnceProp once:
                {
                    bool included = ShouldInclude(key, isPartialReload, partialOnly, partialExcept);
                    bool clientHasIt = clientOnceProps?.Contains(key) ?? false;

                    if (included)
                    {
                        // Key is part of this response — mark in onceKeys
                        onceKeys.Add(key);
                        if (!clientHasIt)
                            resolved[key] = SafeEvaluate(key, () => once.Evaluate(services));
                        // If clientHasIt: value omitted, key still in onceKeys to confirm cache validity
                    }
                    // If !included (partial reload not requesting this key): omit entirely
                    break;
                }

                // ── AlwaysProp — bypasses partial-reload filtering ────────────
                case AlwaysProp always:
                    resolved[key] = SafeEvaluate(key, () => always.Evaluate(services));
                    break;

                // ── MergeProp ─────────────────────────────────────────────────
                case MergeProp merge when ShouldInclude(key, isPartialReload, partialOnly, partialExcept):
                    resolved[key] = merge.Value;
                    // resetProps strips the merge annotation: client replaces instead of merges
                    if (resetProps is null || !resetProps.Contains(key))
                    {
                        mergeKeys[merge.Strategy].Add(key);
                        if (merge.MatchOnField is not null)
                            matchOnFields[key] = merge.MatchOnField;
                    }
                    break;

                // ── OptionalProp ──────────────────────────────────────────────
                case OptionalProp optional when ShouldInclude(key, isPartialReload, partialOnly, partialExcept):
                    resolved[key] = SafeEvaluate(key, () => optional.Evaluate(services));
                    break;

                // ── Eager (plain value) ───────────────────────────────────────
                case null or not IInertiaProperty when ShouldInclude(key, isPartialReload, partialOnly, partialExcept):
                    resolved[key] = value;
                    break;
            }
        }

        return new ResolvedProps(
            Resolved: resolved,
            DeferredGroups: deferredGroups.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly()),
            OnceKeys: onceKeys,
            MergeKeys: mergeKeys.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly()),
            MatchOnFields: matchOnFields);
    }

    private object? SafeEvaluate(string key, Func<object?> evaluate)
    {
        try
        {
            return evaluate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Prop factory for key '{Key}' threw an exception; prop will be null", key);
            return null;
        }
    }

    private static bool ShouldInclude(
        string key,
        bool isPartialReload,
        IReadOnlySet<string>? partialOnly,
        IReadOnlySet<string>? partialExcept)
    {
        if (!isPartialReload) return true;
        if (partialOnly  is not null) return partialOnly.Contains(key);
        if (partialExcept is not null) return !partialExcept.Contains(key);
        return true;
    }
}

public sealed record ResolvedProps(
    IReadOnlyDictionary<string, object?> Resolved,
    IReadOnlyDictionary<string, IReadOnlyList<string>> DeferredGroups,
    IReadOnlySet<string> OnceKeys,
    IReadOnlyDictionary<MergeStrategy, IReadOnlyList<string>> MergeKeys,
    /// <summary>Per-prop match field for identity-based array merging (propKey → fieldName).</summary>
    IReadOnlyDictionary<string, string> MatchOnFields);
