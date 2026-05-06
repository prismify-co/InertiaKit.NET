# Page Object Schema

The page object is the single data structure exchanged between server and client on every Inertia response. On initial load it is embedded as JSON inside a `<script>` tag; on subsequent navigations it is the JSON response body.

## TypeScript Shape (Canonical)

```typescript
interface PageObject {
  // ── Core (always present) ─────────────────────────────────────────────
  component: string;                       // e.g. "Users/Index"
  props:     Record<string, unknown>;      // merged page + shared props
  url:       string;                       // current page URL
  version:   string;                       // current asset version string

  // ── Prop Merge Hints (optional) ───────────────────────────────────────
  mergeProps?:     string[];               // props to append on navigation
  prependProps?:   string[];               // props to prepend on navigation
  deepMergeProps?: string[];               // props to deep-merge recursively
  matchPropsOn?:   string[];              // field to match items on when merging (e.g. "id")

  // ── Loading Hints (optional) ──────────────────────────────────────────
  deferredProps?: Record<string, string[]>; // group → prop keys to load post-render
  onceProps?:     Record<string, {
    prop: string;
    expiresAt?: number | null;
  }>;                                     // once-prop key → cached prop path + optional expiry

  // ── Scroll Management (optional) ──────────────────────────────────────
  scrollRegions?:   Array<{ x: number; y: number }>; // per-region scroll positions
  rememberedState?: Record<string, unknown>;          // component state to restore on back/forward

  // ── History Management (optional) ─────────────────────────────────────
  encryptHistory?:   boolean;  // encrypt this entry before writing to history
  clearHistory?:     boolean;  // clear all encrypted history and generate a new key
  preserveFragment?: boolean;  // keep the URL fragment (#section) when navigating
}
```

## Field Reference

### `component` (string, required)
The frontend component to render, expressed as a path relative to the `Pages/` directory. Separators are forward slashes. Example: `"Users/Index"` → `Pages/Users/Index.vue`.

### `props` (object, required)
The merged data object available to the component. Contains:
- Shared props (injected by middleware on every request)
- Shared-once props (on first visit or if client is missing them)
- Page-specific props returned by the controller
- Flash data from the previous redirect's session
- Validation errors (under the `errors` key, if present)

Prop evaluation order (later entries win on key collision):
1. Shared props
2. Shared-once props
3. Page props
4. Flash props
5. Validation errors

### `url` (string, required)
The canonical URL of the current page, including path and query string. Preserves any trailing slash. Used by the client for browser history management.

### `version` (string, required)
An opaque identifier representing the current asset build. The client echoes this back in `X-Inertia-Version` on every subsequent request. Any change triggers a full reload via the 409 mismatch flow.

### `mergeProps` / `prependProps` / `deepMergeProps` (string[], optional)
Arrays of prop keys that should be combined with the existing client-side prop state rather than replaced. See `server-adapter/prop-system.md` for full semantics.

### `matchPropsOn` (string[], optional)
When a prop is being merged and `matchPropsOn` includes a field name (e.g. `"id"`), the client uses that field to find and replace matching items in the existing array rather than blindly appending.

### `deferredProps` (Record<string, string[]>, optional)
Maps group names to arrays of prop keys that the client should fetch after the initial render in a follow-up partial request. The `"default"` group is loaded first (on next idle); named groups (e.g. `"sidebar"`) can be loaded on demand.

```json
{
  "deferredProps": {
    "default":  ["recentActivity"],
    "sidebar":  ["categories", "tags"]
  }
}
```

### `onceProps` (Record<string, { prop, expiresAt? }>, optional)
Metadata for props that the client caches after the first response and does not request again until they expire or disappear from client state. The object is keyed by the once-prop name. Each value records the prop path the client should restore from its cache and may optionally include an absolute `expiresAt` timestamp in milliseconds.

On subsequent visits the client sends the once-prop keys it already has in `X-Inertia-Except-Once-Props`. The server may omit those values from `props` while still returning their metadata in `onceProps`, allowing the client to merge the cached values back into the response.

```json
{
  "onceProps": {
    "appConfig": {
      "prop": "appConfig",
      "expiresAt": null
    }
  }
}
```

### `scrollRegions` (Array<{x,y}>, optional)
Scroll positions for custom scrollable elements (those with CSS `overflow`). The client restores these positions on back/forward navigation.

### `rememberedState` (object, optional)
Arbitrary component-level state (form values, UI state) that the client persists across navigations and restores when the user navigates back.

### `encryptHistory` (boolean, optional)
When `true`, the client encrypts the page state using the Web Crypto API before writing it to browser history. The decryption key is stored in `sessionStorage`. Useful for pages with sensitive data.

### `clearHistory` (boolean, optional)
When `true`, the client clears all previously encrypted history entries and generates a new encryption key. Typically set on logout or session expiry.

### `preserveFragment` (boolean, optional)
When `true`, the URL fragment (`#section`) is kept when navigating to this page.

## Embedded JSON on Initial Load

```html
<div id="app"></div>
<script type="application/json" id="app-data">
{
  "component": "Users/Index",
  "props": { "users": [...] },
  "url": "/users",
  "version": "abc123"
}
</script>
```

The client reads `#app-data`, parses the page object, and hydrates the component.
