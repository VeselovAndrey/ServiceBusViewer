# Service Bus Viewer Version History

## 1.0.5 (2026-10-25)

- **Security:** The Vite development server now listens only on `localhost` by default, so other machines cannot reach the development SPA or `/api` proxy and retrieve configured Service Bus credentials through it. Local and Aspire development workflows are preserved.
- **Fixed:** Persistent per-message failures in Bulk Drop All and Republish All now stop the operation immediately and report the exact affected/failed counts, instead of retrying the dead-lettered messages indefinitely and inflating the progress. A republish step that could not complete is no longer retried automatically, so an already-delivered copy cannot be resent; cancellation still stops the operation promptly.
- **Fixed:** Single-message Drop and Republish now target the exact selected broker message by its Service Bus sequence number, so they work even when message IDs are null or duplicated. Only actually processed rows are removed from the dead-letter list and displayed totals change by the true affected count; unprocessed duplicate rows remain visible, and message IDs stay display metadata.
- **Fixed:** Single-message republish now rejects messages without a message ID before any API request, preventing a row action from becoming an unintended Republish All. Intentional bulk republish still requires confirmation, and message search handles null IDs safely.
- **Fixed:** Bulk Drop All and Republish All now lock entity selection and property navigation from confirmation through completion or cancellation, preventing later batches from affecting a different entity. Progress and cancellation remain available, and other entities are unchanged.

## 1.0.0 (2026-10-18)

- **Added:** Message count badges in the entity menu: queues and subscriptions show active, scheduled, and dead-letter counts (e.g. `5 | 0 | 2`); topics show the sums across their subscriptions. Counts load on connect and are omitted when unavailable or unreliable (e.g. the Service Bus emulator).
- **Added:** Hover/focus hint on each counter naming its category (e.g. "DLQ: 2"); clicking a counter selects the entity.
- **Added:** Entity counts refresh automatically after viewer message operations (send, receive, dead-letter drop/republish) and on demand via the refresh control in the entity menu.
- **Added:** Optional auto-refresh mode on the entity-menu refresh control with presets (1, 3, 5, 10, 15, 30, 60 seconds) or a custom interval saved in the browser. Auto mode is offered only when enabled in application configuration (`ServiceBusViewer:EntityCountAutoRefresh:Enabled`, disabled by default; enabled in the Docker image and the Aspire AppHost).

## 1.0.0-rc2 (2026-10-14)

- **Added:** Peek count selection on the Viewer page — presets (1, 5, 10, 30, 50, 100) or a custom value up to 500 — applied without reconnecting; the choice is saved in the browser.
- **Added:** A theme option that follows the OS light/dark setting live; the choice is saved in the browser.
- **Added:** Emulator connect hint on the Connect screen, with a button that fills in the emulator connection strings (separate Docker and Podman buttons when the viewer runs in a container).
- **Added:** Sortable message-list columns (Message ID, Session, Delivery Count, Enqueued Time / TTL, Content Type, and Dead Letter Reason in dead-letter mode) with direction indicators, and a search box over the loaded messages — in both peeked and dead-letter modes.
- **Added:** Confirmation prompt for Republish All, matching the Drop All confirmation.
- **Changed:** The **Dead Letter** toggle now shows its target — **To Dead Letter** / **To Active** — with a matching archive/inbox icon and a fixed button width.
- **Changed:** **Republish All** uses the same neutral button style as the other buttons on the page.
- **Changed:** Viewer header layout: Refresh, the mode toggle, search, and the receive controls moved into the message-list and Last Received Message headers; the mode toggle is now "To Dead Letter" / "To Active"; in dead-letter mode Republish All and Drop All sit on a separate line below a divider.
- **Fixed:** Dark-mode tints (dead-letter toggle, dead-letter header bar, button shadows) that were not rendering because the `brand-950` theme color was missing.
- **Fixed:** Page flicker while resizing the window near layout wrap points.

## 1.0.0-rc1 (2026-10-11)

