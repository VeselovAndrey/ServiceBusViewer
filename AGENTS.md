# AI Coding Agent Instructions

> 📌 This document is the primary agent-facing guide for repository-specific implementation rules. Keep it concise, accurate, and update it when repository conventions change.

## Project Context

ServiceBusViewer is split into three cooperating projects:

- `src\ServiceBusViewer` — ASP.NET Core minimal API backend
- `src\ServiceBusViewer.Web` — React + Vite SPA frontend
- `src\ServiceBusViewer.AppHost` — local Aspire/AppHost orchestration

Primary use case: local development and testing with the Azure Service Bus Emulator.

## Source-of-Truth Documents

- `.agents\specs\CSHARP_CODESTYLE.md` — C# coding conventions and style rules.
- `CHANGELOG.md` — complete product version history
- `docs\PROJECT_STRUCTURE.md` — repository layout, folder responsibilities, and dependency boundaries
- `README.md` — developer workflows, container usage, product overview, and recent release summary

Do not duplicate detailed structure or workflow guidance here when those files already cover it.

Follow `.agents\specs\CSHARP_CODESTYLE.md` for C# code. If a rule conflicts with `.editorconfig`, `.editorconfig` takes precedence.

## Architecture Rules

See docs\PROJECT_STRUCTURE.md for the canonical architecture and folder responsibilities.

Enforced rules for agents:
- Minimal API handlers must remain thin: validate input, call the appropriate business capability service, and map results to HTTP responses.
- Preserve browser-session isolation: keep viewer and Service Bus connection state scoped to the `sbv-session` browser cookie.

## Service Bus Implementation Notes

- The code uses `Azure.Messaging.ServiceBus`.
- Prefer `await using` for receivers and senders to ensure deterministic disposal.
- Receivers should use `ServiceBusReceiveMode.PeekLock` unless a specific handler requires a different mode.
- Follow existing session-aware connection management patterns instead of introducing shared viewer state.

## Frontend Conventions

- Keep frontend code organized by responsibility under `src\ServiceBusViewer.Web\src` (`api`, `assets`, `components`, `hooks`, `lib`, `pages`, `state`, `types`).
- Keep API-calling code in the frontend API layer instead of scattering fetch logic across components.
- Preserve the existing `/api` proxy/runtime contract when changing frontend or container configuration.

## Environment Notes

- `SERVICEBUSVIEWER_CONNECTION_STRING` overrides the default primary Service Bus connection string for the API.
- `SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING` optionally supplies the emulator-only administration endpoint.
- `SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME` selects the queue or topic used for direct entity access.
- `SERVICEBUSVIEWER_SUBSCRIPTION_NAME` selects the subscription when direct entity access targets a topic.
- `ASPNETCORE_URLS` can be used to pin the backend port for local development and container scenarios.
- `VITE_PROXY_TARGET` should match the backend URL when running the SPA separately.
- `VITE_API_BASE_PATH` sets the API base path embedded in the SPA during Vite development or build and defaults to `/api`.

## Testing and Validation

- There are currently no automated tests in the repository.
- Prefer targeted manual validation aligned with the changed behavior.
- Keep the combined-image Dockerfile and AppHost emulator/configuration assets aligned with runtime changes.

## Agent Operational Guidelines

- Record repository-specific decisions or exceptions in `.agents/specs/PROJECT_DECISIONS.md`
- Do not commit changes if it was not explicitly requested by developer. If you are unsure, ask for clarification.
- Make surgical, minimal changes that fully address the user's request. Avoid broad refactors unless requested.
- When changing runtime or API contracts, do it only with explicit confirmation and update `docs\PROJECT_STRUCTURE.md` and `README.md` accordingly.
- Validate changes with the smallest-targeted tests or manual checks appropriate to the change; do not add global test suites.
- If any ambiguity exists, ask a clarifying questions using tool before making edits. Prefer multiple-choice options when possible.
- Prefer repo-provided tools and scripts (e.g., `npm run web:install`, `dotnet run --project ...`) for validation rather than installing new global tools.
- Never rewrite existing working tests without explicit confirmation.
- Never install any new tools without explicit confirmation.
- Never commit or push any changes without explicit confirmation.
- Use language servers (LSP) for code navigation and refactoring when available for higher confidence edits.
- Keep exactly the three most recent releases in the README version history and link it to `CHANGELOG.md`. Keep the complete version history, including those three releases, in `CHANGELOG.md`.
