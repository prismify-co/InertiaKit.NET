# Prop System

Props fall into two axes: **evaluation timing** (when the value is computed) and **inclusion rules** (which requests include the prop). The combination of these two axes determines what appears in the page object and when.

## Prop Types

### Eager (default)

Evaluated immediately when the render call is made. Included on every request.

```csharp
Render("Users/Index", new { users = db.Users.ToList() })
```

### Optional (Lazy)

A closure. Evaluated only when the prop is requested. On partial reloads, omitted unless its key appears in `X-Inertia-Partial-Data`. On initial full page load, evaluated and included.

```csharp
Render("Users/Index", new {
    users    = db.Users.ToList(),
    comments = InertiaProps.Optional(() => db.Comments.ToList())
})
```

When the client does `router.visit(url, { only: ["users"] })`, the `comments` closure is never called.

### Always

Evaluated and included on every request including partial reloads, even if the prop key is not in `X-Inertia-Partial-Data`. Use for data that the component always needs regardless of which subset of data was requested (e.g. validation errors, notification counts).

```csharp
Render("Users/Index", new {
    users  = db.Users.ToList(),
    errors = InertiaProps.Always(() => TempData["errors"])
})
```

### Deferred

A closure evaluated not during the initial render but in a follow-up partial request that the client makes automatically after the page becomes interactive. The server includes the prop key in `deferredProps` (grouped) rather than in `props`.

```csharp
Render("Dashboard", new {
    summary     = GetSummary(),
    heavyReport = InertiaProps.Defer(() => BuildHeavyReport(), group: "default")
})
```

Page object:
```json
{
  "props":         { "summary": { ... } },
  "deferredProps": { "default": ["heavyReport"] }
}
```

The client then issues a partial request for `heavyReport` after initial render.

### Once

Sent in `props` on first visit only. On subsequent visits the key appears in `onceProps` but the value is absent — the client uses its cached copy. If the client loses state (hard reload) it will request the key again. Ideal for large static reference data.

```csharp
// In middleware Share():
shared.AddOnce("countries", () => db.Countries.OrderBy(c => c.Name).ToList());
```

Page object on first visit:
```json
{ "props": { "countries": [...] }, "onceProps": ["countries"] }
```

Page object on subsequent visits:
```json
{ "props": {}, "onceProps": ["countries"] }
```

## Merge Strategies

Merge strategies annotate a prop so that the client combines the new value with the existing one rather than replacing it. They appear in `mergeProps`, `prependProps`, or `deepMergeProps` in the page object.

### Append (Merge)

New items are appended to the end of the existing array.

```csharp
Render("Feed/Index", new {
    posts = InertiaProps.Merge(newPage)
})
```

Page object: `"mergeProps": ["posts"]`

Before: `[a, b, c]` + new `[d, e]` → `[a, b, c, d, e]`

### Prepend

New items are prepended to the beginning of the existing array.

```csharp
Render("Feed/Index", new {
    posts = InertiaProps.Prepend(latestPosts)
})
```

Page object: `"prependProps": ["posts"]`

Before: `[a, b, c]` + new `[x, y]` → `[x, y, a, b, c]`

### Deep Merge

Nested objects are recursively merged (union of keys, new values win on collision).

```csharp
Render("Settings", new {
    config = InertiaProps.DeepMerge(updatedConfig)
})
```

Page object: `"deepMergeProps": ["config"]`

Before: `{ theme: "dark", lang: "en" }` + new `{ theme: "light" }` → `{ theme: "light", lang: "en" }`

### Match On (Identity Merge)

When merging arrays, the client matches items by a key field and replaces matched items rather than appending duplicates.

```csharp
Render("Users/Index", new {
    users = InertiaProps.Merge(updatedUsers).MatchOn("id")
})
```

Page object: `"mergeProps": ["users"], "matchPropsOn": ["id"]`

If a user with `id: 5` already exists in the client state, it is replaced. Otherwise it is appended.

## Summary Matrix

| Type | Evaluated | Included in Partial Reload | Included on First Visit | In Page Object |
|---|---|---|---|---|
| Eager | Always | Only if in `Partial-Data` | Yes | In `props` |
| Optional | Only if requested | Only if in `Partial-Data` | Yes | In `props` |
| Always | Always | **Always** | Yes | In `props` |
| Deferred | Never on initial request | Via follow-up partial request | No | Key in `deferredProps` |
| Once | First visit only | If client missing it | Yes | In `props` + key in `onceProps` |
| Merge variants | Same as source type | Same | Yes | In `props` + key in `mergeProps`/`prependProps`/`deepMergeProps` |
