"""Static contract tests for release just recipes."""

from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

_REPO_ROOT = Path(__file__).resolve().parents[2]


def _justfile_text() -> str:
    return (_REPO_ROOT / "justfile").read_text(encoding="utf-8")


def _download_release_zip_recipe() -> str:
    justfile = _justfile_text()
    return justfile.split("\ndownload-release-zip version:\n", maxsplit=1)[1].split(
        "\n# Sync the built mod",
        maxsplit=1,
    )[0]


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

    assert 'tag="v1.2.3"; touch /tmp/qudjp-just-injection #' not in dry_run
    assert "version='1.2.3\"; touch /tmp/qudjp-just-injection #'" in dry_run
