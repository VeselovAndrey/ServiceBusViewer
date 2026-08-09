# Project Decisions

## 2026-08-09: Kind-specific subscription rule responses

- The entity-details API exposes subscription rules as a polymorphic response hierarchy discriminated by `kind`.
- SQL, correlation, and unknown-filter rules contain only their applicable fields instead of sharing one nullable catch-all response type.
- A registered API JSON converter owns the polymorphic wire format so response models remain serialization-agnostic.
- The frontend models these payloads as a matching discriminated union.

## 2026-08-09: Combined application container

- Production publishing produces one `ServiceBusViewer` image that serves the compiled React SPA and `/api` endpoints from ASP.NET Core on port `8080`.
- The multi-stage API Dockerfile builds the SPA with Node 24 and copies its `dist` output into the published application's `wwwroot`; Node and nginx are not included in the final runtime image.
- Unknown `/api/*` routes remain API 404 responses and are excluded from the SPA fallback.
- Local development remains split: AppHost and manual workflows run Vite separately with `VITE_PROXY_TARGET` forwarding `/api` requests to the API.
- The standalone `servicebusviewer-web` image and frontend-upstream environment contract are retired.
- The final image sets an internal `SERVICEBUSVIEWER_RUNNING_IN_CONTAINER` marker. The session-state API exposes that positive signal so container-only UI guidance stays hidden for local and AppHost processes; hostname and filesystem heuristics are intentionally not used.

## 2026-08-09: Centralized API exception handling

- Minimal API handlers return successful results directly and allow exceptions to reach the registered `IExceptionHandler`.
- API failures use RFC `ProblemDetails` with `application/problem+json`, including `status`, `title`, and `detail`; validation failures also retain `errors`.
- Known viewer connection-state exceptions return `409`. Request-format and Azure Service Bus exceptions return `400` with actionable details.
- Unexpected exceptions return a sanitized `500` detail and are logged at error level. Exception messages and stack traces are never exposed for these failures.
- The frontend displays validation errors first, otherwise exactly one message: `detail`, then `title`, then a generic fallback.