- **Added:** Dead Letter toggle showing the selected entity's dead-letter queue with the dead-letter reason, source, and error description.
- **Added:** Republish dead-lettered messages to the original entity or drop them permanently — per message or in bulk (Republish All / Drop All with confirmation), with progress, cancellation, and affected/failed counts.
- **Added:** Message-id strategy dropdown for republishing: keep original (hidden with duplicate detection), auto-generate, GUID or GUID v7, or a manual id for a single message.
- **Added:** Total message count in the message list header when reliable counts are available.
- **Changed:** In dead letter mode, Refresh reloads the dead-letter list and selecting another entity exits the mode; single drop/republish animates the row out; dead-letter buttons stay disabled while an operation runs.
- **Changed:** Upgraded to Aspire 13.6.1, `Azure.Messaging.ServiceBus` 7.21.0, React 19.3, TypeScript 7, and Vite 8.3.
- **Fixed:** Dead-letter drop and republish now allow enough time for the initial receive on session-enabled entities.

## 0.48.0 (2026-08-23)

- **Added:** Per-message **Delivery Count** based on `SystemProperties.DeliveryCount` as reported by Azure Service Bus, shown as a column in the Peeked Messages list and in the Last Received Message details, so redelivered messages are visible without opening each one.
- **Added:** Optional **To**, **Reply To**, **Subject**, and **Partition Key** fields in the Send Message window, with the values sent on the outgoing message and displayed in received and expanded peeked message details.
- **Added:** **Advanced** toggle in the Send Message header that shows or hides the new fields; the choice is saved in a browser cookie and defaults to off.
- **Changed:** The delivery count is display-only: it does not affect receiving, completing, or dead-lettering messages and adds no badges, warnings, or color coding.
- **Changed:** The Peeked Messages table shows the partition key as a sub-value under the entity name for partitioned queues and topics.
- **Changed:** Renamed the **Application Properties** section in the send and receive views to **Custom Properties**.
- **Changed:** The Aspire AppHost and the `SolidCode.Aspire.Hosting.ServiceBusViewer` support library are now built against Aspire 13.5.2.
- **Changed:** The `SolidCode.Aspire.Hosting.ServiceBusViewer` package version is now aligned with the Aspire version it targets, and future releases will continue to carry the same version number as their target Aspire release.
- **Changed:** Paste From Clipboard now skips the **Diagnostic-Id** application property when loading a copied message into the send window.
- **Fixed:** The duplicate-detection warning for the Message ID field is now always visible with its icon and text on entities with duplicate detection enabled, instead of appearing only after a paste.
- **Fixed:** The "Duplicate detection enabled" indicator in the Send Message window no longer mixes information and warning semantics. It shows as a neutral info notice when duplicate detection is enabled, and the amber warning styling applies to the indicator and Message ID field only when the warning is armed with a non-empty Message ID.
- **Fixed:** The Payload editor in the Send Message window now stretches to match the adjacent properties column.
- **Fixed:** Sending a message with a Scheduled Enqueue Time or Time to Live that cannot be parsed now returns a validation error instead of silently dropping the property.
- **Fixed:** The Partition Key in the Send Message window is now limited to 128 characters with client-side and API validation, matching the Azure Service Bus limit.
- **Fixed:** A number of additional minor bugs, with small usability and display improvements.

## 0.45.0 (2026-08-20)

- **Added:** Full-message JSON copy button for received and peeked messages that copies the message body, system properties, and application properties as valid JSON, alongside the existing body-only copy action.
- **Added:** Paste-from-clipboard action in the Send Message window that loads a full-message JSON payload copied from a received or peeked message into the payload editor, message properties, and application properties, updating only the fields present in the JSON.
- **Added:** Warning indicator on the Message ID field when the selected queue or topic (the parent topic of a selected subscription) has duplicate detection enabled and the pasted JSON carries a non-empty message ID; the warning resets when the message ID is edited or after a successful send, and a failed send keeps it.
- **Added:** Copy button for received and peeked message bodies that copies the original message body text as plain text.
- **Added:** Received and peeked message details now display each application property's type alongside its name and value.

## 0.40.5 (2026-08-17)

- **Added:** Introduced the `SolidCode.Aspire.Hosting.ServiceBusViewer` hosting library for running the published Service Bus Viewer container from Aspire AppHosts.
- **Changed:** Reduced the combined Docker image size by using the .NET 10 Ubuntu Chiseled composite runtime with globalization support.
- **Fixed:** Sending a message no longer clears the last received message details.
- **Fixed:** Empty receives from queues and subscriptions now return after about one second instead of waiting for the Azure Service Bus default timeout, including session-enabled entities with no active messages.
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

