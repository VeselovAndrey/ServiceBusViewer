# Project Decisions

## 2026-08-09: Combined application container

- Production publishing produces one `ServiceBusViewer` image that serves the compiled React SPA and `/api` endpoints from ASP.NET Core on port `8080`.
- The multi-stage API Dockerfile builds the SPA with Node 24 and copies its `dist` output into the published application's `wwwroot`; Node and nginx are not included in the final runtime image.
- Unknown `/api/*` routes remain API 404 responses and are excluded from the SPA fallback.
- Local development remains split: AppHost and manual workflows run Vite separately with `VITE_PROXY_TARGET` forwarding `/api` requests to the API.
- The standalone `servicebusviewer-web` image and frontend-upstream environment contract are retired.

## 2026-08-09: Centralized API exception handling

- Minimal API handlers return successful results directly and allow exceptions to reach the registered `IExceptionHandler`.
- API failures use RFC `ProblemDetails` with `application/problem+json`, including `status`, `title`, and `detail`; validation failures also retain `errors`.
- Known viewer connection-state exceptions return `409`. Request-format and Azure Service Bus exceptions return `400` with actionable details.
- Unexpected exceptions return a sanitized `500` detail and are logged at error level. Exception messages and stack traces are never exposed for these failures.
- The frontend displays validation errors first, otherwise exactly one message: `detail`, then `title`, then a generic fallback.
