"""Contract tests for the repo-local Roslyn semantic probe."""
# ruff: noqa: S603 -- invokes repo-local tools with explicit arguments

from __future__ import annotations

import json
import os
import runpy
import subprocess
import sys
import time
from pathlib import Path
from typing import TYPE_CHECKING, TypedDict, cast

if TYPE_CHECKING:
    from collections.abc import Callable

import pytest

REPO_ROOT = Path(__file__).resolve().parents[2]
FIXTURE_SOURCE = Path(__file__).resolve().parent / "fixtures" / "roslyn_semantic_probe"
WRAPPER = REPO_ROOT / "scripts" / "roslyn_semantic_probe.py"
PROBE_DLL = (
    REPO_ROOT
    / "scripts"
    / "tools"
    / "RoslynSemanticProbe"
    / "bin"
    / "Release"
    / "net10.0"
    / "RoslynSemanticProbe.dll"
)
DEFAULT_MANAGED_DIR = (
    Path.home()
    / "Library"
    / "Application Support"
    / "Steam"
    / "steamapps"
    / "common"
    / "Caves of Qud"
    / "CoQ.app"
    / "Contents"
    / "Resources"
    / "Data"
    / "Managed"
)
DEFAULT_DECOMPILED_SOURCE = Path.home() / "dev" / "coq-decompiled_stable"
type CountMap = dict[str, int]


class QueryPayload(TypedDict):
    """Subset of the probe query payload asserted by tests."""

    external_reference_count: int


class MetricsPayload(TypedDict):
    """Subset of the probe metrics payload asserted by tests."""

    total_files: int
    resolved_matching_owner_hits: int
    candidate_matching_owner_hits: int
    unresolved_hits: int
    status_counts: CountMap
    owner_counts: CountMap
    first_string_argument_counts: CountMap
    string_risk_counts: CountMap


class HitPayload(TypedDict):
    """Subset of the probe hit payload asserted by tests."""

    expression: str
    roslyn_symbol_status: str
    owner_matches: bool
    method_or_property_symbol: str
    containing_type_symbol: str
    receiver_type_symbol: str
    string_arguments: list[StringArgumentPayload]


class StringArgumentPayload(TypedDict):
    """Subset of string argument payload asserted by tests."""

    has_qud_markup: bool
    has_tmp_markup: bool
    has_placeholder_like_text: bool


class ProbePayload(TypedDict):
    """Subset of the probe payload asserted by tests."""

    query: QueryPayload
    metrics: MetricsPayload
    hits: list[HitPayload]


def run_probe(*args: str, source_root: Path = FIXTURE_SOURCE) -> ProbePayload:
    """Run the semantic probe wrapper and return parsed JSON."""
    completed = subprocess.run(
        [
            sys.executable,
            str(WRAPPER),
            "--source-root",
            str(source_root),
            *args,
        ],
        cwd=REPO_ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
    )
    return cast("ProbePayload", json.loads(completed.stdout))


def load_payload_validator() -> Callable[[str], object]:
    """Load the wrapper payload validator without invoking the CLI."""
    namespace = runpy.run_path(str(WRAPPER), run_name="roslyn_semantic_probe_test")
    return cast("Callable[[str], object]", namespace["_load_payload"])


def test_owner_filter_excludes_same_name_false_positive() -> None:
    """Semantic owner filtering separates unrelated same-name callsites."""
    doc = run_probe(
        "--method",
        "Show",
        "--owner",
        "XRL.UI.Popup",
        "--include-nonmatching-owners",
        "--limit",
        "20",
    )

    assert doc["metrics"]["resolved_matching_owner_hits"] == 1
    assert doc["metrics"]["candidate_matching_owner_hits"] == 0
    assert doc["metrics"]["status_counts"] == {"resolved": 2, "unresolved": 1}
    assert doc["metrics"]["owner_counts"]["XRL.UI.Popup"] == 1
    assert doc["metrics"]["owner_counts"]["Other.Popup"] == 1
    matching_hits = [hit for hit in doc["hits"] if hit["owner_matches"]]
    assert len(matching_hits) == 1
    assert matching_hits[0]["containing_type_symbol"] == "XRL.UI.Popup"


def test_generic_and_inherited_owner_calls_are_grouped_semantically() -> None:
    """Inherited and generic owner calls are grouped by containing type symbol."""
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

    assert doc["metrics"]["resolved_matching_owner_hits"] == 3
    assert doc["metrics"]["candidate_matching_owner_hits"] == 0
    assert doc["metrics"]["owner_counts"]["XRL.Messages.MessageQueue"] == 2
    assert doc["metrics"]["owner_counts"]["XRL.World.IComponent<XRL.World.GameObject>"] == 1
    assert doc["metrics"]["owner_counts"]["Demo.Wrapper"] == 1
    assert doc["metrics"]["first_string_argument_counts"]["concatenation"] == 2
    assert doc["metrics"]["first_string_argument_counts"]["variable_or_member"] == 1


