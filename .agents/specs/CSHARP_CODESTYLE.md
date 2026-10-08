This document defines required C# code style rules.

# General
- Prefer the latest C# language version and .NET target framework by default unless the specs explicitly require specific versions.
- All types and members must declare an explicit accessibility modifier.
- Use C# 12 primary constructors where applicable, especially for small DTO or immutability-focused types.
- DTO and transport models should be `record` types with positional parameters.
- Methods returning `Task` or `Task<T>` must use the `Async` suffix.

# Naming Conventions
- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private const, fields and local variables (e.g. `private static int _defaultUserId = 1;`).
- Prefix interface names with "I" (e.g., `IUserService`).
- When a model has a business or persistent identity, represent that identity with a dedicated `<Model>Id` `readonly record struct`, even when the identifier contains only one scalar value.
- Do not use a primitive value or the model itself as a substitute for its named ID type.

## Data-Access Method Names

- Name a data-access method that checks only whether an entity exists `ExistsAsync` when the interface subject makes the entity unambiguous. Otherwise, use `Exists<Something>Async`. Return `bool`; the calling business logic owns the decision for a missing entity.
- Name a data-access method that searches for one entity and returns that entity or `null` `FindAsync` when the interface subject makes the entity unambiguous. Otherwise, use `Find<Something>Async`. The calling business logic owns the decision for an absent result.
- Name a data-access method that must return one entity and throws when it is absent `GetAsync` when the interface subject makes the entity unambiguous. Otherwise, use `Get<Something>Async`.
- Name a data-access write method by its exact persistence behavior: `CreateAsync` always adds a new entity, `UpdateAsync` updates an existing entity, and `UpsertAsync` may add or update an entity when the interface subject is unambiguous. Otherwise, add `<Something>` to distinguish the entity type.
- Do not use `LoadAsync` or `SaveAsync` for these entity-specific data-access operations because those names obscure absence and write semantics.
- `CreateAsync` parameters must not include the created entity's ID, `ETag`, `CreatedAtUtc`, or `ModifiedAtUtc`. Use individual field parameters when there are one to four; otherwise, define a dedicated create-input model. The data-access implementation assigns the ID, initial ETag, and timestamps and returns an `<Entity>VersionedId` containing the created `<Entity>Id` and ETag.
- Define `<Entity>Id` and `<Entity>VersionedId` value contracts as `readonly record struct` types.
- When a persisted model has an optimistic-concurrency ETag, define an `<Entity>VersionedId` that contains its `<Entity>Id` and `ETag`. When both types belong to the same folder, declare them together in `<Entity>Id.cs`.
- A business model that needs both an entity ID and its ETag must contain the corresponding `<Entity>VersionedId` instead of separate ID and ETag properties.

## Formatting
- Apply code-formatting style defined in `.editorconfig`.
- Use CRLF (`\r\n`) line endings for all C# files.
- Use file-scoped namespaces on the first non-empty line of each C# file.
- Prefer explicit file-local `using` statements. Avoid `global using` unless it has strong project-wide justification.
- Do not insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Do not use curly braces for `if`/`else` blocks if each branch contains a single line only.
- Both `if` / `else` must have curly braces or not.
- Prefer explicit types to `var`. Use `var` only when type is obvious from the code (e.g. like `var x = new MyObect();`)

# Constructors
- Prefer `var someVariable = new MyType(...);` over `MyType someVariable = new (...);` for local variable declarations.
- For classes that use C# primary constructors, capture constructor dependencies into explicit private readonly fields and use those fields in members.
- Do not reference primary constructor parameters directly throughout the class body after declaration; prefer the explicit-field pattern.

# Nullable Reference Types
- Nullable reference types stay enabled; use `?` for optional parameters and properties.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.\

# XML Docs
- Public methods and properties must have `<summary>` XML docs. Format XML comments on a single line unless the comment text exceeds 100 characters.

# Tests
- Do not write unit tests that only verify property assignment or retrieval for properties without any logic in the getter / setter, or simple conversion logic such as copying an `IEnumerable<T>` into an array, list, or read-only collection.
- Test methods must mark `Arrange`, `Act`, and `Assert` sections with comments such as `// Arrange`.
- The `Act` section must contain only the test action call. Keep setup out of `Act`, and perform assertions only in `Assert`.
- For all non-architectural tests, use `<ClassUnderTest>_<MethodUnderTest>_<TestGoal>_<ExpectedOutcome>` for test method names.
- Do not write unit tests that only verify assignment or retrieval of auto-properties when those properties contain no behavior.
- Do not write unit tests that only verify property assignment, property retrieval, or simple conversion logic such as copying an `IEnumerable<T>` into an array, list, or read-only collection.

# Enforcement
- Use Roslyn analyzers in CI.
- Configure analyzers and/or `.editorconfig` to require public API XML docs and treat nullable warnings as errors.
- Keep analyzer configuration in the repository and run `dotnet build` with analyzers enabled in CI.
- Document accepted deviations in `.agents/specs/PROJECT_DECISIONS.md` and link them from code comments where relevant.
