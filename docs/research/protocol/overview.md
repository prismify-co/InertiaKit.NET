# Protocol Overview

## Initial Page Visit (First Load)

The browser makes a normal HTTP GET request — no special headers. The server detects the absence of `X-Inertia` and returns a full HTML document containing:

1. The root `<div id="app">` mounting point.
2. A `<script type="application/json">` tag embedding the serialized page object.
3. All CSS/JS asset bundles needed to boot the frontend.

The JavaScript application boots, reads the embedded page object, and hydrates the component tree. From this point forward all navigation is handled by Inertia's client router.

## Subsequent Navigation (Inertia Requests)

When the user clicks an `<InertiaLink>` or the app calls `router.visit()`:

1. Client issues an XHR request to the target URL with `X-Inertia: true` (and related headers).
2. Server detects the header, builds the page object, and responds with **JSON only** (`Content-Type: application/json`) — no HTML wrapper.
3. Client swaps the current component and merges new props without a full page reload.

## Post-Mutation Redirect Flow (PRG Pattern)

After a POST / PUT / PATCH / DELETE the server should redirect rather than render inline:

1. Server returns `303 See Other` with a `Location` header pointing to a GET endpoint.
2. Client follows the redirect automatically.
3. The GET request is a normal Inertia request; server responds with the next page object.

This prevents form resubmission when the user presses the browser back button.

## Partial Reload Flow

When the client needs only a subset of props (e.g. after filtering):

1. Client sends `X-Inertia-Partial-Component` and `X-Inertia-Partial-Data` (or `X-Inertia-Partial-Except`) headers.
2. Server evaluates only the requested props, skips optional ones not listed.
3. Server returns the filtered page object.
4. Client merges the new props into the existing component state — non-requested props are preserved.

## Version Mismatch Flow

1. Client sends `X-Inertia-Version: <stored-version>` on every request.
2. If the server's current version differs and the request method is GET:
   - Server flashes the session (to preserve state).
   - Server returns `409 Conflict` with `X-Inertia-Location: <current-url>`.
3. Client receives 409, reads `X-Inertia-Location`, and executes `window.location = url`.
4. Browser performs a full page reload, picking up new assets.

## External Redirect Flow

When the server needs to redirect to a URL outside the Inertia app (e.g. OAuth callback):

1. Server returns `409 Conflict` with `X-Inertia-Location: <external-url>`.
2. Client reads the header and executes `window.location = url`.
