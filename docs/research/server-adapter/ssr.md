# Server-Side Rendering (SSR)

## Overview

Inertia SSR renders the initial page HTML on the server via a separate Node.js process, rather than letting the browser execute JavaScript to paint the first frame. The .NET adapter acts as an HTTP client to this SSR gateway.

## Architecture

```
Browser → .NET Server → (1) Build page object
                      → (2) POST page object to SSR gateway (Node.js)
                      ← (3) Receive rendered HTML fragment
                      → (4) Embed HTML + page object JSON in root view
                      ← (5) Return complete HTML to browser
Browser hydrates on top of rendered HTML (no flash of blank content)
```

## SSR Gateway Contract

**Default endpoint:** `http://127.0.0.1:13714/render`

**Request:**
```
POST /render
Content-Type: application/json

{
  "component": "Users/Index",
  "props":     { "users": [...] },
  "url":       "/users",
  "version":   "abc123"
}
```

**Response:**
```json
{
  "html": "<div class=\"users-index\">...</div>"
}
```

The gateway may also return a `head` field containing `<meta>` / `<title>` / `<link>` tags to inject into `<head>`.

```json
{
  "head": ["<title>Users</title>", "<meta name=\"description\" content=\"...\">"],
  "html": "<div ...>...</div>"
}
```

## Root View Integration

On initial (non-Inertia) requests when SSR is enabled:

```html
<!DOCTYPE html>
<html>
  <head>
    <!-- SSR head tags injected here -->
    <title>Users</title>
  </head>
  <body>
    <!-- SSR-rendered HTML replaces the empty div -->
    <div id="app"><div class="users-index">...</div></div>

    <!-- Page object always embedded for client hydration -->
    <script type="application/json" id="app-data">
      { "component": "Users/Index", "props": {...}, "url": "/users", "version": "abc123" }
    </script>

    <!-- Asset bundles -->
    <script src="/build/app.js"></script>
  </body>
</html>
```

Without SSR the `<div id="app">` would be empty and JavaScript would render the component client-side.

## Failure Handling

When the SSR gateway is unavailable or returns an error:
- The adapter should fall back to returning the empty `<div id="app">` and let the client render.
- Log the SSR error for observability.
- Do not expose SSR errors to the end user.

## Configuration

```csharp
public interface IHandleInertiaRequests
{
    bool   SsrEnabled          { get; }  // default: false
    string SsrUrl              { get; }  // default: "http://127.0.0.1:13714/render"
    string[] SsrExcludedRoutes { get; }  // routes that skip SSR even when enabled
}
```

## When to Use

SSR is valuable for:
- Public-facing pages where SEO and first contentful paint matter.
- Pages shared on social media (Open Graph tags need to be in the HTML).

SSR is typically skipped for:
- Authenticated / behind-login routes.
- Admin panels.
- API routes.
