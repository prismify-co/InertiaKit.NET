# Recommended C# Domain Model

This document captures the interfaces, records, and enums that the research suggests are needed. These are design inputs — not final decisions. The actual types will be refined during implementation.

## Core Value Objects

```csharp
// The canonical page object sent to and received from the client
public interface IOncePropMetadata
{
    string Prop { get; }
    long?  ExpiresAt { get; }
}

public interface IPageObject
{
    string                                        Component       { get; }
    IReadOnlyDictionary<string, object?>          Props           { get; }
    string                                        Url             { get; }
    string                                        Version         { get; }

    // Merge hints
    IReadOnlyList<string>?                        MergeProps      { get; }
    IReadOnlyList<string>?                        PrependProps    { get; }
    IReadOnlyList<string>?                        DeepMergeProps  { get; }
    IReadOnlyList<string>?                        MatchPropsOn    { get; }

    // Loading hints
    IReadOnlyDictionary<string, IReadOnlyList<string>>? DeferredProps { get; }
    IReadOnlyDictionary<string, IOncePropMetadata>? OnceProps    { get; }

    // Navigation/scroll
    IReadOnlyList<ScrollRegion>?                  ScrollRegions   { get; }
    IReadOnlyDictionary<string, object?>?         RememberedState { get; }

    // History flags
    bool?                                         EncryptHistory  { get; }
    bool?                                         ClearHistory    { get; }
    bool?                                         PreserveFragment{ get; }
}

public record ScrollRegion(double X, double Y);
```

## Parsed Inertia Request Context

```csharp
public interface IInertiaRequest
{
    bool             IsInertiaRequest  { get; }
    bool             IsPartialReload   { get; }
    bool             IsPrefetch        { get; }

    string?          Version           { get; }  // X-Inertia-Version
    string?          PartialComponent  { get; }  // X-Inertia-Partial-Component
    string?          ErrorBag          { get; }  // X-Inertia-Error-Bag

    IReadOnlySet<string>? PartialOnly   { get; }  // X-Inertia-Partial-Data
    IReadOnlySet<string>? PartialExcept { get; }  // X-Inertia-Partial-Except
    IReadOnlySet<string>? ResetProps    { get; }  // X-Inertia-Reset
}
```

## Prop Wrapper Interfaces

```csharp
// Marker for all Inertia-annotated prop values
public interface IInertiaProperty { }

// Evaluated only when the prop key is in X-Inertia-Partial-Data (or on first visit)
public interface IOptionalProperty : IInertiaProperty
{
    object? Evaluate(IServiceProvider services);
}

// Always evaluated and included, even during partial reloads
public interface IAlwaysProperty : IInertiaProperty
{
    object? Evaluate(IServiceProvider services);
}

// Deferred to a follow-up client request after initial render
public interface IDeferredProperty : IInertiaProperty
{
    string   Group    { get; }  // "default" or a named group
    object?  Evaluate(IServiceProvider services);
}

// Sent once; client caches and server omits on subsequent visits
public interface IOnceProperty : IInertiaProperty
{
    object? Evaluate(IServiceProvider services);
}

// Annotates a value/closure for client-side merging
public interface IMergeProperty : IInertiaProperty
{
    MergeStrategy Strategy      { get; }
    string?        MatchOnField { get; }  // null unless MatchOn() was called
    object?        Evaluate();
}

public enum MergeStrategy { Append, Prepend, DeepMerge }
```

## Response Builder

```csharp
// Fluent builder returned by Inertia.Render()
public interface IInertiaResponse
{
    string                           Component { get; }
    IDictionary<string, object?>     Props     { get; }

    IInertiaResponse Flash(string key, object? value);
    IInertiaResponse Flash(IDictionary<string, object?> values);

    IInertiaResponse ViewData(string key, object? value);

    IInertiaResponse WithEncryptHistory(bool encrypt = true);
    IInertiaResponse WithClearHistory(bool clear   = true);
    IInertiaResponse WithPreserveFragment();
}

// 409 external/location redirect
public interface IInertiaLocationResponse
{
    string Url { get; }
}
```

## Core Service (Injected into Controllers)

