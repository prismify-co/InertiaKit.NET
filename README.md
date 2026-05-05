# InertiaKit for .NET

InertiaKit for .NET is an Inertia.js server adapter for ASP.NET Core. It keeps the C# API surface under `InertiaKit.*` while using the `InertiaKit.NET.*` naming family for the repository, solution, and NuGet packages.

This repository contains the core protocol model, the ASP.NET Core adapter, a FastEndpoints integration layer, example applications, and protocol-focused tests.

Repository: https://github.com/Prismify-co/InertiaKit.NET

## Packages

The current solution targets .NET 10 and is organized around three publishable packages:

| NuGet Package | Namespace Root | Purpose |
| --- | --- | --- |
| `InertiaKit.NET.Core` | `InertiaKit.Core` | Page object model, request parsing, prop abstractions, and prop resolution/serialization support. |
| `InertiaKit.NET.AspNetCore` | `InertiaKit.AspNetCore` | Dependency injection, middleware, rendering service, shared props, SSR bridge, flash/errors integration, and MVC/Minimal API support. |
| `InertiaKit.NET.FastEndpoints` | `InertiaKit.FastEndpoints` | FastEndpoints-specific registration and base endpoint types built on top of the ASP.NET Core adapter. |

Package metadata is configured directly in the library projects. Each package also embeds this README so the NuGet listing stays aligned with the repository docs.

## Supported Features

The implementation in this repository currently covers the core Inertia server-adapter behaviors exercised by the examples and tests:

- Initial HTML responses with an embedded page object for first visits.
- JSON page responses for subsequent Inertia requests.
- Asset version checking with `409 Conflict` and `X-Inertia-Location` reloads.
- Partial reload handling via `X-Inertia-Partial-*` headers.
- Lazy prop types: eager, optional, always, deferred, once, and merge props.
- Merge annotations for append, prepend, deep merge, and `MatchOn(...)` identity hints.
- Session-backed flash data and validation errors, including error-bag scoping.
- `422 Unprocessable Entity` responses for mutation requests that return validation errors inline.
- `303 See Other` redirect normalization after non-GET requests.
- Safe external redirects via `InertiaLocationResult`.
- Same-origin back redirects via `IInertiaService.Back(...)`.
- Prefetch short-circuit handling.
- Optional server-side rendering through a Node SSR gateway.
- Page metadata flags such as remembered state, scroll regions, history encryption, and fragment preservation.

## Solution Layout

| Path | Description |
| --- | --- |
| `src/` | Production code. |
| `tests/` | Unit, middleware/protocol, and end-to-end tests. |
| `examples/MinimalApi` | Minimal API example covering shared props, optional/deferred/merge props, validation, redirects, and external locations. |
| `examples/Mvc` | MVC example covering controller-based rendering, shared props, optional/always/deferred props, and redirect flows. |
| `examples/FastEndpointsExample` | FastEndpoints example covering endpoint-based rendering, once/deferred props, merge annotations, and PRG handling. |
| `docs/research/` | Protocol and adapter research that informed the implementation. |
| `InertiaKit.NET.slnx` | Solution entry point. |

## Naming

The naming split is intentional:

- Use `InertiaKit for .NET` in branding and documentation.
- Use `InertiaKit.NET.*` for package IDs, project files, and the solution.
- Use `InertiaKit.*` for C# namespaces so consumer code stays clean.

## How It Fits Together

At runtime the ASP.NET Core adapter parses the incoming Inertia headers, merges shared props with page props, resolves any lazy prop wrappers, builds the page object, and returns either:

- an HTML document with embedded JSON for the initial visit, or
- an `application/json` page object for Inertia XHR navigation.

For mutating requests it also normalizes redirect behavior and can surface validation errors either inline or through session-backed PRG flows.

## Quick Start

Choose the adapter that matches your host style.

### Minimal API / ASP.NET Core

```csharp
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using InertiaKit.Core.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInertia(options =>
{
    options.RootView = "App";
    options.VersionResolver = () => "1.0.0";
});

builder.Services.AddInertiaHandler<AppInertiaHandler>();

var app = builder.Build();

app.UseInertia();

app.MapGet("/dashboard", (IInertiaService inertia) =>
    inertia.Render("Dashboard/Index", new Dictionary<string, object?>
    {
        ["summary"] = new { users = 120, revenue = 45_000 },
        ["topUsers"] = inertia.Optional(() => UserRepository.Top(10)),
        ["monthlyChart"] = inertia.Defer(() => Analytics.MonthlyData(), "charts"),
        ["activity"] = inertia.Merge(ActivityFeed.Page(1)).MatchOn("id"),
    }));

app.Run();

sealed class AppInertiaHandler : HandleInertiaRequestsBase
{
    public override string RootView => "App";

    public override string? Version(HttpContext context) => "1.0.0";

    public override void Share(IInertiaShareBuilder shared, HttpContext context)
    {
        shared.Add("auth", new
        {
            user = context.User.Identity?.IsAuthenticated == true
                ? new { name = context.User.Identity.Name }
                : null,
        });
    }
}
```

