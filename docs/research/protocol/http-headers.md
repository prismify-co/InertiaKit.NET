# HTTP Headers Reference

## Client → Server (Request Headers)

| Header | Value | Required | Purpose |
|---|---|---|---|
| `X-Inertia` | `"true"` | Yes (all Inertia requests) | Identifies the request as an Inertia XHR |
| `X-Inertia-Version` | string | Yes | Client's current asset version for mismatch detection |
| `X-Requested-With` | `"XMLHttpRequest"` | Yes | Standard XHR identifier |
| `X-Inertia-Partial-Component` | component name string | Partial reloads only | Component being partially reloaded |
| `X-Inertia-Partial-Data` | comma-separated prop keys | Partial reloads only | Props the client wants included |
| `X-Inertia-Partial-Except` | comma-separated prop keys | Partial reloads only | Props the client wants excluded |
| `X-Inertia-Error-Bag` | string | Form submissions only | Scopes validation errors to a named bag |
| `X-Inertia-Reset` | comma-separated prop keys | Optional | Props the client wants reset to initial state before merge |
| `Purpose` | `"prefetch"` | Optional | Signals a speculative prefetch request |
| `Cache-Control` | `"no-cache"` | On forced reload | Forces a fresh response (used by `router.reload()`) |

## Server → Client (Response Headers)

| Header | Value | Required | Purpose |
|---|---|---|---|
| `X-Inertia` | `"true"` | Yes | Confirms this is an Inertia JSON response |
| `Vary` | `"X-Inertia"` | **Yes — on every response** | Tells caches to store HTML and JSON separately; must appear on both the initial HTML response and all subsequent Inertia JSON responses |
| `X-Inertia-Location` | absolute URL string | On 409 responses | Target URL for client-side `window.location` redirect (version mismatch or external redirect) |
| `Content-Type` | `application/json` (Inertia) or `text/html` (initial) | Yes | Disambiguates response type |

### Notes on `Vary: X-Inertia`

This header is **mandatory on all responses**, not just Inertia ones. Without it, a CDN or browser cache that stores a JSON response could serve it to a regular browser request (or vice versa), breaking the app. The server adapter middleware must inject this header in the response pipeline regardless of whether the current request is an Inertia request.
