"""Tests for repo-local skill eval prompt rendering."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
RENDERER = REPO_ROOT / "scripts" / "render_skill_eval_prompts.py"


def _write_skill(root: Path, relative_path: str, body: str) -> None:
    skill_dir = root / relative_path
    skill_dir.mkdir(parents=True, exist_ok=True)
    (skill_dir / "SKILL.md").write_text(body, encoding="utf-8")


def _write_manifest(path: Path, skills: dict[str, Any]) -> None:
    path.write_text(json.dumps({"$schema": "test", "skills": skills}), encoding="utf-8")


def _run_renderer(manifest: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(  # noqa: S603 -- invokes repo-local renderer with explicit args
        [sys.executable, str(RENDERER), str(manifest), *args],
        cwd=REPO_ROOT,
        check=False,
        text=True,
        capture_output=True,
    )


def test_renderer_supports_repo_and_dotfiles_skill_roots(tmp_path: Path) -> None:
    """Manifest scenarios can target both repo-local and dotfiles-managed skills."""
    dotfiles_root = tmp_path / "dotfiles"
    _write_skill(dotfiles_root, "home/.codex/skills/demo", "# Demo Skill\n\nUse demo.")

    manifest = tmp_path / "skill-evals.json"
    _write_manifest(
        manifest,
        {
            "demo-dotfiles": {
                "skill_path": "home/.codex/skills/demo",
                "scenarios": [
                    {
                        "id": "median-demo",
                        "type": "median",
                        "prompt": "Use the demo skill.",
                        "critical": ["Mention demo."],
                        "expected": ["Stay concise."],
                        "forbidden": ["Do not edit files."],
                    },
                ],
            },
            "roslyn-static-analysis": {
                "skill_root": "repo",
                "skill_path": ".codex/skills/roslyn-static-analysis",
                "scenarios": [
                    {
                        "id": "median-roslyn",
                        "type": "median",
                        "prompt": "Route a C# owner investigation.",
                        "critical": ["Choose the right tool lane."],
                        "expected": ["Mention semantic-probe."],
                        "forbidden": ["Do not edit files."],
                    },
                ],
            },
        },
    )

    completed = subprocess.run(  # noqa: S603 -- invokes repo-local renderer with explicit args
        [
            sys.executable,
            str(RENDERER),
            str(manifest),
            "--dotfiles-root",
            str(dotfiles_root),
        ],
        cwd=REPO_ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
    )

    assert "Path: `home/.codex/skills/demo/SKILL.md`" in completed.stdout
    assert "Path: `.codex/skills/roslyn-static-analysis/SKILL.md`" in completed.stdout
    assert "Scenario: `median-demo`" in completed.stdout
    assert "Scenario: `median-roslyn`" in completed.stdout


def test_renderer_can_render_repo_skill_without_dotfiles_root(tmp_path: Path) -> None:
    """Repo-local skill evals should not require DOTFILES_ROOT."""
    manifest = tmp_path / "skill-evals.json"
    _write_manifest(
        manifest,
        {
            "roslyn-static-analysis": {
                "skill_root": "repo",
                "skill_path": ".codex/skills/roslyn-static-analysis",
                "scenarios": [
                    {
                        "id": "median-roslyn",
                        "type": "median",
                        "prompt": "Route a C# owner investigation.",
                        "critical": ["Choose the right tool lane."],
                        "expected": ["Mention semantic-probe."],
                        "forbidden": ["Do not edit files."],
                    },
                ],
            },
        },
    )

    completed = subprocess.run(  # noqa: S603 -- invokes repo-local renderer with explicit args
        [
            sys.executable,
            str(RENDERER),
            str(manifest),
            "--skill",
            "roslyn-static-analysis",
        ],
        cwd=REPO_ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
    )

    assert "Path: `.codex/skills/roslyn-static-analysis/SKILL.md`" in completed.stdout
    assert "Scenario: `median-roslyn`" in completed.stdout


def test_renderer_rejects_non_object_manifest(tmp_path: Path) -> None:
    """Manifest root must be an object so schema errors stay user-readable."""
    manifest = tmp_path / "skill-evals.json"
    manifest.write_text("[]", encoding="utf-8")

    completed = _run_renderer(manifest)

    assert completed.returncode == 1
    assert "skill eval manifest must be a JSON object" in completed.stderr


def test_renderer_normalizes_manifest_read_errors(tmp_path: Path) -> None:
    """Manifest read and decode failures should return exit code 1, not a traceback."""
    manifest = tmp_path / "skill-evals.json"
    manifest.write_bytes(b"\xff")

    completed = _run_renderer(manifest)

    assert completed.returncode == 1
    assert "failed to read" in completed.stderr


def test_renderer_normalizes_skill_file_read_errors(tmp_path: Path) -> None:
    """Skill file read and decode failures should return exit code 1, not a traceback."""
    dotfiles_root = tmp_path / "dotfiles"
    skill_dir = dotfiles_root / "home/.codex/skills/demo"
    skill_dir.mkdir(parents=True)
    (skill_dir / "SKILL.md").write_bytes(b"\xff")

    manifest = tmp_path / "skill-evals.json"
    _write_manifest(
        manifest,
        {
            "demo-dotfiles": {
                "skill_path": "home/.codex/skills/demo",
                "scenarios": [
                    {
                        "id": "median-demo",
                        "type": "median",
                        "prompt": "Use the demo skill.",
                        "critical": ["Mention demo."],
                        "expected": ["Stay concise."],
                        "forbidden": ["Do not edit files."],
                    },
                ],
            },
        },
    )

    completed = _run_renderer(manifest, "--dotfiles-root", str(dotfiles_root))

    assert completed.returncode == 1
    assert "failed to read skill file for demo-dotfiles" in completed.stderr