### MVC

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddInertia(options =>
{
    options.RootView = "App";
    options.VersionResolver = () => "1.0.0";
});
builder.Services.AddInertiaHandler<AppInertiaHandler>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseInertia();
app.UseAuthorization();
```

In controllers, return `inertia.Render(...)` directly or store the result with `HttpContext.SetInertiaResult(...)`.

### FastEndpoints

```csharp
using FastEndpoints;
using InertiaKit.FastEndpoints;
using InertiaKit.FastEndpoints.Extensions;

builder.Services.AddFastEndpoints();
builder.Services.AddInertiaForFastEndpoints(options =>
{
    options.RootView = "App";
    options.VersionResolver = () => "1.0.0";
});

var app = builder.Build();

app.UseInertiaWithFastEndpoints();
app.UseFastEndpoints();

public sealed class UsersEndpoint : InertiaEndpoint
{
    public override void Configure()
    {
        Get("/users");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        RenderAsync("Users/Index", new Dictionary<string, object?>
        {
            ["users"] = UserRepository.All(),
            ["stats"] = Inertia.Defer(() => UserRepository.Stats()),
            ["countries"] = Inertia.Once(() => CountryRepository.All()),
        }, ct);
}
```

`UseInertiaWithFastEndpoints()` must run before `UseFastEndpoints()`. If you rely on flash data or validation errors, place it after `UseSession()`.

## Rendering and Navigation API

The main application-facing API lives on `IInertiaService`:

- `Render(component)` and `Render(component, props)` create an `InertiaResult`.
- `Location(url)` forces a full navigation with `409 + X-Inertia-Location`.
- `Back(context, fallback)` redirects to the same-origin referrer or a safe fallback.
- `SeeOther(url)` creates an explicit `303 See Other` redirect.
- `Optional`, `Always`, `Defer`, `Once`, and `Merge` create typed prop wrappers.

`InertiaResult` also supports additional page-level hints:

- `WithFlash(key, value)`
- `WithRememberedState(state)`
- `WithScrollRegions(regions)`
- `WithEncryptHistory()`
- `WithClearHistory()`
- `WithPreserveFragment()`

## Examples

The example applications are the best place to see end-to-end usage:

- [examples/MinimalApi](examples/MinimalApi) shows shared props, inline validation errors, external redirects, optional/deferred props, and merge annotations.
- [examples/Mvc](examples/Mvc) shows MVC controller integration, shared props, partial reload behavior, and deferred/merge props.
- [examples/FastEndpointsExample](examples/FastEndpointsExample) shows FastEndpoints base endpoints, once props, deferred props, and `MatchOn("id")` merge hints.

## Running the Repo

Build the solution:

```bash
dotnet build InertiaKit.NET.slnx
```

Run the full test suite:

```bash
dotnet test InertiaKit.NET.slnx
```

Run an example app:

```bash
dotnet run --project examples/MinimalApi/MinimalApi.csproj
dotnet run --project examples/Mvc/Mvc.csproj
dotnet run --project examples/FastEndpointsExample/FastEndpointsExample.csproj
```

## Testing Strategy

The repository includes three layers of verification:

- Core unit tests for request parsing, page object construction, merge hints, and lazy prop behavior.
- ASP.NET Core middleware/protocol tests for status codes, redirects, version checks, error bags, flash handling, and protocol compliance.
- End-to-end example tests for Minimal API, MVC, and FastEndpoints behavior.

## Research Notes

The implementation is backed by protocol and prior-art research under [docs/research](docs/research), including:

- [docs/research/protocol/overview.md](docs/research/protocol/overview.md)
- [docs/research/server-adapter](docs/research/server-adapter)
- [docs/research/prior-art/dotnet-implementations.md](docs/research/prior-art/dotnet-implementations.md)

## Status

This repository is already structured as a reusable multi-project solution with working examples and automated tests. If you are adopting it in another application, start from the example closest to your host style and then trim it down to your needs.