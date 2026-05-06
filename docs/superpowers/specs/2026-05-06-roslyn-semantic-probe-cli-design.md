# Roslyn Semantic Probe CLI Design

## Goal

Add a narrow repo-local Roslyn semantic probe for decompiled C# exploration in
QudJP. The probe promotes `rg` and `ast-grep` candidates into symbol-backed
owner evidence when translation work depends on receiver type, overload,
inheritance, generic owner identity, or Unity/TMP UI text ownership.

The probe is not a replacement for `rg`, `ast-grep`, existing purpose-built
inventories, runtime evidence, or tracked generated artifacts.

## Adoption Requirements

The CLI is acceptable for QudJP only if it satisfies these requirements:

1. Same-name false positives are excluded by semantic owner.
   Example: `Other.Popup.Show(...)` and unrelated `.Show(...)` calls must not
   count as `XRL.UI.Popup`.
2. Semantic owner evidence is explicit.
   The output must include method/property symbol, containing type symbol,
   receiver type symbol, source location, original expression, and
   `roslyn_symbol_status`.
3. Uncertainty is consumer-visible.
   `candidate` and `unresolved` rows must never be reported as resolved. Metrics
   must split resolved target owner hits from candidate/unresolved target-looking
   rows.
4. String payload shape is classified for translation triage.
   The minimum required expression kinds are `string_literal`, `concatenation`,
   `interpolated_string`, `invocation`, `variable_or_member`, and
   `element_access`.
5. Markup and placeholder risk are surfaced.
   The scanner must flag Qud markup such as `{{W|...}}`, color shorthand such as
   `&G`, TMP color markup such as `<color=...>`, and placeholder-like text such
   as `=subject.T=`.
6. Unity/TMP references are supported without committing game binaries.
   A local managed directory option must resolve Unity UI and TextMeshPro
   metadata references while avoiding `Assembly-CSharp.dll` duplication against
   decompiled source.
7. Speed remains suitable for local exploration.
   Fixture tests catch gross regressions. Optional real-source smoke tests run
   only when `~/dev/coq-decompiled_stable` exists and should complete
   representative queries under the documented threshold.
8. Wrapper policy is explicit.
   Wrapper callsites are not propagated to wrapped owners in the first version.
   The probe reports the wrapper call as the wrapper owner and reports the
   wrapped call inside the wrapper body separately. A future wrapper propagation
   feature requires a separate design and tests.

## Tool Priority

Use the smallest tool that can answer the question:

1. `rg`
   Use for literal text, symbol names, file discovery, and fast nearby context.
2. `just sg-cs '<pattern>'`
   Use for structural C# shape: calls, assignments, arguments, wrappers, and
   attributes. Treat the result as candidate evidence when type identity matters.
3. `just semantic-probe ...`
   Use when the answer depends on semantic owner identity, overload choice,
   inherited/generic owner mapping, aliases, Unity/TMP property symbols, or when
   candidate rows need explicit uncertainty.
4. Purpose-built Roslyn inventories
   Use or extend these when the output becomes a source-of-truth artifact,
   requires domain-specific closure classification, or must be regenerated as
   tracked project evidence.
5. Runtime evidence
   Use fresh logs and runtime reproduction for route proof. Static analysis does
   not prove runtime visibility or route ownership by itself.

## Workflow Integration

### Translation Triage Flow

1. Start with `rg` for the visible phrase, method name, or nearby vocabulary.
2. Run `just sg-cs` when the relevant question has a recognizable syntax shape.
3. Run `just semantic-probe` when a candidate set contains common method names,
   wrapper-like callsites, inherited members, aliases, overloads, or direct
   Unity/TMP `.text` assignment.
4. Use the probe output to decide whether a string is:
   - a fixed leaf candidate,
   - an owner patch candidate,
   - a dynamic/procedural route,
   - a markup-sensitive route needing preservation tests,
   - or a candidate requiring runtime proof.
5. Promote recurring surfaces into a purpose-built scanner only after the probe
   shows stable value and the target surface needs durable counts or tracked
   artifacts.

### Expected Commands

The final command names should follow existing Justfile style:

```bash
just semantic-probe --method Show --owner XRL.UI.Popup --limit 20
just semantic-probe --method 'Show*' --owner XRL.UI.Popup --include-nonmatching-owners
just semantic-probe --assignment-property text --owner TMPro.TMP_Text --owner UnityEngine.UI.Text --managed-dir "$COQ_MANAGED_DIR"
just semantic-probe-check
just semantic-probe-real-smoke
```

