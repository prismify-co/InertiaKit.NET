# Existing .NET Implementations

Four C# packages exist. All were studied for architectural patterns and gaps. None should be copied wholesale — this project aims for a higher level of completeness, type safety, and framework agnosticism.

## 1. InertiaCore (kapi2289)

- **Package:** `AspNetCore.InertiaCore` on NuGet
- **Source:** https://github.com/kapi2289/InertiaCore
- **Maturity:** Stable releases (v0.0.5+), actively maintained

**Architecture:**
- Middleware-based shared prop injection
- Response builder pattern
- Static `Inertia` service accessed via DI
- Vite integration helpers
- Cycle-safe JSON serialization (prevents entity relationship infinite loops)

**Strengths:**
- Solid middleware foundation
- Handles the lazy prop closure pattern
- SSR support included

**Gaps:**
- No `X-Inertia-Partial-Except` header support
- No error bag scoping
- No history encryption flags
- No deferred props
- No merge/prepend/deepMerge prop strategies
- No once props
- Weak type safety (`object` everywhere)
- No testing utilities

---

## 2. InertiaNetCore (mergehez)

- **Package:** `InertiaNetCore` on NuGet
- **Source:** https://github.com/mergehez/InertiaNetCore
- **Maturity:** Actively maintained with demo application

**Architecture:**
- `IInertiaService` injected into controllers
- `InertiaOptions` configuration object
- Prop metadata wrapper system
- Flash message integration
- Demo app (Vue + Vite)

**Strengths:**
- Cleanest separation of concerns of the four
- History encryption flags in page object
- Deferred props grouping
- Merge props with group support
- Good documentation relative to others
- Demo app makes integration patterns clear

**Gaps:**
- Once props not fully implemented
- No `X-Inertia-Partial-Except` support
- No precognition support
- No `matchPropsOn` for identity-based merge
- Prepend strategy missing
- Testing utilities absent

---

## 3. inertia-dotnet (idotta)

- **Source:** https://github.com/idotta/inertia-dotnet
- **Package:** Not yet published to NuGet (in development)
- **Maturity:** Phase 5 — most complete implementation, not production released

**Architecture:**
- Property interface hierarchy (`IOptionalProperty`, `IDeferredProperty`, `IAlwaysProperty`, etc.)
- Comprehensive middleware with full lifecycle coverage
- TagHelpers for Razor view integration
- SSR with health monitoring
- Fluent assertion utilities for testing (`InertiaAssertions`)
- Git submodule tracking `inertia-laravel` v2.0.14 as reference

**Strengths:**
- Most protocol-complete of all existing .NET implementations
- First to have testing utilities
- Proper property interface hierarchy matches this project's goals
- Sample projects for React, Vue, and SSR
- History encryption, clear history, once props all present

**Gaps:**
- Not production released
- Documentation thin
- No NuGet package
- Deep ASP.NET Core coupling (not framework-agnostic)

---

## 4. INERTIAJS.ASPNETCORE.ADAPTER

- **Package:** `INERTIAJS.ASPNETCORE.ADAPTER` on NuGet
- **Maturity:** Limited maintenance, early version

**Architecture:** Basic rendering with shared props and validation errors.

**Gaps:** Minimal feature set, not actively maintained. Not recommended as reference.

---

## Comparative Feature Matrix

| Feature | InertiaCore | InertiaNetCore | inertia-dotnet | Target (this project) |
|---|---|---|---|---|
| Basic middleware | ✓ | ✓ | ✓ | Required |
| Shared props | ✓ | ✓ | ✓ | Required |
| Optional/Lazy props | ✓ | ✓ | ✓ | Required |
| Always props | Partial | ✓ | ✓ | Required |
| Deferred props | Basic | ✓ | ✓ | Required |
| Once props | Minimal | Partial | ✓ | Required |
| Merge (append) | Basic | ✓ | ✓ | Required |
| Merge (prepend) | ✗ | ✗ | ✓ | Required |
| Merge (deep) | ✗ | ✗ | ✓ | Required |
| Merge (matchOn) | ✗ | ✗ | ✓ | Required |
| Partial-Except header | ✗ | ✗ | Partial | Required |
| Error bags | ✗ | ✗ | Partial | Required |
| History encryption | ✗ | ✓ | ✓ | Required |
| Clear history | ✗ | ✓ | ✓ | Required |
| SSR | ✓ | ✓ | ✓ | Required |
| Vary header | Partial | ✓ | ✓ | Required |
| 302→303 promotion | Partial | ✓ | ✓ | Required |
| Fragment redirect (409) | ✗ | Partial | ✓ | Required |
| External redirect (409) | ✓ | ✓ | ✓ | Required |
| Version mismatch (409) | ✓ | ✓ | ✓ | Required |
| Testing utilities | ✗ | ✗ | ✓ | Required |
| Framework-agnostic | ✗ | ✗ | ✗ | Required |
| Prop type interfaces | ✗ | Partial | ✓ | Required |
| Compile-time safety | Low | Medium | High | High |

## Architectural Patterns Worth Adopting

1. **Property interface hierarchy** (from inertia-dotnet) — each prop type (`Optional`, `Always`, `Deferred`, `Once`, `Merge`) implements a marker interface; the middleware inspects the interface to determine evaluation and inclusion rules.
2. **Fluent response builder** (common to all) — `Inertia.Render(...).Flash(...).EncryptHistory()` pattern is well-established and expected by users.
3. **Configuration via middleware override** (from Laravel adapter pattern, used by all) — users subclass `HandleInertiaRequestsBase` and override `Version()`, `Share()`, `RootView`.
4. **Testing assertion fluent API** (from inertia-dotnet) — `response.Should().HasComponent("Users/Index").HasProp("users")` pattern should be standard from day one.

## What This Project Must Do Better

1. **Framework-agnostic core** — the domain model and prop resolution logic must not depend on ASP.NET Core types. Framework-specific adapters (ASP.NET Core, FastEndpoints) sit on top.
2. **Complete protocol support** — every header, every flag, every prop type from the official spec.
3. **Strong type system** — `IInertiaProperty` hierarchy, typed `IPageObject`, typed `IInertiaRequest`.
4. **Testing first** — assertion helpers ship in the core package, not as an afterthought.
5. **Documentation** — match or exceed the Laravel adapter's documentation quality.
