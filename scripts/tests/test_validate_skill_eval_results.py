"""Tests for skill eval result manifest backing validation."""
# ruff: noqa: ANN401

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = REPO_ROOT / "scripts" / "validate_skill_eval_results.py"


def _write_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload), encoding="utf-8")


def _write_jsonl(path: Path, records: list[dict[str, Any]]) -> None:
    path.write_text("\n".join(json.dumps(record) for record in records), encoding="utf-8")


def _result_record(
    *,
    skill: str = "demo",
    scenario: str = "median-demo",
    scenario_type: str = "median",
) -> dict[str, Any]:
    return {
        "run_id": "run-1",
        "date": "2026-05-06",
        "skill": skill,
        "scenario": scenario,
        "scenario_type": scenario_type,
        "model": "test",
        "status": "pass",
        "requirements": [{"name": "requirement", "status": "pass", "evidence": "evidence"}],
        "unclear_points": [],
        "discretionary_assumptions": [],
        "retries": 0,
        "proposed_skill_changes": [],
    }


def _run_validator(results: Path, manifest: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(  # noqa: S603 -- invokes repo-local validator with explicit args
        [
            sys.executable,
            str(VALIDATOR),
            str(results),
            "--manifest",
            str(manifest),
        ],
        cwd=REPO_ROOT,
        check=False,
        text=True,
        capture_output=True,
    )


def test_validator_accepts_manifest_backed_results(tmp_path: Path) -> None:
    """A result row is valid when skill, scenario, and type match the manifest."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "median"}],
                },
            },
        },
    )
    _write_jsonl(results, [_result_record()])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 0, completed.stderr
    assert "Validated 1 skill eval result record(s)." in completed.stdout


def test_validator_rejects_results_without_manifest_scenario(tmp_path: Path) -> None:
    """A raw result cannot be summarized as empirical evidence without manifest backing."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(manifest, {"skills": {"demo": {"skill_path": ".codex/skills/demo", "scenarios": []}}})
    _write_jsonl(results, [_result_record()])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "no manifest scenario for demo/median-demo" in completed.stderr


def test_validator_rejects_non_object_manifest(tmp_path: Path) -> None:
    """Manifest root must be an object so schema errors stay user-readable."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    manifest.write_text("[]", encoding="utf-8")
    _write_jsonl(results, [_result_record()])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "skill eval manifest must be a JSON object" in completed.stderr


def test_validator_normalizes_manifest_read_errors(tmp_path: Path) -> None:
    """Manifest read and decode failures should return exit code 1, not a traceback."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    manifest.write_bytes(b"\xff")
    _write_jsonl(results, [_result_record()])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "failed to read" in completed.stderr


def test_validator_normalizes_result_read_errors(tmp_path: Path) -> None:
    """Result read and decode failures should return exit code 1, not a traceback."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "median"}],
                },
            },
        },
    )
    results.write_bytes(b"\xff")

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "failed to read" in completed.stderr


def test_validator_rejects_duplicate_manifest_scenarios(tmp_path: Path) -> None:
    """Duplicate scenario ids for one skill are ambiguous and should not be overwritten."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [
                        {"id": "median-demo", "type": "median"},
                        {"id": "median-demo", "type": "edge"},
                    ],
                },
            },
        },
    )
    _write_jsonl(results, [_result_record()])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "duplicate manifest scenario: demo/median-demo" in completed.stderr


def test_validator_rejects_schema_invalid_results(tmp_path: Path) -> None:
    """Manifest backing does not excuse missing result schema fields."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "median"}],
                },
            },
        },
    )
    _write_jsonl(results, [{"skill": "demo", "scenario": "median-demo", "scenario_type": "median"}])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "missing required field: run_id" in completed.stderr


def test_validator_rejects_scenario_type_mismatch(tmp_path: Path) -> None:
    """The JSONL scenario_type must match the manifest scenario type."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "edge"}],
                },
            },
        },
    )
    _write_jsonl(results, [_result_record()])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "scenario_type for demo/median-demo is median, expected edge" in completed.stderr


def test_validator_rejects_unknown_scenario_type(tmp_path: Path) -> None:
    """Scenario types must match the schema enum even when manifest and result agree."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "weird"}],
                },
            },
        },
    )
    _write_jsonl(results, [_result_record(scenario_type="weird")])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "scenarios[].type must be one of" in completed.stderr


def test_validator_rejects_negative_optional_counts(tmp_path: Path) -> None:
    """Optional numeric metrics still need to satisfy the result schema."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "median"}],
                },
            },
        },
    )
    record = _result_record()
    record["tool_count"] = -1
    _write_jsonl(results, [record])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "tool_count must be a non-negative integer" in completed.stderr


def test_validator_rejects_non_string_optional_executor(tmp_path: Path) -> None:
    """Optional executor metadata must still satisfy the result schema."""
    manifest = tmp_path / "skill-evals.json"
    results = tmp_path / "skill-eval-results.jsonl"
    _write_json(
        manifest,
        {
            "skills": {
                "demo": {
                    "skill_path": ".codex/skills/demo",
                    "scenarios": [{"id": "median-demo", "type": "median"}],
                },
            },
        },
    )
    record = _result_record()
    record["executor"] = 123
    _write_jsonl(results, [record])

    completed = _run_validator(results, manifest)

    assert completed.returncode == 1
    assert "executor must be a string" in completed.stderr
