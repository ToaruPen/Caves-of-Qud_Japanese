# ast-grep Rules

Project-specific structural rules live here once a repeated QudJP workflow or
source pattern can be checked deterministically.

Follow the `ast-grep-practice` skill workflow:

1. Add a failing case under `rule-tests/`.
2. Add the smallest matching rule under this directory.
3. Run `just ast-grep-check`.

When no project rules are registered yet, `just ast-grep-check` reports that
state explicitly and runs `just ast-grep-smoke` so the ast-grep CLI path is
still exercised.
