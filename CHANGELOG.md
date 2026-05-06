# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-06

### Added

- History-security controls across all adapters: global history-encryption defaults, Minimal API route metadata, MVC action/controller attributes, and FastEndpoints helper methods for encrypted history and history-key rotation.
- Antiforgery integration helpers for Inertia browser clients: `AddInertiaAntiforgery()`, `HttpContext.SetXsrfTokenCookie()`, and `IInertiaShareBuilder.AddCsrfToken(...)`.
- Shared asset-shell markup builder used by the built-in HTML renderers, including Vite React Fast Refresh preamble injection for development `.jsx` entrypoints.
- Expanded example coverage with authenticated profile flows, signed-out clear-history pages, shared design-system styling, optional catch-all docs routes, and a dedicated FastEndpoints Vite client source tree.

### Changed

- README expanded to document package boundaries, supported features, history controls, antiforgery helpers, and example workflows in more detail.
- Example applications now share a more consistent design system and product-style navigation across Minimal API, MVC, and FastEndpoints demos.
- Frontend and server-side tests now cover history encryption, clear-history flows, antiforgery helpers, and the richer example navigation flows.

### Fixed

- Initial HTML responses rendered through the asset shell no longer produce a blank page when the Vite dev server serves React `.jsx` entrypoints.

## [1.1.0] - 2026-05-05

### Added

- **Pluggable HTML renderer abstraction** (`IInertiaRenderer`, `InertiaRenderContext`, `InertiaAssetShellOptions`) — decouple HTML shell production from the response executor. Three built-in implementations:
  - `DefaultHtmlShellInertiaRenderer` — inline HTML string for Minimal API / FastEndpoints (`AddInertia`)
  - `AssetShellInertiaRenderer` — load the shell from a Vite-built asset file (`InertiaAssetShellOptions`)
  - `MvcViewInertiaRenderer` — delegate to a Razor view (`AddInertiaMvc`)
- **React client app** for the MinimalApi example (Vite + `@inertiajs/react`)
- **Vue client app** for the Mvc example (Vite + `@inertiajs/vue3`)
- Pre-built `wwwroot` assets wired into the FastEndpoints example
- Shared `resolveInertiaPage.js` helper for dynamic page-component resolution across all client apps
- NPM workspace (`package.json`) coordinating all client packages
- **Playwright browser E2E suite** targeting all three example servers (`tests/frontend-e2e/`)
- Frontend unit tests for the shared `resolveInertiaPage` helper (`tests/frontend-utils/`)

### Changed

- `InertiaOptions` gains `RootView` and `AssetShell` configuration properties
- `InertiaResponseExecutor` simplified by delegating all HTML production to the active renderer
- `HandleInertiaRequestsBase` extended to support new renderer pipeline
- `ServiceCollectionExtensions` updated with renderer registration helpers

## [1.0.0] - 2026-05-05

### Added

- `InertiaKit.NET.Core` — core Inertia protocol types, `PageObject`, prop resolution (`IInertiaProps`), deferred / once / merge / always / optional prop variants, and `InertiaShareBuilder`
- `InertiaKit.NET.AspNetCore` — ASP.NET Core middleware (`InertiaMiddleware`), `InertiaResult`, `InertiaLocationResult`, `HandleInertiaRequestsBase` middleware hook, and `IInertiaService`
- `InertiaKit.NET.FastEndpoints` — `InertiaEndpoint<TRequest, TResponse>` base for FastEndpoints integration
- Example apps for Minimal API, MVC, and FastEndpoints
- Full test suite covering protocol compliance, middleware behaviour, prop resolution security, and E2E integration

[1.1.0]: https://github.com/prismify-co/InertiaKit.NET/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/prismify-co/InertiaKit.NET/releases/tag/v1.0.0
[1.2.0]: https://github.com/prismify-co/InertiaKit.NET/compare/v1.1.0...v1.2.0
