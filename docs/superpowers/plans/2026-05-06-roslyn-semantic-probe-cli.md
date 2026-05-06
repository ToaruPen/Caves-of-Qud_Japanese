# Roslyn Semantic Probe CLI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a narrow Roslyn semantic probe CLI to QudJP tooling and document where it sits between `rg`, `ast-grep`, purpose-built inventories, and runtime evidence.

**Architecture:** C# owns Roslyn parsing, compilation, semantic classification, and JSON output. A Python wrapper owns repo defaults, subprocess errors, JSON validation, and pytest integration. Justfile recipes and agent-facing docs expose the workflow without replacing existing scanners.

**Tech Stack:** .NET `net10.0`, Roslyn `Microsoft.CodeAnalysis.CSharp`, Python 3.12, pytest, Ruff, basedpyright, Justfile.

---

## File Structure

- Create: `scripts/tools/RoslynSemanticProbe/RoslynSemanticProbe.csproj`
- Create: `scripts/tools/RoslynSemanticProbe/Program.cs`
- Create: `scripts/tools/RoslynSemanticProbe/CliOptions.cs`
- Create: `scripts/tools/RoslynSemanticProbe/ReferenceResolver.cs`
- Create: `scripts/tools/RoslynSemanticProbe/SemanticProbe.cs`
- Create: `scripts/tools/RoslynSemanticProbe/StringExpressionClassifier.cs`
- Create: `scripts/tools/RoslynSemanticProbe/OutputModels.cs`
- Create: `scripts/roslyn_semantic_probe.py`
- Create: `scripts/tests/test_roslyn_semantic_probe.py`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/Demo/Cases.cs`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/Other/Popup.cs`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/TMPro/TMP_Text.cs`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/UnityEngine.UI/Text.cs`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/XRL.Messages/MessageQueue.cs`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/XRL.UI/Popup.cs`
- Create: `scripts/tests/fixtures/roslyn_semantic_probe/XRL.World/IComponent.cs`
- Modify: `scripts/tests/test_roslyn_extractor_smoke.py`
- Modify: `Justfile` and `justfile`
- Modify: `scripts/AGENTS.md`
- Modify: `AGENTS.md`
- Modify: `.codex/skills/roslyn-static-analysis/SKILL.md`

## Task 1: Add Fixture Contract Tests First

**Files:**
- Create: `scripts/tests/test_roslyn_semantic_probe.py`
- Create fixture files under `scripts/tests/fixtures/roslyn_semantic_probe/`

- [ ] **Step 1: Add fixture source files**

Create fixture types matching the PoC contract:

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/Demo/Cases.cs
using XRL.Messages;
using XRL.UI;
using XRL.World;
using TMPro;

namespace Demo;

public sealed class Cases : IComponent<GameObject>
{
    private readonly string name = "snapjaw";
    private readonly int count = 100;
    private readonly string memberText = "member text";
    private readonly string[] options = { "one", "two" };

    public void Calls(GameObject go, UITextSkin skin, TMP_Text tmpText, UnityEngine.UI.Text unityText, OtherText otherText)
    {
        Popup.Show("A fixed popup leaf.");
        Other.Popup.Show("A false positive popup.");
        MissingReceiver.Show("This call must stay unresolved.");
        Popup.Maybe(null);
        MessageQueue.AddPlayerMessage("You gain " + count + " XP.");
        AddPlayerMessage("Inherited message for " + name + ".");
        Wrapper.AddPlayerMessage("Wrapped static message.");
        EmitMessage($"Hello {name}");
        EmitMessage(MakeMessage());
        EmitMessage(memberText);
        EmitMessage(options[0]);
        IComponent<GameObject>.EmitMessage(go, "{{W|Marked}} =subject.T=");
        IComponent<GameObject>.EmitMessage(go, "&Ggreen");
        IComponent<GameObject>.EmitMessage(go, "<color=#44ff88>tmp</color>");
        skin.SetText(string.Format("{{{{W|{0}}}}}", name));
        tmpText.text = "<color=#44ff88>direct tmp</color>";
        unityText.text = "Direct " + name;
        otherText.text = "not a UI text assignment";
    }

    private string MakeMessage()
    {
        return "made message";
    }
}

public static class Wrapper
{
    public static void AddPlayerMessage(string message)
    {
        MessageQueue.AddPlayerMessage(message);
    }
}

