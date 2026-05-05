# Inertia.js Protocol Research

Research into the Inertia.js protocol, server-adapter contract, and .NET prior art — foundational input for the InertiaKit for .NET domain model.

## Structure

```
research/
  protocol/
    overview.md          — request/response lifecycle (initial visit, navigation, partial reloads)
    http-headers.md      — every header, direction, and semantics
    page-object.md       — page object schema with all fields
    status-codes.md      — HTTP status codes and when each is used
  server-adapter/
    responsibilities.md  — exhaustive list of what a server adapter must do
    middleware-pipeline.md — step-by-step middleware execution order
    prop-system.md       — all prop types (Optional, Always, Deferred, Merge, Once, Prepend, DeepMerge)
    asset-versioning.md  — versioning mechanism and 409 mismatch flow
    validation-errors.md — error handling, error bags, and session integration
    redirects.md         — redirect rules, 303 normalization, external redirects
    ssr.md               — SSR protocol and Node.js gateway contract
  prior-art/
    dotnet-implementations.md — existing .NET adapters, architecture, gaps
  domain-model/
    interfaces.md        — recommended C# interfaces, records, and enums
```

## Sources

- https://inertiajs.com/the-protocol
- https://github.com/inertiajs/inertia (client core)
- https://github.com/inertiajs/inertia-laravel (reference server adapter)
- https://github.com/inertiajs/inertia-rails (reference server adapter)
- https://github.com/kapi2289/InertiaCore
- https://github.com/mergehez/InertiaNetCore
- https://github.com/idotta/inertia-dotnet
