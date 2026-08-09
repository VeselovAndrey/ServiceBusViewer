# Project Decisions

## 2026-08-09: Centralized API exception handling

- Minimal API handlers return successful results directly and allow exceptions to reach the registered `IExceptionHandler`.
- API failures use RFC `ProblemDetails` with `application/problem+json`, including `status`, `title`, and `detail`; validation failures also retain `errors`.
- Known viewer connection-state exceptions return `409`. Request-format and Azure Service Bus exceptions return `400` with actionable details.
- Unexpected exceptions return a sanitized `500` detail and are logged at error level. Exception messages and stack traces are never exposed for these failures.
- The frontend displays validation errors first, otherwise exactly one message: `detail`, then `title`, then a generic fallback.