public sealed class OtherText
{
    public string text { get; set; } = string.Empty;
}
```

Create these supporting fixtures:

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/Other/Popup.cs
namespace Other;

public static class Popup
{
    public static void Show(string message)
    {
    }
}
```

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/TMPro/TMP_Text.cs
namespace TMPro;

public class TMP_Text
{
    public string text { get; set; } = string.Empty;
}

public sealed class TextMeshProUGUI : TMP_Text
{
}
```

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/UnityEngine.UI/Text.cs
namespace UnityEngine.UI;

public class Text
{
    public string text { get; set; } = string.Empty;
}
```

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/XRL.Messages/MessageQueue.cs
namespace XRL.Messages;

public static class MessageQueue
{
    public static void AddPlayerMessage(string message, string? color = null, bool capitalize = true)
    {
    }
}
```

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/XRL.UI/Popup.cs
namespace XRL.UI;

public static class Popup
{
    public static void Show(string message)
    {
    }

    public static void Maybe(string message)
    {
    }

    public static void Maybe(string[] messages)
    {
    }
}

public sealed class UITextSkin
{
    public bool SetText(string text)
    {
        return true;
    }
}
```

```csharp
// scripts/tests/fixtures/roslyn_semantic_probe/XRL.World/IComponent.cs
namespace XRL.World;

public sealed class GameObject
{
}

public class IComponent<T>
{
    public void EmitMessage(string message)
    {
    }

    public static void EmitMessage(T source, string message)
    {
    }

    public void AddPlayerMessage(string message)
    {
    }
}
```

- [ ] **Step 2: Add failing pytest expectations**

Write `scripts/tests/test_roslyn_semantic_probe.py` with a shared runner and
these concrete assertions:

```python
def test_candidate_status_is_not_reported_as_resolved_owner_hit() -> None:
    doc = run_probe("--method", "Maybe", "--owner", "XRL.UI.Popup", "--limit", "20")
    assert doc["metrics"]["resolved_matching_owner_hits"] == 0
    assert doc["metrics"]["candidate_matching_owner_hits"] == 1
    assert doc["metrics"]["status_counts"] == {"candidate": 1}
    assert doc["hits"][0]["roslyn_symbol_status"] == "candidate"


def test_unresolved_status_is_not_reported_as_resolved_owner_hit() -> None:
    doc = run_probe(
        "--method",
        "Show",
        "--owner",
        "XRL.UI.Popup",
        "--include-nonmatching-owners",
        "--limit",
        "20",
    )
    unresolved_hit = next(hit for hit in doc["hits"] if "MissingReceiver.Show" in hit["expression"])
    assert unresolved_hit["roslyn_symbol_status"] == "unresolved"
    assert unresolved_hit["owner_matches"] is False
    assert doc["metrics"]["status_counts"]["unresolved"] == 1


def test_wrapper_calls_are_not_propagated_to_wrapped_owner() -> None:
    doc = run_probe(
        "--method",
        "AddPlayerMessage",
        "--owner",
        "XRL.Messages.MessageQueue",
        "--owner",
        "XRL.World.IComponent<XRL.World.GameObject>",
        "--include-nonmatching-owners",
        "--limit",
        "20",
    )
    wrapper_hit = next(hit for hit in doc["hits"] if hit["expression"] == 'Wrapper.AddPlayerMessage("Wrapped static message.")')
    wrapped_hit = next(hit for hit in doc["hits"] if hit["expression"] == "MessageQueue.AddPlayerMessage(message)")
    assert wrapper_hit["owner_matches"] is False
    assert wrapper_hit["containing_type_symbol"] == "Demo.Wrapper"
    assert wrapped_hit["owner_matches"] is True
    assert wrapped_hit["containing_type_symbol"] == "XRL.Messages.MessageQueue"


def test_direct_text_assignments_are_grouped_by_semantic_owner() -> None:
    doc = run_probe(
        "--assignment-property",
        "text",
        "--owner",
        "TMPro.TMP_Text",
        "--owner",
        "UnityEngine.UI.Text",
        "--include-nonmatching-owners",
        "--limit",
        "20",
    )
    assert doc["metrics"]["resolved_matching_owner_hits"] == 2
    assert doc["metrics"]["owner_counts"]["TMPro.TMP_Text"] == 1
    assert doc["metrics"]["owner_counts"]["UnityEngine.UI.Text"] == 1
    assert doc["metrics"]["owner_counts"]["Demo.OtherText"] == 1
    other_hit = next(hit for hit in doc["hits"] if hit["containing_type_symbol"] == "Demo.OtherText")
    assert other_hit["owner_matches"] is False
```