def test_candidate_status_is_not_reported_as_resolved_owner_hit() -> None:
    """Overload candidates stay separate from resolved owner evidence."""
    doc = run_probe("--method", "Maybe", "--owner", "XRL.UI.Popup", "--limit", "20")

    assert doc["metrics"]["resolved_matching_owner_hits"] == 0
    assert doc["metrics"]["candidate_matching_owner_hits"] == 1
    assert doc["metrics"]["status_counts"] == {"candidate": 1}
    assert doc["hits"][0]["roslyn_symbol_status"] == "candidate"


def test_unresolved_status_is_not_reported_as_resolved_owner_hit() -> None:
    """Unresolved rows remain visible and do not become target owner evidence."""
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
    assert doc["metrics"]["unresolved_hits"] == 1
    assert doc["metrics"]["resolved_matching_owner_hits"] == 1


def test_wrapper_calls_are_not_propagated_to_wrapped_owner() -> None:
    """Wrapper callsites are separate from the wrapped target call inside the wrapper body."""
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

    wrapper_hit = next(
        hit for hit in doc["hits"] if hit["expression"] == 'Wrapper.AddPlayerMessage("Wrapped static message.")'
    )
    wrapped_hit = next(hit for hit in doc["hits"] if hit["expression"] == "MessageQueue.AddPlayerMessage(message)")
    assert wrapper_hit["owner_matches"] is False
    assert wrapper_hit["containing_type_symbol"] == "Demo.Wrapper"
    assert wrapped_hit["owner_matches"] is True
    assert wrapped_hit["containing_type_symbol"] == "XRL.Messages.MessageQueue"


def test_string_argument_shapes_and_markup_risks_are_reported() -> None:
    """String shape and markup risk buckets support translation triage."""
    emit_doc = run_probe(
        "--method",
        "EmitMessage",
        "--owner",
        "XRL.World.IComponent<XRL.World.GameObject>",
        "--limit",
        "20",
    )
    set_text_doc = run_probe(
        "--method",
        "SetText",
        "--owner",
        "XRL.UI.UITextSkin",
        "--limit",
        "20",
    )

    assert emit_doc["metrics"]["resolved_matching_owner_hits"] == 12
    assert emit_doc["metrics"]["first_string_argument_counts"]["interpolated_string"] == 1
    assert emit_doc["metrics"]["first_string_argument_counts"]["string_literal"] == 7
    assert emit_doc["metrics"]["first_string_argument_counts"]["invocation"] == 2
    assert emit_doc["metrics"]["first_string_argument_counts"]["variable_or_member"] == 1
    assert emit_doc["metrics"]["first_string_argument_counts"]["element_access"] == 1
    assert emit_doc["metrics"]["string_risk_counts"]["qud_markup"] == 4
    assert emit_doc["metrics"]["string_risk_counts"]["tmp_markup"] == 1
    assert emit_doc["metrics"]["string_risk_counts"]["placeholder_like_text"] == 2
    ordinary_hit = next(hit for hit in emit_doc["hits"] if "ordinary & symbol" in hit["expression"])
    assert ordinary_hit["string_arguments"][0]["has_qud_markup"] is False
    assert ordinary_hit["string_arguments"][0]["has_placeholder_like_text"] is False
    escaped_brace_hit = next(hit for hit in emit_doc["hits"] if 'string.Format("{{0}}", name)' in hit["expression"])
    assert escaped_brace_hit["string_arguments"][0]["has_qud_markup"] is False
    assert escaped_brace_hit["string_arguments"][0]["has_placeholder_like_text"] is False
    assert set_text_doc["metrics"]["first_string_argument_counts"]["invocation"] == 1
    assert set_text_doc["hits"][0]["method_or_property_symbol"] == "bool XRL.UI.UITextSkin.SetText(string text)"


def test_fixture_smoke_runtime_catches_gross_speed_regression() -> None:
    """Fixture-scale smoke catches gross performance regressions."""
    start = time.perf_counter()
    doc = run_probe("--method", "Show", "--owner", "XRL.UI.Popup", "--limit", "0")
    elapsed = time.perf_counter() - start

    assert doc["metrics"]["total_files"] == 7
    assert elapsed < 10.0


def test_direct_text_assignments_are_grouped_by_semantic_owner() -> None:
    """Direct .text assignments are grouped by property owner symbol."""
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

    assert doc["metrics"]["resolved_matching_owner_hits"] == 3
    assert doc["metrics"]["candidate_matching_owner_hits"] == 0
    assert doc["metrics"]["owner_counts"]["TMPro.TMP_Text"] == 2
    assert doc["metrics"]["owner_counts"]["UnityEngine.UI.Text"] == 1
    assert doc["metrics"]["owner_counts"]["Demo.OtherText"] == 1
    assert doc["metrics"]["first_string_argument_counts"]["string_literal"] == 2
    assert doc["metrics"]["first_string_argument_counts"]["concatenation"] == 1
    assert doc["metrics"]["string_risk_counts"]["tmp_markup"] >= 1
    other_hit = next(hit for hit in doc["hits"] if hit["containing_type_symbol"] == "Demo.OtherText")
    derived_hit = next(hit for hit in doc["hits"] if "tmpDerivedText.text" in hit["expression"])
    assert other_hit["owner_matches"] is False
    assert derived_hit["containing_type_symbol"] == "TMPro.TMP_Text"
    assert derived_hit["receiver_type_symbol"] == "TMPro.TextMeshProUGUI"


