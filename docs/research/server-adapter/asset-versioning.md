# Asset Versioning

## Purpose

When assets are redeployed (new JS bundle, new CSS), clients holding an older version will fetch fresh assets on their next hard navigation. Inertia enforces this by comparing a version string on every request and triggering a full browser reload when a mismatch is found.

## Version String

The version string is an opaque identifier. Any string that changes on each deployment works:

| Strategy | Example |
|---|---|
| Git commit hash | `"a3f9b2c"` |
| File manifest hash | `MD5(File.ReadAllText("public/build/manifest.json"))` |
| Semantic version | `"1.4.2"` |
| Build timestamp | `"20250505-143012"` |

The adapter reads the current version and:
1. Returns it in the `version` field of every page object.
2. Compares it to the `X-Inertia-Version` header on every Inertia GET request.

## Mismatch Flow

```
Client sends: X-Inertia-Version: "old"
Server current: "new"

Server:
  1. Detects mismatch (old ≠ new)
  2. Flashes the session (preserves any pending flash/error data)
  3. Returns 409 Conflict
     X-Inertia-Location: https://app.example.com/current-url

Client receives 409:
  1. Reads X-Inertia-Location header
  2. Executes window.location = headerValue
  3. Browser performs full page load
  4. New assets are loaded as part of the fresh HTML
```

## Rules

- Only check version on **GET** requests. POST / PUT / PATCH / DELETE are not version-checked — they proceed to their redirect flow regardless.
- The version comparison is exact string equality. Semver ordering is not applied.
- Flash the session **before** returning the 409 so that any data the controller had set is not lost across the reload.

## Configuration Interface

```csharp
public interface IHandleInertiaRequests
{
    // Return null to disable version checking
    string? Version(HttpContext context);
}
```

Returning `null` from `Version()` disables version checking for that request (useful for routes that don't participate in the asset lifecycle).

## Practical Setup (Vite)

Vite writes a `manifest.json` to the public build directory. A common implementation:

```csharp
public override string? Version(HttpContext context)
{
    var manifestPath = Path.Combine(env.WebRootPath, "build", "manifest.json");
    if (!File.Exists(manifestPath)) return null;
    using var md5 = MD5.Create();
    var bytes = File.ReadAllBytes(manifestPath);
    return Convert.ToHexString(md5.ComputeHash(bytes)).ToLowerInvariant();
}
```