Also include the same-name, inherited/generic owner, string shape/risk,
runtime-smoke, and external-reference tests from the design requirements. Each
test must assert concrete metric values for `resolved_matching_owner_hits`,
`candidate_matching_owner_hits`, `unresolved_hits`, `status_counts`, and
owner/risk counts relevant to that behavior.

- [ ] **Step 3: Run the new tests and verify they fail**

Run:

```bash
uv run pytest scripts/tests/test_roslyn_semantic_probe.py -q
```

Expected: fail because the wrapper and C# project do not exist yet.

## Task 2: Implement the C# Semantic Probe

**Files:**
- Create all files under `scripts/tools/RoslynSemanticProbe/`

- [ ] **Step 1: Create the project**

Create `RoslynSemanticProbe.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>QudJP.Tools.RoslynSemanticProbe</RootNamespace>
    <AssemblyName>RoslynSemanticProbe</AssemblyName>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.3.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Implement CLI parsing**

Support:

```text
--method <name-or-prefix*>
--assignment-property <property>
--owner <fully.qualified.Type> repeated
--source-root <dir>
--path-filter <substring>
--limit <n>
--include-nonmatching-owners
--compile-candidates-only
--reference <dll> repeated
--managed-dir <dir>
--output <json-path>
```

Exactly one of `--method` or `--assignment-property` is required. At least one
`--owner` is required.

- [ ] **Step 3: Implement reference resolution**

Reference trusted platform assemblies plus explicit `--reference` paths. For
`--managed-dir`, include `Unity*.dll` only. Do not automatically include
`Assembly-CSharp.dll`.

- [ ] **Step 4: Implement semantic scanning**

Scan candidate files by text prefilter, parse all files in full mode, create a
best-effort `CSharpCompilation`, and inspect invocation or assignment syntax.

For each hit, emit `resolved`, `candidate`, or `unresolved`. Count:

```text
resolved_matching_owner_hits
candidate_matching_owner_hits
unresolved_hits
```

Do not let candidate rows inflate resolved owner totals.

- [ ] **Step 5: Implement string expression classification**

Classify at least:

```text
string_literal
concatenation
interpolated_string
invocation
variable_or_member
element_access
conditional
```

Flag Qud markup, TMP markup, and placeholder-like text with the same names used
in the design doc.

- [ ] **Step 6: Run focused C# build**

Run:

```bash
dotnet build scripts/tools/RoslynSemanticProbe/RoslynSemanticProbe.csproj --configuration Release --no-incremental
```

Expected: build succeeds with 0 errors.

## Task 3: Add the Python Wrapper

**Files:**
- Create: `scripts/roslyn_semantic_probe.py`

- [ ] **Step 1: Implement wrapper CLI**

The wrapper should pass through all probe arguments, default `--source-root` to
`~/dev/coq-decompiled_stable`, run `dotnet run --project`, and return parsed
JSON for Python callers.

- [ ] **Step 2: Validate JSON contract**

Reject missing `schema_version`, `query`, `metrics`, or `hits`. Validate the
metric keys used by tests:

```text
total_files
parsed_files
candidate_files
returned_hits
resolved_matching_owner_hits
candidate_matching_owner_hits
unresolved_hits
status_counts
owner_counts
string_argument_counts
first_string_argument_counts
string_risk_counts
timings_ms
```

- [ ] **Step 3: Run fixture tests**

Run:

```bash
uv run pytest scripts/tests/test_roslyn_semantic_probe.py -q
```

Expected: tests pass.

## Task 4: Integrate Justfile Gates

**Files:**
- Modify: `Justfile`
- Modify: `justfile`
- Modify: `scripts/tests/test_roslyn_extractor_smoke.py`

- [ ] **Step 1: Add build recipe**

Add:

```make
roslyn-build-semantic-probe:
  dotnet build scripts/tools/RoslynSemanticProbe/RoslynSemanticProbe.csproj --configuration Release --no-incremental
