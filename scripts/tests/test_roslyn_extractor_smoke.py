"""Smoke test: AnnalsPatternExtractor csproj builds in Release."""
# ruff: noqa: S603,S607 -- tests invoke dotnet (PATH-resolved) to drive the repo-local tool

from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

import pytest

PROJECT_PATH = Path("scripts/tools/AnnalsPatternExtractor/AnnalsPatternExtractor.csproj")


@pytest.mark.skipif(not shutil.which("dotnet"), reason="dotnet SDK not available")
def test_extractor_csproj_builds_in_release() -> None:
    """The Roslyn extractor csproj must build cleanly so the CI step does not rot."""
    result = subprocess.run(
        ["dotnet", "build", str(PROJECT_PATH), "--configuration", "Release"],
        capture_output=True,
        text=True,
        check=False,
    )
    assert result.returncode == 0, (
        f"dotnet build failed (exit {result.returncode}).\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}"
    )
