# AI Coding Agent Instructions

> 📌 This document is the primary agent-facing guide for repository-specific implementation rules. Keep it concise, accurate, and update it when repository conventions change.

## Project Context

ServiceBusViewer is split into three cooperating projects:

- `src\ServiceBusViewer` — ASP.NET Core minimal API backend
- `src\ServiceBusViewer.Web` — React + Vite SPA frontend
- `src\ServiceBusViewer.AppHost` — local Aspire/AppHost orchestration

Primary use case: local development and testing with the Azure Service Bus Emulator.

## Source-of-Truth Documents

- `docs\PROJECT_STRUCTURE.md` — repository layout, folder responsibilities, and dependency boundaries
- `README.md` — developer workflows, container usage, and product overview
- `.agents\specs\CSHARP_CODESTYLE.md` — C# coding conventions and style rules

Do not duplicate detailed structure or workflow guidance here when those files already cover it.

Follow `.agents\specs\CSHARP_CODESTYLE.md` for all C# style rules.

## Architecture Rules

See docs\PROJECT_STRUCTURE.md for the canonical architecture and folder responsibilities.

Enforced rules for agents:
- Minimal API handlers must remain thin: validate input, call `Business\Services`, and map results to HTTP responses.
- Preserve browser-session isolation: keep viewer and Service Bus connection state scoped to the `sbv-session` browser cookie.

## Service Bus Implementation Notes

- The code uses `Azure.Messaging.ServiceBus`.
- Prefer `await using` for receivers and senders to ensure deterministic disposal.
- Receivers should use `ServiceBusReceiveMode.PeekLock` unless a specific handler requires a different mode.
- Follow existing session-aware connection management patterns instead of introducing shared viewer state.

## Frontend Conventions

- Keep frontend code organized by responsibility under `src\ServiceBusViewer.Web\src` (`api`, `components`, `hooks`, `lib`, `pages`, `state`, `types`).
- Keep API-calling code in the frontend API layer instead of scattering fetch logic across components.
- Preserve the existing `/api` proxy/runtime contract when changing frontend or container configuration.

## Environment Notes

- `CONNECTION_STRING` overrides the default Service Bus connection string for the API.
- `ROOT_CONNECTION_STRING` is used for namespace/root operations when available.
- `ASPNETCORE_URLS` can be used to pin the backend port for local development and container scenarios.
- `VITE_PROXY_TARGET` should match the backend URL when running the SPA separately.

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