```

Include it in `roslyn-build`.

- [ ] **Step 2: Add probe recipes**

Add:

```make
semantic-probe *args:
  {{python}} scripts/roslyn_semantic_probe.py {{args}}

semantic-probe-check: roslyn-build-semantic-probe
  uv run pytest scripts/tests/test_roslyn_semantic_probe.py scripts/tests/test_roslyn_extractor_smoke.py -q
  ruff check scripts/roslyn_semantic_probe.py scripts/tests/test_roslyn_semantic_probe.py scripts/tests/test_roslyn_extractor_smoke.py
  uvx basedpyright scripts/roslyn_semantic_probe.py scripts/tests/test_roslyn_semantic_probe.py scripts/tests/test_roslyn_extractor_smoke.py

semantic-probe-real-smoke:
  uv run pytest scripts/tests/test_roslyn_semantic_probe.py -q -m semantic_probe_real
```

Include the focused pytest file in `roslyn-test` and Python static checks in
`roslyn-python-check`.

- [ ] **Step 3: Add optional real-source smoke**

Add a pytest test skipped unless `~/dev/coq-decompiled_stable` exists. The test
should run a representative `SetText` or `.text` query and assert the command
completes, emits JSON, and has no unresolved rows for the Unity/TMP assignment
case when the local managed directory is present.

Mark the test with `@pytest.mark.semantic_probe_real`, and register the marker
in `pyproject.toml` if pytest is configured to require known markers.

- [ ] **Step 4: Add build smoke**

Extend `scripts/tests/test_roslyn_extractor_smoke.py` with a Release build test
for `RoslynSemanticProbe.csproj`.

## Task 5: Update Agent-Facing Workflow Docs

**Files:**
- Modify: `AGENTS.md`
- Modify: `scripts/AGENTS.md`
- Modify: `.codex/skills/roslyn-static-analysis/SKILL.md`

- [ ] **Step 1: Update root tool-routing text**

Clarify:

```text
rg -> literal and file discovery
just sg-cs -> structural C# candidate search
just semantic-probe -> symbol-backed owner evidence for ad hoc exploration
purpose-built Roslyn inventories -> tracked or durable source-of-truth artifacts
runtime evidence -> live route proof
```

- [ ] **Step 2: Update scripts guide**

Add `just semantic-probe`, `just semantic-probe-check`, and
`just semantic-probe-real-smoke` to the scripts command list.

- [ ] **Step 3: Update Roslyn static analysis skill**

Add a "Semantic Probe Quick Reference" section with examples:

```bash
just semantic-probe --method Show --owner XRL.UI.Popup --limit 20
just semantic-probe --assignment-property text --owner TMPro.TMP_Text --owner UnityEngine.UI.Text --managed-dir "$COQ_MANAGED_DIR"
```

State that candidate/unresolved rows are review evidence, not resolved owner
proof.

## Task 6: Final Validation, Review, and Simplify Gate

**Files:**
- All new and modified files

- [ ] **Step 1: Run focused checks**

Run:

```bash
just semantic-probe-check
just roslyn-check
```

Expected: both pass.

- [ ] **Step 2: Run optional real-source smoke**

Run:

```bash
just semantic-probe-real-smoke
```

Expected: passes on machines with local decompiled source and Unity managed
references; skips otherwise.

- [ ] **Step 3: Request reviewer review**

Ask a reviewer to inspect the CLI implementation, wrapper, tests, Justfile
integration, and agent-facing docs. Required approval: no critical or high
findings, and no medium finding that affects semantic correctness.

- [ ] **Step 4: Run simplify after approval**

After reviewer approval, run the simplify workflow on:

```text
scripts/tools/RoslynSemanticProbe/
scripts/roslyn_semantic_probe.py
scripts/tests/test_roslyn_semantic_probe.py
```

Re-run:

```bash
just semantic-probe-check
just roslyn-check
```

Expected: both pass after simplification.

## Self-Review

- Spec coverage: The plan covers requirements, CLI architecture, tests, Justfile
  recipes, tool priority, skill updates, review, and simplify gates.
- Placeholder scan: No task contains `TBD` or implementation-free test
  instructions.
- Scope: The plan adds one generic probe and workflow docs. It does not modify
  tracked inventories or runtime translation behavior.
- Risk: The plan explicitly prevents candidate rows from counting as resolved
  owner evidence and keeps wrapper propagation out of v1.
