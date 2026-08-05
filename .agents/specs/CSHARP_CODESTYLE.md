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

## Formatting
- Apply code-formatting style defined in `.editorconfig`.
- Use file-scoped namespaces on the first non-empty line of each C# file.
- Prefer explicit file-local `using` statements. Avoid `global using` unless it has strong project-wide justification.
- Do not insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Do not use curly braces for `if`/`else` blocks if it conststs from the single line only.
- Both `if` / `else` must have curly braces or not.
- Prefer explicit types to `var`. Use `var` only when type is obvious from the code (e.g. like `var x = new MyObect();`)

# Constructors
- Prefer `var = new MyType(...);` over `MyType var = new (...);` for local variable declarations.
- For classes that use C# primary constructors, capture constructor dependencies into explicit private readonly fields and use those fields in members.
- Do not reference primary constructor parameters directly throughout the class body after declaration; prefer the explicit-field pattern.

# Nullable Reference Types
- Nullable reference types stay enabled; use `?` for optional parameters and properties.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.\

# XML Docs
- Public methods and properties must have `<summary>` XML docs. Format XML comments on a single line unless the comment text exceeds 100 characters.

# Tests
- Test methods must mark `Arrange`, `Act`, and `Assert` sections with comments such as `// Arrange`.
- The `Act` section must contain only the test action call. Keep setup out of `Act`, and perform assertions only in `Assert`.
- For all non-architectural tests, use `<ClassUnderTest>_<MethodUnderTest>_<TestGoal>_<ExpectedOutcome>` for test method names.

# Registration and reflection
- Register services explicitly via extension methods or a composition root in the composing project.
- Reflection is forbidden by default for service registration and other runtime composition patterns.
- Reflection may be used only when that exact usage location is explicitly confirmed by the team, or explicitly documented in the relevant workitem for that exact place.
- Reflection approval is per location; approval in one place does not authorize any new place.

# Enforcement
- Use Roslyn analyzers in CI.
- Configure analyzers and/or `.editorconfig` to require public API XML docs and treat nullable warnings as errors.
- Keep analyzer configuration in the repository and run `dotnet build` with analyzers enabled in CI.
- Document accepted deviations in `.agents/specs/PROJECT_DECISIONS.md` and link them from code comments where relevant.
