# Validation Errors

## Behaviour

Unlike a traditional PRG flow, Inertia does **not** redirect after a validation failure. Instead:

1. Controller validates the request. Validation fails.
2. Server returns `422 Unprocessable Entity` with errors embedded in `props.errors`.
3. Client stays on the current component and displays the errors.
4. The form retains its filled-in values (handled client-side by the Inertia form helper).

## Error Shape

**Single error per field (default):**
```json
{
  "props": {
    "errors": {
      "email":    "The email field is required.",
      "password": "Password must be at least 8 characters."
    }
  }
}
```

**All errors per field (configurable):**
```json
{
  "props": {
    "errors": {
      "email":    ["Email is required.", "Email must be valid."],
      "password": ["Too short.", "Must include a number."]
    }
  }
}
```

Whether to return the first or all errors per field is an adapter configuration option.

## Error Bags

When a page has multiple independent forms, errors can be scoped to a named bag. The client sends the bag name in `X-Inertia-Error-Bag`; the server scopes the errors under that key.

**Request header:**
```
X-Inertia-Error-Bag: loginForm
```

**Page object:**
```json
{
  "props": {
    "errors": {
      "loginForm": {
        "email":    "Email is required.",
        "password": "Password is required."
      }
    }
  }
}
```

Without an error bag header, errors are placed directly at `props.errors`.

## Session Storage Pattern

Many frameworks use a redirect-and-display cycle even for validation:

1. Controller validation fails → stores errors in session flash → returns redirect back.
2. GET request arrives → middleware reads errors from session → injects into `props.errors`.

The adapter must:
- Detect errors in the session on each request and inject them before building the page object.
- Clear errors from the session after injecting (they are one-time flash data).
- Scope errors by error bag name when `X-Inertia-Error-Bag` was set on the originating request.

## Always-Injected Errors

If the controller returns an `IInertiaResponse` that already has inline errors (not session-based), those are merged directly into `props.errors` without the flash cycle.

## Interaction with Partial Reloads

Errors injected from session are treated as `Always` props — they appear in the page object even during partial reloads, regardless of `X-Inertia-Partial-Data`. This ensures the component always has access to current error state.