```csharp
public interface IInertia
{
    // Render a component with props
    IInertiaResponse Render(string component);
    IInertiaResponse Render(string component, object props);
    IInertiaResponse Render(string component, IDictionary<string, object?> props);

    // 409 external redirect
    IInertiaLocationResponse Location(string url);

    // Prop type factories
    IOptionalProperty Optional(Func<object?>              factory);
    IOptionalProperty Optional(Func<IServiceProvider, object?> factory);

    IAlwaysProperty   Always(object?                      value);
    IAlwaysProperty   Always(Func<object?>                factory);
    IAlwaysProperty   Always(Func<IServiceProvider, object?> factory);

    IDeferredProperty Defer(Func<object?>                 factory, string group = "default");
    IDeferredProperty Defer(Func<IServiceProvider, object?> factory, string group = "default");

    IOnceProperty     Once(object?                        value);
    IOnceProperty     Once(Func<object?>                  factory);

    IMergeProperty    Merge(object?                       value, MergeStrategy strategy = MergeStrategy.Append);
}
```

## Middleware Configuration Contract

```csharp
// Users subclass this (or implement the interface) to configure Inertia per application
public interface IHandleInertiaRequests
{
    // Return null to disable versioning
    string? Version(IInertiaRequestContext context);

    // Register props shared on every response
    void Share(IInertiaShareBuilder shared, IInertiaRequestContext context);

    // Root view / layout template name
    string RootView { get; }

    // SSR settings
    bool     SsrEnabled          { get; }
    string   SsrUrl              { get; }
    string[] SsrExcludedRoutes   { get; }
}

public interface IInertiaShareBuilder
{
    IInertiaShareBuilder Add(string key, object? value);
    IInertiaShareBuilder Add(string key, Func<object?> factory);
    IInertiaShareBuilder Add(string key, Func<IServiceProvider, object?> factory);
    IInertiaShareBuilder AddOnce(string key, object? value);
    IInertiaShareBuilder AddOnce(string key, Func<object?> factory);
}
```

## Serialization

```csharp
public interface IInertiaSerializer
{
    string      Serialize(IPageObject page);
    IPageObject Deserialize(string json);
}
```

## Validation Errors

```csharp
public interface IValidationErrors
{
    // All errors keyed by field name
    IReadOnlyDictionary<string, IReadOnlyList<string>> All { get; }

    // First error per field (subset view)
    IReadOnlyDictionary<string, string> First { get; }

    bool Any { get; }
}
```

## Testing Assertions

```csharp
public interface IInertiaAssertions
{
    IInertiaAssertions HasComponent(string expected);

    IInertiaAssertions HasProp(string key);
    IInertiaAssertions HasProp(string key, object? expectedValue);
    IInertiaAssertions HasProps(params string[] keys);
    IInertiaAssertions DoesNotHaveProp(string key);

    IInertiaAssertions HasVersion(string expected);
    IInertiaAssertions HasUrl(string expected);

    IInertiaAssertions HasError(string field);
    IInertiaAssertions HasError(string field, string messageContains);
    IInertiaAssertions HasNoErrors();

    IInertiaAssertions HasFlash(string key);
    IInertiaAssertions HasFlash(string key, object? expectedValue);

    IInertiaAssertions HasMergeProps(params string[] propNames);
    IInertiaAssertions HasDeferredProps(string group, params string[] propNames);
    IInertiaAssertions HasOnceProps(params string[] propNames);

    IInertiaAssertions IsEncryptedHistory();
    IInertiaAssertions ClearsHistory();
}

// Extension point
public static class InertiaPageObjectExtensions
{
    public static IInertiaAssertions Should(this IPageObject page) => ...;
}
```

## Design Principles

1. **Framework-agnostic core.** `IPageObject`, `IInertiaRequest`, all prop interfaces, and the serializer must not reference `Microsoft.AspNetCore.*`. Framework-specific wiring (reading HTTP headers, writing HTTP responses) lives in separate adapter packages.

2. **Immutable page object.** All collections on `IPageObject` are `IReadOnly*`. The builder (mutable) is internal to the adapter; controllers receive the read-only result.

3. **Lazy evaluation via closures.** `Func<object?>` and `Func<IServiceProvider, object?>` are the evaluation contract. The middleware decides whether to call the closure based on inclusion rules.

4. **No static access.** `IInertia` is injected via DI. No `Inertia.Render(...)` static calls — this enables testing and avoids hidden coupling.

5. **Testing first.** `IInertiaAssertions` ships in the core package alongside the adapter. Testing is a first-class concern, not an afterthought.
