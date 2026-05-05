# Middleware Pipeline

The server adapter is implemented as a middleware sitting between the framework's routing layer and the application controllers. Below is the precise execution order.

```
HTTP Request Received
│
├─ [1] Inject Vary: X-Inertia on response (unconditionally)
│
├─ [2] Parse Inertia request headers
│       X-Inertia, X-Inertia-Version, X-Inertia-Partial-Component,
│       X-Inertia-Partial-Data, X-Inertia-Partial-Except,
│       X-Inertia-Error-Bag, X-Inertia-Reset, Purpose
│
├─ [3] Hydrate request context (IInertiaRequest)
│       IsInertiaRequest, IsPartialReload, Version, etc.
│
├─ if NOT X-Inertia request
│   └─ Pass through to routing (no further Inertia processing)
│
├─ [4] Asset version check (GET requests only)
│       Compare X-Inertia-Version vs current version
│       └─ Mismatch → flash session → 409 + X-Inertia-Location → STOP
│
├─ [5] Session read
│       Extract flash data from previous request
│       Extract validation errors (if any were stored after a failed POST)
│       Extract error bag name
│
├─ [6] Route → Controller
│       Normal framework routing executes the action
│
├─ [7] Inspect controller return value
│       ├─ IInertiaResponse         → continue to [8]
│       ├─ RedirectResult           → continue to [12]
│       ├─ Non-Inertia result (View, JSON, etc.) → return as-is, STOP
│       └─ null / empty             → redirect to Referer (or fallback), STOP
│
├─ [8] Resolve shared data
│       Evaluate Share() factories (every request)
│       Evaluate ShareOnce() factories (first visit only; mark key in onceProps otherwise)
│
├─ [9] Build props
│       Merge order: shared → once → page → flash → errors
│       If partial reload:
│         ├─ Partial-Data set  → keep only those keys
│         ├─ Partial-Except set → remove those keys
│         └─ Always props      → include regardless
│       Evaluate lazy closures only for requested keys
│
├─ [10] Build page object
│        component, props, url, version
│        + mergeProps, prependProps, deepMergeProps, matchPropsOn
│        + deferredProps, onceProps
│        + scrollRegions, rememberedState
│        + encryptHistory, clearHistory, preserveFragment
│
├─ [11] Serialize and respond
│        Inertia request → JSON body + X-Inertia: true header
│        Initial request → render root view with embedded page JSON
│        └─ SSR enabled → POST page object to SSR gateway, embed rendered HTML
│
├─ [12] Redirect handling
│        ├─ Detect fragment in Location URL
│        │   └─ Yes → 409 + X-Inertia-Location (fragment redirect), STOP
│        ├─ Method was PUT/PATCH/DELETE and status is 302
│        │   └─ Promote to 303 See Other
│        ├─ Flash session data for next request
│        └─ Return redirect response
│
└─ Response returned to client
```

## Key Invariants

- Step [1] (Vary header) runs on **every** request, even non-Inertia ones.
- Step [4] (version check) fires only on GET — mutations already have their own redirect flow.
- Step [9] (prop filtering) always includes `Always` props even in partial reloads.
- Step [12] (redirect promotion) converts 302→303 only for non-GET origin methods.
- The middleware must not evaluate expensive prop closures it won't include in the response.
