# Service Bus Viewer Version History

## 0.40.3 (2026-08-13)

- **Changed:** Reduced the combined Docker image size by using the .NET 10 Ubuntu Chiseled composite runtime with globalization support.
- **Fixed:** Aborted requests and disconnects now cancel in-flight Service Bus operations so they do not block the browser session.
- **Fixed:** A failed message peek while selecting an entity no longer changes the browser session's selected entity or messages.
- **Fixed:** Successful sends and receives are no longer reported as failures when the following message-list refresh fails.

## 0.40.0 (2026-08-12)

- **Breaking:** Renamed runtime configuration to `SERVICEBUSVIEWER_CONNECTION_STRING`, `SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING`, `SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME`, and `SERVICEBUSVIEWER_SUBSCRIPTION_NAME` with no legacy aliases.
- **Added:** An entity sidebar filter with debounced input, immediate application on Enter, and state preserved between viewer and entity details pages.
- **Changed:** Unified Azure connection handling so one Manage-capable connection string is used for entity discovery and message operations, with automatic fallback to direct entity access when management is unauthorized.
- **Changed:** Reserved the optional second connection string for emulator management and renamed it to **Emulator Management Connection String** throughout the API and UI.
- **Fixed:** Failed connection attempts during initial message peeking no longer leave the browser session marked as connected, allowing immediate retry with corrected credentials.
- **Fixed:** A queue, topic, or subscription supplied with a management-capable connection is retained as the initial viewer selection.
- **Fixed:** Long expanded message bodies increasing the width of the Peeked Messages table.

## 0.30.0 (2026-08-09)

- Replaced the Razor UI with a React + Vite SPA and reorganized the backend as a minimal API.
- Isolated viewer state and Service Bus connections by browser session.
- Added centralized RFC `ProblemDetails` API error handling.
- Consolidated production deployment into one ASP.NET Core container that serves both the SPA and API.

## 0.11.1 (2026-08-04)

- Fixed failed connection attempts leaving the app stuck in a connected state. You can now retry immediately with a corrected connection string.

## 0.11.0 (2026-08-01)

- Redesigned the application UI and user experience.
- Improved packaging and build configuration.

## 0.10.0 (2026-02-15)

- Added client-side JSON formatting and a message-body formatting toggle.
- Added subscription filter details.
- Fixed sending messages to topics.
- Included other minor fixes.

## 0.9.0 (2026-02-10)

- Added support for receiving messages from session-enabled entities with an optional Session ID.
- Added outgoing message properties: `MessageId`, `SessionId`, `CorrelationId`, `ScheduledEnqueueTime`, and `TimeToLive`.
- Added typed application properties.
- Added entity list caching.

## 0.8.0 (2026-02-05)

- Added queue/topic/subscription properties view.

## 0.7.0 (2026-02-01)

- Implemented client-side message expansion/collapse for better UX.

## 0.6.0 (2026-01-31)

- Added entity selection feature with visual icons for queues, topics, and subscriptions.

## 0.5.2 (2026-01-24)

- Added support for setting the connection string through an environment variable.

## 0.5.1 (2026-01-22)

- Updated to .NET 10.

## 0.5.0 (2025-05-26)

- Initial version.
