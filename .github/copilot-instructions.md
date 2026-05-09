# Repository Instructions — Sample App (Demo)

## Project Context
- .NET 9 console app
- Single project, in-memory data
- Used as a demo target for code-review and doc-writer agents

## Coding Standards (target — many violations are intentional in the demo)
- Use `ILogger<T>` for logging — never `Console.WriteLine`
- Use parameterised queries — never string-interpolate SQL
- Catch specific exceptions — never swallow with empty catch
- Avoid nested if/else > 2 levels — prefer guard clauses
- Avoid magic strings for status / type discriminators — use enums

## Review Output

When an agent in `.github/agents/` produces a report:
- Output is short and actionable — never more than 10 findings
- Each finding cites file and line
- Severity grouped (HIGH / MEDIUM / LOW)
- Saved under `out/` with the filename declared in the agent
