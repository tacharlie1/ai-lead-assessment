# AI Review: Sample.DotNetApp (feature/order-confirmation)

_Provider: simulated · Generated: 2026-11-17 20:58 · 2 files changed (+16 / -2)_

> This review is advisory. A human reviewer makes the final decision.

## Summary

- Modifies `src/Sample.DotNetApp/OrderService.cs` (+15 / -1 lines).
- Modifies `src/Sample.DotNetApp/appsettings.json` (+1 / -1 lines).

## Potential Risks

- Public member changed or removed in `OrderService.cs`: `public decimal GetTotal(int orderId)`. Callers may break. Check usages before merging.
- In `OrderService.cs`, `public async void SendConfirmation(int orderId)`: `async void` means callers cannot await it or catch its exceptions. Return `Task` instead.
- In `OrderService.cs`, `var response = _http.GetAsync($"https://example.com/notify/{orderId}")...`: blocking on a task can deadlock and ties up threads. Use `await` instead.
- In `OrderService.cs`, `catch (Exception)`: catching every exception can hide real failures. Catch the specific exceptions you expect.
- In `OrderService.cs`, `private const string ApiKey = "***";`: this looks like a hard-coded secret. Move it to configuration or a secrets vault, and rotate it if it was ever committed.
- `src/Sample.DotNetApp/appsettings.json` is a project or configuration file. Check the change is right for every environment.

## Suggested Tests

- No test changes accompany `OrderService.cs`. Add or update tests for the changed behaviour.

## Suggestions

- Resolve or create a ticket for the note added in `OrderService.cs`: `// TODO: convert currency properly`.

## Files Changed

| File | Change | Added | Removed |
|---|---|---:|---:|
| `src/Sample.DotNetApp/OrderService.cs` | Modified | 15 | 1 |
| `src/Sample.DotNetApp/appsettings.json` | Modified | 1 | 1 |
