"""Validate that skill eval JSONL records are backed by this repo's manifest."""
# ruff: noqa: ANN401, EM101, EM102, TRY003, TRY004, T201

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
REQUIRED_FIELDS = (
    "run_id",
    "date",
    "skill",
    "scenario",
    "scenario_type",
    "model",
    "status",
    "requirements",
    "unclear_points",
    "discretionary_assumptions",
    "retries",
    "proposed_skill_changes",
)
STATUSES = {"pass", "partial", "fail"}
SCENARIO_TYPES = {"median", "edge", "holdout"}


def _load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise ValueError(f"missing required file: {path}") from exc
    except (OSError, UnicodeDecodeError) as exc:
        raise ValueError(f"failed to read {path}: {exc}") from exc
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid JSON in {path}: {exc}") from exc


def _load_manifest(path: Path) -> dict[str, Any]:
    manifest = _load_json(path)
    if not isinstance(manifest, dict):
        raise ValueError("skill eval manifest must be a JSON object")
    return manifest


def _manifest_scenario_key(skill_name: str, scenario: dict[str, Any]) -> tuple[str, str, str]:
    scenario_id = scenario.get("id")
    scenario_type = scenario.get("type")
    if not isinstance(scenario_id, str) or not scenario_id.strip():
        raise ValueError(f"skills.{skill_name}.scenarios[].id must be non-empty")
    if not isinstance(scenario_type, str) or not scenario_type.strip():
        raise ValueError(f"skills.{skill_name}.scenarios[].type must be non-empty")
    if scenario_type not in SCENARIO_TYPES:
        raise ValueError(f"skills.{skill_name}.scenarios[].type must be one of {sorted(SCENARIO_TYPES)}")
    return skill_name, scenario_id, scenario_type


def _manifest_scenarios(manifest: dict[str, Any]) -> dict[tuple[str, str], str]:
    skills = manifest.get("skills")
    if not isinstance(skills, dict):
        raise ValueError("skill eval manifest must contain a skills object")

    scenarios_by_key: dict[tuple[str, str], str] = {}
    for skill_name, skill_entry in skills.items():
        if not isinstance(skill_name, str) or not isinstance(skill_entry, dict):
            raise ValueError("skill eval manifest skills must map names to objects")
        scenarios = skill_entry.get("scenarios")
        if not isinstance(scenarios, list):
            raise ValueError(f"skills.{skill_name}.scenarios must be an array")
        for scenario in scenarios:
            if not isinstance(scenario, dict):
                raise ValueError(f"skills.{skill_name}.scenarios entry must be an object")
            _, scenario_id, scenario_type = _manifest_scenario_key(skill_name, scenario)
            key = (skill_name, scenario_id)
            if key in scenarios_by_key:
                raise ValueError(f"duplicate manifest scenario: {skill_name}/{scenario_id}")
            scenarios_by_key[key] = scenario_type
    return scenarios_by_key


def _iter_records(path: Path) -> list[tuple[int, dict[str, Any]]]:
    records: list[tuple[int, dict[str, Any]]] = []
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except FileNotFoundError as exc:
        raise ValueError(f"missing required file: {path}") from exc
    except (OSError, UnicodeDecodeError) as exc:
        raise ValueError(f"failed to read {path}: {exc}") from exc
    for line_number, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            record = json.loads(line)
        except json.JSONDecodeError as exc:
            raise ValueError(f"line {line_number}: invalid JSON: {exc}") from exc
        if not isinstance(record, dict):
            raise ValueError(f"line {line_number}: record must be an object")
        records.append((line_number, record))
    return records


def _require_record_string(record: dict[str, Any], key: str, line_number: int) -> str:
    value = record.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"line {line_number}: {key} must be a non-empty string")
    return value


def _require_string_array(record: dict[str, Any], key: str, line_number: int) -> None:
    value = record.get(key)
    if not isinstance(value, list) or not all(isinstance(item, str) and item.strip() for item in value):
        raise ValueError(f"line {line_number}: {key} must be a string array")


def _validate_requirement(item: Any, line_number: int) -> None:
    if not isinstance(item, dict):
        raise ValueError(f"line {line_number}: requirements entries must be objects")
    for key in ("name", "status", "evidence"):
        value = item.get(key)
        if not isinstance(value, str) or not value.strip():
            raise ValueError(f"line {line_number}: requirements[].{key} must be a non-empty string")
    if item["status"] not in STATUSES:
        raise ValueError(f"line {line_number}: requirements[].status must be one of {sorted(STATUSES)}")


