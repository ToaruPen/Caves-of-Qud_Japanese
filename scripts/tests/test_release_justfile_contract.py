"""Static contract tests for release just recipes."""

from __future__ import annotations

import re
import shutil
import subprocess
from pathlib import Path

_REPO_ROOT = Path(__file__).resolve().parents[2]


def _justfile_text() -> str:
    return (_REPO_ROOT / "justfile").read_text(encoding="utf-8")


def _download_release_zip_recipe() -> str:
    justfile = _justfile_text()
    marker = "\ndownload-release-zip version:\n"
    _prefix, separator, remainder = justfile.partition(marker)
    assert separator, "download-release-zip version: recipe not found in justfile"
    next_recipe = re.search(r"^[A-Za-z0-9_-]+(?:\s+[^:\n]+)*:\n", remainder, flags=re.MULTILINE)
    return remainder[: next_recipe.start()] if next_recipe is not None else remainder


def test_download_release_zip_quotes_version_argument() -> None:
    """The release download recipe must not splice raw version text into shell syntax."""
    just = shutil.which("just")
    if just is None:
        recipe = _download_release_zip_recipe()

        assert "version={{quote(version)}}" in recipe
        assert 'tag="v{{version}}"' not in recipe
        assert "QudJP-v{{version}}" not in recipe
        return

    probe = '1.2.3"; touch /tmp/qudjp-just-injection #'
    result = subprocess.run(  # noqa: S603 - intentionally probes shell quoting via just dry-run.
        [just, "--dry-run", "download-release-zip", probe],
        cwd=_REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
    )

    dry_run = result.stdout + result.stderr

    assert re.search(r"^version=(['\"]).*touch /tmp/qudjp-just-injection.*\1$", dry_run, re.MULTILINE)
    assert not re.search(r"^(?!version=).*;\s*touch\s+/tmp/qudjp-just-injection", dry_run, re.MULTILINE)
