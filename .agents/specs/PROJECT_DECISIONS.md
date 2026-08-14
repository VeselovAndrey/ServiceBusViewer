# Project Decisions

## 2026-08-13: Aspire hosting package defaults

- The reusable `SolidCode.Aspire.Hosting.ServiceBusViewer` package registers the published `ghcr.io/veselovandrey/servicebusviewer:latest` container image by default and exposes convenience helpers for the supported `SERVICEBUSVIEWER_*` runtime environment variables.
- `src\ServiceBusViewer.AppHost` keeps the existing local-project development path by default and uses the published container path only when `AppHost:UsePublishedServiceBusViewerContainer=true` is configured in its appsettings.

## 2026-08-12
- Direct-entity access excludes standalone topics
  - Direct-entity mode supports queues and topic subscriptions. A standalone topic is not a valid direct target because messages cannot be peeked or received from a topic itself.
  - Without management access, `QueueOrTopicName` identifies a queue when `SubscriptionName` is absent and identifies the parent topic when `SubscriptionName` is present.
  - Namespace mode can still select discovered topics, including for sending messages.
- Send completion and message refresh are separate operations
  - `POST /api/viewer/send` completes only after the Service Bus send succeeds and returns `204 No Content`; immediate visibility in the peeked-message list is not part of the send contract.
  - The frontend records send success immediately, then calls `/api/viewer/refresh` separately. A refresh failure preserves the send confirmation and stale message list and directs the user to Refresh instead of resending.
  - Refresh replaces only the peeked-message list and preserves the displayed received message and send confirmation.
  - Receive retains its `200` viewer-state response and treats its post-receive peek as best effort so a successfully consumed message is not reported as a failed receive.

## 2026-08-11: Unified Service Bus connection handling

- The primary connection string is always used for message operations and is also used for Azure entity discovery when its credential has Manage permission.
- Azure management capability is detected by entity enumeration. Only explicit authorization failures fall back to direct-entity mode; connectivity, timeout, credential-format, and service failures remain visible.
- The optional emulator management connection string is accepted only when both strings declare `UseDevelopmentEmulator=true`; emulator messaging endpoints are never probed for management.
- Direct-entity mode requires a queue, or a topic and subscription pair, only after management is known to be unavailable, allowing the connection page to submit Azure credentials without an entity name.
- The API field is `emulatorManagementConnectionString`. Runtime defaults use the `SERVICEBUSVIEWER_*` environment-variable names with no legacy aliases.

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