def _validate_optional_number(record: dict[str, Any], key: str, line_number: int) -> None:
    value = record.get(key)
    if value is not None and (not isinstance(value, int | float) or isinstance(value, bool) or value < 0):
        raise ValueError(f"line {line_number}: {key} must be a non-negative number")


def _validate_optional_integer(record: dict[str, Any], key: str, line_number: int) -> None:
    value = record.get(key)
    if value is not None and (not isinstance(value, int) or isinstance(value, bool) or value < 0):
        raise ValueError(f"line {line_number}: {key} must be a non-negative integer")


def _validate_required_fields(record: dict[str, Any], line_number: int) -> None:
    for key in REQUIRED_FIELDS:
        if key not in record:
            raise ValueError(f"line {line_number}: missing required field: {key}")
    for key in ("run_id", "date", "skill", "scenario", "scenario_type", "model"):
        _require_record_string(record, key, line_number)


def _validate_status_fields(record: dict[str, Any], line_number: int) -> None:
    scenario_type = _require_record_string(record, "scenario_type", line_number)
    if scenario_type not in SCENARIO_TYPES:
        raise ValueError(f"line {line_number}: scenario_type must be one of {sorted(SCENARIO_TYPES)}")
    status = _require_record_string(record, "status", line_number)
    if status not in STATUSES:
        raise ValueError(f"line {line_number}: status must be one of {sorted(STATUSES)}")


def _validate_requirements(record: dict[str, Any], line_number: int) -> None:
    requirements = record["requirements"]
    if not isinstance(requirements, list) or not requirements:
        raise ValueError(f"line {line_number}: requirements must be a non-empty array")
    for item in requirements:
        _validate_requirement(item, line_number)


def _validate_single_result_schema(record: dict[str, Any], line_number: int) -> None:
    _validate_required_fields(record, line_number)
    _validate_status_fields(record, line_number)
    _validate_requirements(record, line_number)
    for key in ("unclear_points", "discretionary_assumptions", "proposed_skill_changes"):
        _require_string_array(record, key, line_number)
    retries = record["retries"]
    if not isinstance(retries, int) or isinstance(retries, bool) or retries < 0:
        raise ValueError(f"line {line_number}: retries must be a non-negative integer")
    executor = record.get("executor")
    if executor is not None and not isinstance(executor, str):
        raise ValueError(f"line {line_number}: executor must be a string")
    _validate_optional_number(record, "duration_seconds", line_number)
    _validate_optional_integer(record, "tool_count", line_number)


def _validate_result_schema(records: list[tuple[int, dict[str, Any]]]) -> None:
    for line_number, record in records:
        _validate_single_result_schema(record, line_number)


def _validate_manifest_backing(
    records: list[tuple[int, dict[str, Any]]],
    scenarios_by_key: dict[tuple[str, str], str],
) -> None:
    for line_number, record in records:
        skill = _require_record_string(record, "skill", line_number)
        scenario = _require_record_string(record, "scenario", line_number)
        scenario_type = _require_record_string(record, "scenario_type", line_number)

        expected_type = scenarios_by_key.get((skill, scenario))
        if expected_type is None:
            raise ValueError(f"line {line_number}: no manifest scenario for {skill}/{scenario}")
        if scenario_type != expected_type:
            raise ValueError(
                f"line {line_number}: scenario_type for {skill}/{scenario} is {scenario_type}, "
                f"expected {expected_type}"
            )


def _parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate skill eval JSONL records against skill-evals.json.")
    parser.add_argument(
        "results",
        nargs="?",
        default=REPO_ROOT / "skill-eval-results.jsonl",
        type=Path,
        help="skill eval JSONL result file",
    )
    parser.add_argument(
        "--manifest",
        default=REPO_ROOT / "skill-evals.json",
        type=Path,
        help="skill eval manifest path",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    """Validate that every result record points at a manifest scenario."""
    args = _parse_args(argv)
    try:
        manifest = _load_manifest(args.manifest.resolve())
        scenarios_by_key = _manifest_scenarios(manifest)
        records = _iter_records(args.results.resolve())
        _validate_result_schema(records)
        _validate_manifest_backing(records, scenarios_by_key)
    except (FileNotFoundError, ValueError) as exc:
        print(exc, file=sys.stderr)
        return 1

    print(f"Validated {len(records)} skill eval result record(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
