@copilot please perform a full code review of this repository.

This is a **scheduled review** — automation, not human-triggered.

## Persona

Use the persona defined in `.github/agents/code-reviewer.agent.md`
(see also `.github/copilot-instructions.md` for project context).

Apply its checklist (Security / Architecture / Code Quality) and follow
its output format.

## Scope

- All `*.cs` files in the repository
- Skip `bin/`, `obj/`, generated code, test fixtures

## Output

Write the report to **`out/code-review.md`** with this exact structure:

- Header: `# Code Review — <YYYY-MM-DD>` (use today's UTC date)
- Summary line: `HIGH: N | MEDIUM: N | LOW: N`
- Findings grouped by severity, each with:
  - **File**: `path/to/file.cs:line`
  - **Issue**: one sentence
  - **Fix**: one sentence
- Final section "Top 3 actions"
- **Maximum 8 findings total** — keep the highest-impact ones

## Acceptance criteria

- File `out/code-review.md` exists in the resulting PR
- Each finding cites a real file path and line number (no fabricated lines)
- No leftover placeholders or `TODO` markers in the output
- Report fits on roughly one screen (≤ 60 lines)

When done, open a PR linked to this issue and apply label `auto-review`.
