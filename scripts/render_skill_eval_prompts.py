"""Render fresh-executor prompts from this repository's skill eval manifest."""
# ruff: noqa: ANN401, EM101, EM102, TRY003, TRY004, T201

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]


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


def _iter_selected_scenarios(
    manifest: dict[str, Any],
    *,
    skill_filter: str | None,
    scenario_filter: str | None,
) -> list[tuple[str, dict[str, Any], dict[str, Any]]]:
    skills = manifest.get("skills")
    if not isinstance(skills, dict):
        raise ValueError("skill eval manifest must contain a skills object")

    selected: list[tuple[str, dict[str, Any], dict[str, Any]]] = []
    for skill_name, skill_entry in sorted(skills.items()):
        if skill_filter is not None and skill_name != skill_filter:
            continue
        if not isinstance(skill_entry, dict):
            raise ValueError(f"skills.{skill_name} must be an object")
        scenarios = skill_entry.get("scenarios")
        if not isinstance(scenarios, list):
            raise ValueError(f"skills.{skill_name}.scenarios must be an array")
        for scenario in scenarios:
            if not isinstance(scenario, dict):
                raise ValueError(f"skills.{skill_name}.scenarios entry must be an object")
            scenario_id = scenario.get("id")
            if scenario_filter is not None and scenario_id != scenario_filter:
                continue
            selected.append((skill_name, skill_entry, scenario))

    if not selected:
        raise ValueError("no matching skill eval scenarios")
    return selected


def _numbered_lines(items: list[str], *, critical: bool = False) -> str:
    lines = []
    for index, item in enumerate(items, start=1):
        prefix = "[critical] " if critical else ""
        lines.append(f"{index}. {prefix}{item}")
    return "\n".join(lines)


def _require_string_array(value: Any, path: str) -> list[str]:
    if not isinstance(value, list) or not all(isinstance(item, str) and item.strip() for item in value):
        raise ValueError(f"{path} must be a string array")
    return value


def _resolve_skill_file(
    skill_name: str,
    skill_entry: dict[str, Any],
    *,
    dotfiles_root: Path | None,
) -> tuple[Path, str]:
    skill_path_text = skill_entry.get("skill_path")
    if not isinstance(skill_path_text, str) or not skill_path_text.strip():
        raise ValueError(f"skills.{skill_name}.skill_path must be a non-empty string")

    skill_root = skill_entry.get("skill_root", "dotfiles")
    if skill_root == "repo":
        root = REPO_ROOT
    elif skill_root == "dotfiles":
        if dotfiles_root is None:
            raise ValueError(f"skills.{skill_name} uses dotfiles skill_root but DOTFILES_ROOT is not set")
        root = dotfiles_root
    else:
        raise ValueError(f"skills.{skill_name}.skill_root must be 'repo' or 'dotfiles'")

    skill_file = root / skill_path_text / "SKILL.md"
    if not skill_file.is_file():
        raise ValueError(f"missing skill file for {skill_name}: {skill_file}")

    try:
        display_path = str(skill_file.relative_to(REPO_ROOT if skill_root == "repo" else root))
    except ValueError:
        display_path = str(skill_file)
    return skill_file, display_path


def _render_prompt(
    skill_name: str,
    skill_entry: dict[str, Any],
    scenario: dict[str, Any],
    *,
    dotfiles_root: Path | None,
) -> str:
    skill_file, display_path = _resolve_skill_file(skill_name, skill_entry, dotfiles_root=dotfiles_root)

    scenario_id = scenario.get("id")
    scenario_type = scenario.get("type")
    prompt = scenario.get("prompt")
    for key, value in {
        "id": scenario_id,
        "type": scenario_type,
        "prompt": prompt,
    }.items():
        if not isinstance(value, str) or not value.strip():
            raise ValueError(f"skills.{skill_name}.scenarios[].{key} must be non-empty")

    critical = _require_string_array(scenario.get("critical"), f"skills.{skill_name}.scenarios[].critical")
    expected = _require_string_array(scenario.get("expected"), f"skills.{skill_name}.scenarios[].expected")
    forbidden = _require_string_array(scenario.get("forbidden"), f"skills.{skill_name}.scenarios[].forbidden")

    try:
        skill_text = skill_file.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError) as exc:
        raise ValueError(f"failed to read skill file for {skill_name}: {skill_file}: {exc}") from exc
    return (
        "You are a fresh executor reading the target skill with no hidden context.\n"
        "Use only the target skill text and the scenario below. "
        "Do not infer the author's intent from this repository unless the skill says to.\n\n"
        "## Target Skill\n\n"
        f"Path: `{display_path}`\n\n"
        "```markdown\n"
        f"{skill_text.rstrip()}\n"
        "```\n\n"
        "## Scenario\n\n"
        f"- Skill: `{skill_name}`\n"
        f"- Scenario: `{scenario_id}`\n"
        f"- Type: `{scenario_type}`\n\n"
        f"{prompt}\n\n"
        "## Requirements Checklist\n\n"
        f"{_numbered_lines(critical, critical=True)}\n"
        f"{_numbered_lines(expected)}\n\n"
        "## Forbidden Behaviors\n\n"
        f"{_numbered_lines(forbidden)}\n\n"
        "## Task\n\n"
        "1. Execute the scenario as if this skill had been selected for the user request.\n"
        "2. Produce the artifact or response the skill calls for.\n"
        "3. Report whether each requirement passed, partially passed, or failed.\n\n"
        "## Report Structure\n\n"
        "- Artifact:\n"
        "- Requirements: each checklist item as pass / partial / fail with evidence\n"
        "- Unclear Points: wording or decisions that were ambiguous\n"
        "- Discretionary Assumptions: choices you filled in yourself\n"
        "- Retries: repeated attempts or changed decisions\n"
        "- Tool Use: tools you would need or used, if any\n"
    )


def _parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Render fresh-executor prompts from skill eval scenarios.")
    parser.add_argument(
        "manifest",
        nargs="?",
        default=REPO_ROOT / "skill-evals.json",
        type=Path,
        help="skill eval manifest path",
    )
    parser.add_argument("--dotfiles-root", type=Path, help="dotfiles repository root for dotfiles-scoped skills")
    parser.add_argument("--skill", help="render only one skill")
    parser.add_argument("--scenario", help="render only one scenario id")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    """Render selected skill-eval prompts."""
    args = _parse_args(argv)
    manifest_path = args.manifest.resolve()
    dotfiles_root = args.dotfiles_root.resolve() if args.dotfiles_root is not None else None

    try:
        manifest = _load_manifest(manifest_path)
        selected = _iter_selected_scenarios(
            manifest,
            skill_filter=args.skill,
            scenario_filter=args.scenario,
        )
        prompts = [
            _render_prompt(skill_name, skill_entry, scenario, dotfiles_root=dotfiles_root)
            for skill_name, skill_entry, scenario in selected
        ]
    except ValueError as exc:
        print(exc, file=sys.stderr)
        return 1

    print("\n\n---\n\n".join(prompts))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
