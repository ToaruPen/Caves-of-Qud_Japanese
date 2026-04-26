"""Golden tests for the AnnalsPatternExtractor C# tool."""
# ruff: noqa: S603,S607 -- tests invoke dotnet (PATH-resolved) to drive the repo-local tool

from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path

import pytest

PROJECT_PATH = Path("scripts/tools/AnnalsPatternExtractor/AnnalsPatternExtractor.csproj")
FIXTURES = Path("scripts/tests/fixtures/annals")


def _run_extractor(include: str, output: Path) -> subprocess.CompletedProcess[str]:
    """Invoke the AnnalsPatternExtractor dotnet tool against a single fixture file."""
    return subprocess.run(
        [
            "dotnet",
            "run",
            "--project",
            str(PROJECT_PATH),
            "--",
            "--source-root",
            str(FIXTURES),
            "--include",
            include,
            "--output",
            str(output),
        ],
        capture_output=True,
        text=True,
        check=False,
    )


@pytest.mark.skipif(not shutil.which("dotnet"), reason="dotnet SDK not available")
@pytest.mark.parametrize(
    "fixture",
    [
        "simple_concat",
        "string_format",
        "switch_cases",
        "unresolved_variable",
    ],
)
def test_extractor_matches_golden(fixture: str, tmp_path: Path) -> None:
    """Extractor output for each fixture must match the committed golden JSON exactly."""
    output = tmp_path / f"{fixture}.json"
    result = _run_extractor(f"{fixture}.cs", output)
    assert result.returncode == 0, (
        f"extractor failed (exit {result.returncode}). stdout:\n{result.stdout}\nstderr:\n{result.stderr}"
    )

    actual = json.loads(output.read_text(encoding="utf-8"))
    expected = json.loads((FIXTURES / f"expected_{fixture}.json").read_text(encoding="utf-8"))

    # Schema sanity (will catch if golden was regenerated against a broken extractor)
    assert actual["schema_version"] == "1"
    assert "candidates" in actual

    # Direct equality. If the extractor changes output shape, the golden must be regenerated.
    assert actual == expected, f"extractor output diverged from golden for {fixture}"
