# AI Coding Agent Instructions

> 📌 **This document is the primary memory store for all AI agents working on this project.** All conventions, patterns, decisions, and coding standards documented here should be followed consistently across all code changes. When making updates to the codebase, ensure that all relevant team conventions from this file are applied.

# Key folders and files

- `docs\` stores the main product and architecture documentation, including:
    - the [PRD](docs/PRD.md)
    - the [technical spec](docs/TECH_SPEC.md)
    - the [glossary](docs/GLOSSARY.md)
    - the [project structure](docs/PROJECT_STRUCTURE.md) - load this when selecting a location for a file or class
- `.agents\specs\` stores agent-oriented engineering guidance, including:
    - the [C# code style](.agents/specs/CSHARP_CODESTYLE.md) - MUST follow these rules for C# code
    - the [architecture rules](.agents/specs/ARCHITECTURE_RULES.md) - MUST follow these rules for module boundaries, dependencies, registration, and reflection
    - the [git branching rules](.agents\specs\GIT_BRANCHING_RULES.md) - MUST follow these rules for git branching
    - the [work-item conventions](.agents/specs/WORK_ITEM_CONVENTIONS.md) - MUST follow when planning, implementing, or updating work items; the owning skill defines exact workflow procedures
- `.workitems/` stores requirements and delivery work by lifecycle state; follow the [work-item folder layout](.agents/specs/WORK_ITEM_CONVENTIONS.md#folder-layout).
- `src\` stores the C# source code, including the `.slnx` solution file, projects, implementation code, and tests.
    - Host-project Dockerfiles belong inside their corresponding project folders under `src\`, not under `deployment\`.
- `deployment\` stores deployment and build scripts, including PowerShell scripts and DevOps pipelines.

# General rules

- Do not assume or guesss, always ask to clarify.
- Store agent-created temporary files in `.agents/.tmp/` inside the repository; create the folder when needed. Never use `.workitems` or `src` for temporary files.
- Never rewrite existing working tests without explicit confirmation.
- Never install any new tools without explicit confirmation.
- Ask only when a missing decision materially affects the current planning or implementation level. Preserve safe explicit assumptions when clarification is unnecessary.
- Git staging, commits (including amendments), and pushes each require an explicit developer request, including when performed through tools or delegated agents. Work items and implementation approval are not authorization. Requirements file staging is unrelated. Clarify ambiguous scope before acting.
- The [PRD](docs\PRD.md) and the [technical spec](docs\TECH_SPEC.md) are ToC files. When investigating load only sections that required for the current task or referenced by other section. Do not load the entire PRD or technical spec unless explicitly requested.


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
- `SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME` selects a queue for direct access, or the parent topic when a subscription is also configured.
- `SERVICEBUSVIEWER_SUBSCRIPTION_NAME` optionally selects a subscription under the configured parent topic; standalone topic access is not supported without management access.
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
