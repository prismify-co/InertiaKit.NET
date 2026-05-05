# Redirects

## Standard Inertia Redirect (303 See Other)

After a mutating request (POST / PUT / PATCH / DELETE), the controller should redirect to a GET endpoint. The adapter enforces this via 303 rather than 302 to guarantee the follow-up request is a GET.

```
POST /users
  → Controller creates user
  → Returns: redirect to /users/42

Adapter:
  → Status 302 on a POST/PUT/PATCH/DELETE → promote to 303
  → Client follows 303 as a GET Inertia request
  → Server renders Users/Show page
```

**302 → 303 promotion rule:** Any 302 response produced by a non-GET request must be changed to 303. 307 Temporary Redirect and 308 Permanent Redirect preserve the original method and should not be promoted.

## Redirect Back

After a failed action, redirect the user back to the page they came from (using `Referer` header as the target).

```csharp
return Inertia.Back();          // Uses Referer header
return Inertia.Back("/users");  // Falls back to /users if no Referer
```

This returns 303 to the referring URL.

## External Redirects (409)

When redirecting to a URL outside the Inertia application (OAuth providers, payment gateways, external sites), a normal 3xx redirect would be followed by the XHR and discarded. Instead:

```csharp
return Inertia.Location("https://payment.example.com/checkout?session=abc");
```

Response:
```
HTTP/1.1 409 Conflict
X-Inertia-Location: https://payment.example.com/checkout?session=abc
```

The client reads `X-Inertia-Location` and executes `window.location = value`, causing a full browser navigation.

## Fragment Redirects (409)

URL fragments (`#section`) are handled client-side by the browser and cannot be followed by XHR. If the `Location` header of a redirect contains a fragment, the adapter must intercept it and use the same 409 + `X-Inertia-Location` mechanism:

```
Redirect to /users#active
  → Adapter detects "#" in Location
  → Returns 409 + X-Inertia-Location: /users#active
  → Client executes window.location = "/users#active"
```

## Flash Data on Redirect

Before returning any redirect response, flash data must be written to the session so it survives the round-trip. The next GET request will read it from session and inject it into `props`.

```csharp
return Inertia.SeeOther("/users")
    .Flash("message", "User created successfully.");
```

Implementation note: flash writing must happen **before** the response is sent — typically in the middleware's response finalization step.

## Summary Table

| Scenario | Method | Status | Mechanism |
|---|---|---|---|
| After POST creating a resource | POST | 303 | `Location` header to GET endpoint |
| After PUT/PATCH updating a resource | PUT/PATCH | 303 | `Location` header to GET endpoint |
| After DELETE | DELETE | 303 | `Location` header (often back to list) |
| Validation failure | POST | 422 | Inline errors in `props.errors`, no redirect |
| Redirect to external URL | Any | 409 | `X-Inertia-Location` header |
| Redirect containing `#fragment` | Any | 409 | `X-Inertia-Location` header |
| Asset version mismatch | GET | 409 | `X-Inertia-Location: <current-url>` |
| Redirect back (validation errors with session) | POST | 303 | `Location: Referer` + session flash |