def test_external_reference_option_preserves_existing_resolution() -> None:
    """Explicit references are accepted without degrading fixture resolution."""
    doc = run_probe(
        "--method",
        "Show",
        "--owner",
        "XRL.UI.Popup",
        "--reference",
        str(PROBE_DLL),
        "--limit",
        "20",
    )

    assert doc["query"]["external_reference_count"] == 1
    assert doc["metrics"]["resolved_matching_owner_hits"] == 1
    assert doc["metrics"]["status_counts"] == {"resolved": 2, "unresolved": 1}


def test_wrapper_output_option_writes_file_and_stdout(tmp_path: Path) -> None:
    """The Python wrapper keeps stdout JSON available when --output writes a file."""
    output = tmp_path / "semantic-probe.json"
    doc = run_probe(
        "--method",
        "Show",
        "--owner",
        "XRL.UI.Popup",
        "--output",
        str(output),
        "--limit",
        "20",
    )

    file_doc = cast("ProbePayload", json.loads(output.read_text(encoding="utf-8")))
    assert doc["metrics"]["resolved_matching_owner_hits"] == 1
    assert file_doc["metrics"]["resolved_matching_owner_hits"] == 1
    assert file_doc["metrics"]["status_counts"] == {"resolved": 2, "unresolved": 1}


def test_wrapper_payload_validation_requires_metric_contract_keys() -> None:
    """Wrapper contract validation rejects incomplete metrics before downstream use."""
    load_payload = load_payload_validator()
    status_counts: CountMap = {}
    owner_counts: CountMap = {}
    string_argument_counts: CountMap = {}
    string_risk_counts: CountMap = {}
    metrics: dict[str, object] = {
        "resolved_matching_owner_hits": 0,
        "candidate_matching_owner_hits": 0,
        "unresolved_hits": 0,
        "status_counts": status_counts,
        "owner_counts": owner_counts,
        "string_argument_counts": string_argument_counts,
        "string_risk_counts": string_risk_counts,
    }
    hits: list[object] = []
    payload: dict[str, object] = {
        "schema_version": "1",
        "query": {},
        "hits": hits,
        "metrics": metrics,
    }

    with pytest.raises(RuntimeError) as exc_info:
        _ = load_payload(json.dumps(payload))

    message = str(exc_info.value)
    assert "first_string_argument_counts" in message
    assert "total_files" in message


def test_wrapper_payload_validation_rejects_bad_json_shapes() -> None:
    """Wrapper contract validation normalizes malformed JSON shapes to RuntimeError."""
    load_payload = load_payload_validator()

    with pytest.raises(RuntimeError, match="payload must be a JSON object"):
        _ = load_payload("[]")

    with pytest.raises(RuntimeError, match="metrics must be an object"):
        _ = load_payload(json.dumps({"schema_version": "1", "query": {}, "hits": [], "metrics": []}))


def test_value_options_reject_following_flag_as_missing_value() -> None:
    """Value-taking CLI options reject another flag where the value should be."""
    completed = subprocess.run(
        [
            sys.executable,
            str(WRAPPER),
            "--source-root",
            str(FIXTURE_SOURCE),
            "--method",
            "Show",
            "--owner",
            "XRL.UI.Popup",
            "--output",
            "--include-nonmatching-owners",
        ],
        cwd=REPO_ROOT,
        check=False,
        text=True,
        capture_output=True,
    )

    assert completed.returncode == 1
    assert "missing value for --output" in completed.stderr


@pytest.mark.semantic_probe_real
@pytest.mark.skipif(
    os.environ.get("QUDJP_RUN_SEMANTIC_PROBE_REAL") != "1",
    reason="set QUDJP_RUN_SEMANTIC_PROBE_REAL=1 to run real-source semantic probe smoke",
)
@pytest.mark.skipif(not DEFAULT_DECOMPILED_SOURCE.is_dir(), reason="decompiled C# source is not available")
@pytest.mark.skipif(not DEFAULT_MANAGED_DIR.is_dir(), reason="Caves of Qud Unity managed directory is not available")
def test_real_source_unity_tmp_text_assignments_resolve() -> None:
    """Real-source smoke ensures Unity/TMP references resolve direct UI text assignments."""
    doc = run_probe(
        "--assignment-property",
        "text",
        "--owner",
        "TMPro.TMP_Text",
        "--owner",
        "TMPro.TMP_InputField",
        "--owner",
        "UnityEngine.UI.Text",
        "--owner",
        "UnityEngine.UI.InputField",
        "--managed-dir",
        str(DEFAULT_MANAGED_DIR),
        "--limit",
        "0",
        source_root=DEFAULT_DECOMPILED_SOURCE,
    )

    assert doc["metrics"]["total_files"] > 1000
    assert doc["metrics"]["resolved_matching_owner_hits"] > 100
    assert doc["metrics"]["unresolved_hits"] == 0
