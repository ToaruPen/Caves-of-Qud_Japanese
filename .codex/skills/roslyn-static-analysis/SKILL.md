---
name: roslyn-static-analysis
description: Use in QudJP when designing, reviewing, or extending C# static analysis over decompiled Caves of Qud source, especially scanner inventories, owner/sink route discovery, symbol-backed call classification, or decisions about when ast-grep results should be promoted to a Roslyn SemanticModel-based tool.
---

# Roslyn Static Analysis

Use this skill for QudJP static analysis work where syntax shape is not enough.
The main goal is to turn exploratory source findings into a durable, low-false-positive inventory.

## Tool Choice

Start light, then promote when needed:

1. Use `rg` for literal names, files, and nearby vocabulary.
2. Use `ast-grep` for call shape, argument shape, assignments, wrappers, and quick structural exploration. In this repo, prefer `just sg-cs '<pattern>'` for common C# structural searches; it defaults to `~/dev/coq-decompiled_stable`, and accepts an optional path as the second argument.
3. Promote to Roslyn when the result becomes a source-of-truth inventory or when correctness depends on type, receiver, overload, inheritance, alias, or extension-method identity.

Do not replace `ast-grep` with Roslyn for one-off exploration. Do not keep an `ast-grep` or regex scanner as the authoritative source when same-name methods on unrelated types would change the answer.

## Roslyn Scanner Contract

For repo-local scanner tools:

- Keep the Roslyn implementation in `scripts/tools/<ToolName>/`.
- Keep Python responsible for CLI compatibility, cache handling, JSON validation, and integration with existing script tests.
- Keep C# responsible for syntax traversal, `Compilation`, `SemanticModel`, symbol resolution, and source location extraction.
- Build with the SDK version already used by repo tools, currently `net10.0`.
- Add or update a smoke test that builds the `.csproj`, so CI catches tool rot.
- Keep build-time analysis separate from shipped runtime DLLs.

Use these Roslyn defaults unless the task proves otherwise:

- Parse with `LanguageVersion.Preview`.
- Allow unsafe source when scanning decompiled game code.
- Build a best-effort `CSharpCompilation` over all candidate source files.
- Reference trusted platform assemblies plus any repo-known game assemblies that are locally available.
- Include the reference set in output metadata or cache fingerprinting when it can change symbol resolution.
- Treat diagnostics as evidence, not an automatic failure. Decompiled source often has missing references or unsupported fragments.

## Semantic Classification

Prefer symbol-backed classification over name-only classification.

Before implementing a scanner, define the target surface explicitly:

- List the fully qualified owner types and method names that are in scope.
- Decide how inherited, generic, implicit-receiver, and extension-method calls should map to that surface.
- Record intentionally excluded same-name helpers or wrappers when they are known false positives.

For callsite inventories, emit enough metadata for later review:

- `roslyn_symbol_status`: `resolved`, `candidate`, `unresolved`, or a similarly explicit status.
- `method_symbol`: the resolved or selected candidate method.
- `containing_type_symbol`: the owning type of the method symbol.
- `receiver_type_symbol`: the receiver type when available.
- Source location fields that are stable under regenerated decompiled source.
- A reason or classification field for any fallback.

Classification policy:

- `resolved`: Roslyn selected one method symbol and the owning type matches the intended surface.
- `candidate`: Roslyn produced candidates but could not choose one; include the best candidate only when a deterministic policy exists.
- `unresolved`: no useful symbol evidence; keep the row visible and avoid silently treating it as true.

When filtering target surfaces, compare symbols or fully qualified containing type names. A method name match alone is not enough for source-of-truth output.

In this skill, authoritative does not automatically mean `candidate=0` and `unresolved=0`. It means every row has an explicit semantic status, fallback reason, and consumer-visible uncertainty. If a downstream consumer requires only resolved targets, set an explicit threshold for allowed `candidate` / `unresolved` counts and fail validation when the threshold is exceeded.

## ast-grep Boundary

Use `ast-grep` results as a candidate set, not a proof, when any of these are true:

- The method name is common or wrapper-like, such as `Show`, `EmitMessage`, or `AddPlayerMessage`.
- The same call shape can occur on unrelated helper types.
- The receiver may be implicit, inherited, generic, aliased, or an extension method.
- The output will drive patch ownership, translation family status, or test expectations.

Use `ast-grep` as the final tool only when the rule is purely structural and false positives are acceptable or independently reviewed.

## Cache And Regeneration

Scanner output is stale if either source or scanner code changes.

Include cache fingerprints for:

- Source root path and relevant source file metadata.
- Roslyn tool project files and source files.
- Python wrapper code that changes output shape.
- Reference-resolution inputs such as target surface configuration and assembly reference lists.
- Schema version or output contract version.

After changing classification logic, regenerate against the real decompiled source and report totals for callsites, families, text arguments, and symbol statuses. Any expected total change should be explained as a false-positive removal, newly covered surface, or classification policy change.

## Validation

Use a two-layer check:

1. Fixture tests: small C# samples that lock edge cases such as same-name false positives, overloads, named arguments, direct-marked strings, or collection arguments.
2. Real-source regeneration: run the scanner against `~/dev/coq-decompiled_stable/` and compare aggregate totals and representative changed rows.

Recommended local checks:

```bash
dotnet build scripts/tools/<ToolName>/<ToolName>.csproj --configuration Release --no-incremental
uv run pytest scripts/tests/test_<scanner>.py scripts/tests/test_roslyn_extractor_smoke.py -q
ruff check scripts/<wrapper>.py scripts/tests/test_<scanner>.py scripts/tests/test_roslyn_extractor_smoke.py
uvx basedpyright scripts/<wrapper>.py scripts/tests/test_<scanner>.py scripts/tests/test_roslyn_extractor_smoke.py
```

For QudJP static producer inventory work, also run the real-source regeneration command and inspect `roslyn_symbol_status` counts before claiming the scanner is authoritative.

Acceptance criteria for scanner changes should state:

- The intended target surface and known exclusions.
- The expected totals or allowed delta for callsites, families, text arguments, and symbol statuses.
- Whether `candidate` / `unresolved` rows are allowed, and why.
- Which fixture edge cases protect the classification policy.

When extending an existing scanner, preserve its established schema names unless the task is explicitly a schema migration. Add new fields or statuses only with wrapper validation, fixture expectations, docs, and regenerated inventory updated together.
