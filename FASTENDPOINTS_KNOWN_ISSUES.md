# FastEndpoints Integration - Known Issues

## Status
As of the latest attempts, FastEndpoints integration has **2/15 tests passing (13% success rate)**.

## Problem Statement
FastEndpoints has a fundamental architectural mismatch with the middleware-based Inertia response handling:

1. **Auto-Response Issue**: FastEndpoints sends a default 204 (No Content) response when an endpoint handler completes without explicitly writing a response or returning an IResult that's recognized.

2. **Timing Issue**: Even when endpoints return `Task<IResult>`, FastEndpoints appears to either:
   - Not recognize our IResult as handling the response
   - Send the 204 response before FastEndpoints processes the IResult.ExecuteAsync()

3. **Response Lifecycle**: By the time the Inertia middleware gets control after `await next(context)`, the response has already started (response.HasStarted == true), preventing the middleware from:
   - Setting the status code
   - Adding headers
   - Writing the JSON response body

##  Technical Details

### What Works (MinimalAPI, MVC)
- **MinimalAPI**: Endpoints call `ctx.SetInertiaResult()` and return nothing. No auto-response occurs, middleware handles everything.
- **MVC**: Controllers return `IActionResult` (InertiaResult), middleware processes it via ExecuteResultAsync().

### What Doesn't Work (FastEndpoints)
- Endpoints returning `Task<IResult>` still get 204 No Content
- Endpoints that just store result and return Task still get 204 No Content
- Middleware defensive checks (`if (!context.Response.HasStarted)`) prevent exceptions but mean responses remain empty

### Attempted Solutions
1. ✗ Making InertiaResult implement IResult with ExecuteAsync()
2. ✗ Returning IResult from endpoint handlers
3. ✗ Returning Task<IResult> from HandleAsync
4. ✗ Defensive response.HasStarted checks in middleware
5. ✗ Manually calling IResult.ExecuteAsync before returning

### Passing Tests (2/15)
The 2 tests that pass appear to be edge cases that don't need response bodies or use 303 redirects that don't depend on middleware response writing.

## Potential Solutions (Not Yet Implemented)

1. **Custom PostExecutionHandler**: FastEndpoints might have a plugin point that runs BEFORE auto-response
2. **Global Endpoint Configuration**: Disable auto-response behavior globally for FastEndpoints
3. **Wrapper Result Type**: Create a FastEndpoints-specific IResult that prevents auto-response
4. **Endpoint Base Class Changes**: Override internal FastEndpoints methods to handle response lifecycle differently
5. **Separate FastEndpoints Middleware**: Create a FastEndpoints-specific middleware that runs inside endpoint execution

## Recommendations

For now, Inertia.NET works well with MinimalAPI and MVC. FastEndpoints integration should be marked as experimental or unsupported until:
- FastEndpoints documentation clarifies the response lifecycle for custom IResult implementations
- A working solution is found that doesn't duplicate middleware logic in every endpoint
- The two-test pass rate improves significantly

Users needing FastEndpoints should:
- Use MinimalAPI or MVC for Inertia routes
- Or manually implement response writing in FastEndpoints endpoints (duplicating middleware logic)
