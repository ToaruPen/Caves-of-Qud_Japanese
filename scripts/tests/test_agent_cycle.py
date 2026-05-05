"""Tests for scripts/agent_cycle.sh."""

from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

import pytest

REPO_ROOT = Path(__file__).resolve().parents[2]
AGENT_CYCLE = REPO_ROOT / "scripts" / "agent_cycle.sh"


def _run_agent_cycle(*args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(  # noqa: S603 -- tests invoke the repo-local shell script via bash
        ["bash", str(AGENT_CYCLE), *args],  # noqa: S607
        capture_output=True,
        text=True,
        cwd=REPO_ROOT,
        check=False,
    )


@pytest.mark.skipif(not shutil.which("ast-grep"), reason="ast-grep CLI not available")
def test_ast_grep_smoke_finds_static_producer_fixture() -> None:
    """The agent-cycle ast-grep smoke must prove structural search actually works."""
    completed = _run_agent_cycle("ast-grep-smoke")

    assert completed.returncode == 0, completed.stderr
    assert "Demo/StaticProducerCases.cs:25:" in completed.stdout
    assert 'Popup.Show("Popup body", Title: "Ignored title")' in completed.stdout


@pytest.mark.skipif(not shutil.which("ast-grep"), reason="ast-grep CLI not available")
def test_ast_grep_check_reports_empty_rule_set_and_runs_smoke() -> None:
    """An empty rule directory should be explicit, not a silent 0-test pass."""
    rule_files = [
        *list((REPO_ROOT / "rules").rglob("*.yml")),
        *list((REPO_ROOT / "rules").rglob("*.yaml")),
        *list((REPO_ROOT / "rule-tests").rglob("*.yml")),
        *list((REPO_ROOT / "rule-tests").rglob("*.yaml")),
    ]
    if rule_files:
        pytest.skip("project ast-grep rules are registered")

    completed = _run_agent_cycle("ast-grep-check")

    assert completed.returncode == 0, completed.stderr
    assert "No project ast-grep rules registered" in completed.stdout
    assert "Demo/StaticProducerCases.cs:25:" in completed.stdout