`semantic-probe-real-smoke` should skip when the decompiled source is absent.
It must not require committed game binaries.

## Architecture

The implementation should mirror the repo's existing Roslyn tool pattern:

- C# tool under `scripts/tools/RoslynSemanticProbe/`
  Handles syntax traversal, Roslyn `Compilation`, `SemanticModel`, symbol
  resolution, string expression classification, reference resolution, timing,
  and JSON output.
- Python wrapper at `scripts/roslyn_semantic_probe.py`
  Handles repo-friendly CLI defaults, local path expansion, subprocess errors,
  JSON validation, and integration with pytest.
- Tests under `scripts/tests/`
  Fixture tests lock semantic behavior. Smoke tests build the C# project. A
  skipped real-source test checks representative runtime on machines with the
  decompiled source.
- Justfile recipes
  Add focused build/test/check/preview recipes and include the build/test/check
  in the existing `roslyn-*` gates.
- Documentation and skills
  Update root `AGENTS.md`, `scripts/AGENTS.md`, and
  `.codex/skills/roslyn-static-analysis/SKILL.md` so agents choose `rg`,
  `ast-grep`, the semantic probe, purpose-built inventories, and runtime
  evidence in the right order.

## Output Contract

The JSON output should include:

- `schema_version`
- `query`
  - method or assignment property
  - owner list
  - source root omitted or made relative-safe for checked artifacts
  - path filter
  - reference count and reference source summary
- `metrics`
  - total files, parsed files, candidate files
  - returned hits
  - resolved matching owner hits
  - candidate matching owner hits
  - unresolved hits
  - status counts
  - owner counts
  - string argument counts
  - first string argument counts
  - string risk counts
  - timing fields
- `hits`
  - file, line
  - syntax method or assignment property
  - expression
  - `roslyn_symbol_status`
  - owner match status
  - method/property symbol
  - containing type symbol
  - receiver type symbol
  - string arguments

Resolved target owner hits are authoritative static evidence. Candidate and
unresolved rows are review evidence only.

## Non-Goals

- Do not add rewrites.
- Do not create tracked generated inventories from this generic probe.
- Do not replace `StaticProducerInventoryScanner`,
  `TextConstructionInventory`, or `AnnalsPatternExtractor`.
- Do not treat static hits as runtime route proof.
- Do not propagate wrapper callsites to wrapped owners in the first version.
- Do not commit `Assembly-CSharp.dll`, Unity DLLs, or other game binaries.

## Empirical Tool-Routing Evaluation

After implementation, run a low-effort fresh-subagent routing check over
`AGENTS.md`, `scripts/AGENTS.md`, and
`.codex/skills/roslyn-static-analysis/SKILL.md`. The goal is to verify that a
cold agent can choose the right tool family without author-side interpretation.

Evaluation scenarios:

- Ad hoc `Popup.Show` owner-route investigation with unrelated same-name
  `Show` methods present.
- Localization XML placeholder and Japanese prose QA where the task is about
  assets, not C# owner discovery.
- Durable inventory generation for `EmitMessage`, `Popup.Show*`, and
  `AddPlayerMessage` as tracked project evidence.

Observed result on 2026-05-06: all three scenarios passed. The agents chose
`rg` / `ast-grep` before `just semantic-probe` for ad hoc C# owner evidence,
kept `candidate` / `unresolved` rows visible, declined Roslyn for localization
asset QA, and promoted tracked producer-callsite work to
`docs/static-producer-inventory.json` through the purpose-built inventory path.
The only prompt-tuning change needed was to make the initial lane choice and the
tracked artifact boundary explicit near the top of the Roslyn static-analysis
skill.

The `roslyn-static-analysis` scenarios are registered in `skill-evals.json`, and
the repo-local skill eval renderer supports both dotfiles-managed skills and
repo-local `.codex/skills/` paths. Raw JSONL results are acceptable only when
they correspond to manifest-backed scenarios.

## Acceptance Gates

Before implementation is considered complete:

```bash
just semantic-probe-check
just roslyn-check
just semantic-probe-real-smoke
```

The real smoke may skip if decompiled source is missing. On this development
machine it should run against `~/dev/coq-decompiled_stable`.

Before merge, the CLI itself must receive code review. After reviewer approval,
run the simplify workflow on the new/modified CLI files and re-run the focused
checks.
