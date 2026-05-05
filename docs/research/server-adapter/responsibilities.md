# Server Adapter Responsibilities

A complete Inertia server adapter must fulfil the following responsibilities. Each is a hard requirement unless marked optional.

## A. Request Detection

- Detect Inertia requests by checking for the `X-Inertia: true` header.
- Detect partial reload requests by checking for `X-Inertia-Partial-Component`.
- Extract and store all Inertia request headers into a typed request context:
  - `X-Inertia-Version`
  - `X-Inertia-Partial-Component`
  - `X-Inertia-Partial-Data` (parse comma-separated list into a set)
  - `X-Inertia-Partial-Except` (parse comma-separated list into a set)
  - `X-Inertia-Error-Bag`
  - `X-Inertia-Reset` (parse comma-separated list)
  - `Purpose` (detect prefetch)

## B. Response Headers

- Set `Vary: X-Inertia` on **every response** regardless of request type — required for correct cache behaviour.
- Set `X-Inertia: true` on every Inertia JSON response.
- Set `Content-Type: application/json` on Inertia JSON responses.

## C. Asset Version Validation

- Compare `X-Inertia-Version` from the request against the application's current version.
- On mismatch **and GET method only**:
  1. Flash the session to preserve pending state.
  2. Return `409 Conflict` with `X-Inertia-Location: <current-url>`.
  3. Short-circuit the rest of the pipeline.
- Do not trigger a mismatch redirect on POST / PUT / PATCH / DELETE (those have their own redirect flow).

## D. Shared Data Resolution

- Evaluate all shared prop factories registered via `Share()` on every Inertia request.
- Evaluate shared-once prop factories and:
  - Include their values in `props` on first visit.
  - Include their keys in `onceProps` on subsequent visits (so the server skips re-sending and the client keeps its cached value).

## E. Prop Resolution & Filtering

For every render call:
1. Merge shared props + page props (page props win on collision).
2. Merge flash props from session.
3. Merge validation errors into `props.errors` (or scoped by error bag name).
4. For **partial reloads**:
   - If `X-Inertia-Partial-Data` is present: include only those prop keys.
   - If `X-Inertia-Partial-Except` is present: include all props except those keys.
   - Always include props wrapped as `Always` regardless of the above.
   - Evaluate lazy/optional closures only if their key is requested.
5. Skip evaluation of optional/deferred prop closures when not needed (performance).

## F. Page Object Construction

Build the page object with:
- `component`: from the render call
- `props`: resolved and filtered props (all closures evaluated)
- `url`: current request URL
- `version`: current asset version
- `mergeProps`, `prependProps`, `deepMergeProps`, `matchPropsOn`: from any merge-annotated props
- `deferredProps`: from any deferred-annotated props
- `onceProps`: keys of once props not sent this request
- `scrollRegions`, `rememberedState`, `encryptHistory`, `clearHistory`, `preserveFragment`: from response builder flags

## G. Response Serialization

- Serialize the page object to JSON.
- For Inertia requests: return JSON directly.
- For initial (non-Inertia) requests: embed JSON in the root view template.

## H. Redirect Normalization

- After PUT / PATCH / DELETE, convert any `302 Found` redirect to `303 See Other`.
- Detect if a redirect URL contains a fragment (`#`). If so:
  - Return `409 Conflict` with `X-Inertia-Location: <url-with-fragment>` so the client can navigate there natively.
- Flash session data before any redirect so it survives the round-trip.

## I. External Redirect Support

- Expose an API (e.g. `Inertia.Location(url)`) that returns `409 Conflict` + `X-Inertia-Location` header, bypassing Inertia's XHR follow behaviour.

## J. Validation Error Handling

- After a failed form submission, flash errors into the session.
- On the following request, read errors from session and inject them into `props.errors`.
- If `X-Inertia-Error-Bag` is present, scope the errors under that bag name: `props.errors = { [bagName]: errors }`.
- Support returning the first error per field (default) or all errors as an array (configurable).

## K. Flash Data Integration

- After any redirect, store flash data in the session.
- On the next request, read flash data from session and merge it into `props`.
- Flash data is **not** persisted in browser history (it must not appear in `rememberedState`).

## L. History Encryption (optional feature)

- When the response builder sets `EncryptHistory = true`, include it in the page object.
- When `ClearHistory = true`, include it in the page object.
- No server-side cryptography needed — this is purely a client-side feature; the server only signals intent via the page object flags.

## M. CSRF Protection

- Share the CSRF token as a prop or set it as a cookie.
- Validate the token on mutating requests before processing.

## N. SSR Gateway (optional feature)

- When SSR is enabled for a route:
  1. Build the page object as normal.
  2. POST the page object JSON to the configured SSR server URL.
  3. Receive rendered HTML from the SSR server.
  4. Embed the HTML in the root view (replacing the empty `<div id="app">`).
  5. Still embed the page object JSON script tag so the client can hydrate.

## O. Empty Response Handling

- If a controller returns an empty/null response on an Inertia request, redirect back to the referrer (or a configured fallback) rather than returning an empty 200.
