# HTTP Status Codes

| Code | Trigger Condition | Key Headers | Client Behaviour |
|---|---|---|---|
| `200 OK` | Successful GET or any Inertia JSON response | `X-Inertia: true`, `Content-Type: application/json` | Swap component and merge props |
| `303 See Other` | After any PUT / PATCH / DELETE (or any POST that redirects) | `Location: <url>` | Follow redirect as a GET Inertia request |
| `409 Conflict` | Asset version mismatch OR external redirect needed | `X-Inertia-Location: <url>` | Execute `window.location = header-value` (full reload) |
| `422 Unprocessable Entity` | Validation failure on an Inertia form submission | `X-Inertia: true` — errors embedded in `props.errors` | Keep current component, display errors |
| `204 No Content` | Precognition validation request passed | — | Signal to the form: no validation errors |
| `302 Found` (incoming) | Standard framework redirect after POST/PUT/PATCH/DELETE | `Location: <url>` | Adapter **must** promote to 303 for non-GET methods |

## Notes

### Why 303 instead of 302 after mutations

HTTP 302 on a POST is ambiguous — some browsers re-POST on follow. RFC 7231 defines 303 as "redirect to a GET". Inertia mandates that the server adapter converts any 302 produced after a PUT / PATCH / DELETE into 303. The client always follows with a GET.

### Why 409 for version mismatch

409 "Conflict" was chosen to indicate a state conflict (client version ≠ server version). It is distinct from 3xx redirects so the client can apply the special `window.location` reload logic rather than its normal Inertia navigation path.

### 409 for external redirects

Because Inertia's XHR follows 3xx redirects transparently, a normal redirect to an external URL would silently fetch the external HTML and discard it. Instead the server returns 409 + `X-Inertia-Location`, and the client navigates the browser there directly.

### Validation errors — no redirect

Unlike the classic PRG pattern, Inertia returns validation errors **inline** (422 with errors in props) on the same URL. The client stays on the form page and displays the errors without a round-trip redirect.
