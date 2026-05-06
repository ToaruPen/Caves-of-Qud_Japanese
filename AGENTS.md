# QudJP

## Why

QudJP is the Japanese localization mod for Caves of Qud `2.0.4`. The repo contains the shipped DLL, localization assets, and the tooling used to validate and deploy them.

## What

- Read the scoped guide for the area you are changing:
  - `Mods/QudJP/Assemblies/AGENTS.md` for C# patches, helpers, and tests
  - `Mods/QudJP/Localization/AGENTS.md` for XML and JSON localization assets
  - `scripts/AGENTS.md` for Python and shell tooling
- Source of truth:
  - behavior: `Mods/QudJP/Assemblies/QudJP.Tests/`
  - layer boundaries: `docs/test-architecture.md`
  - translation ownership and route decision rules: `docs/RULES.md`
  - PR and runtime procedures: `docs/workflows/pr-review.md`,
    `docs/workflows/runtime-evidence.md`
  - runtime evidence: fresh logs under `~/Library/Logs/Freehold Games/CavesOfQud/`
  - Steam Workshop release procedure: `docs/release.md`
    - Codex local workflow shortcut: `~/.codex/skills/ship-steam-workshop/SKILL.md`

## How

- If a stale note conflicts with tests or fresh runtime evidence, follow tests first.
- Use `docs/RULES.md` when deciding where a translation belongs. Use
  `docs/workflows/` when executing PR, runtime-log, sync, or deployment
  procedures.
- For decompiled C# exploration, use structural search with `ast-grep` before
  or alongside `rg` when call shape, argument structure, producer/sink routes,
  wrappers, assignments, or attributes matter. Prefer `just sg-cs
  'Popup.Show($$$ARGS)'` for common C# searches; plain `rg` is still fine for
  literal text, symbol names, and file discovery.
- For authoritative C# static inventories or scanner changes where type,
  receiver, overload, alias, inheritance, or extension-method identity affects
  correctness, use the repo-local Roslyn static analysis skill at
  `.codex/skills/roslyn-static-analysis/SKILL.md`. Treat `ast-grep` hits as
  candidates in that case, not proof.
- Prefer `just` recipes for routine validation so local runs match the repo task runner.
  Raw commands below document what the recipes execute.
- Core commands:

```bash
just build
just test-l1
just test-l2
just test-l2g
just python-check
just python-test
just localization-check
just translation-token-check
just deploy-mod
just sync-mod
```

- Decompiled game source lives in `~/dev/coq-decompiled_stable/` and must never be committed.
- Do not commit `Assembly-CSharp.dll` or other game binaries.
- Only the built DLL and localization assets ship; `.cs` source files do not.
