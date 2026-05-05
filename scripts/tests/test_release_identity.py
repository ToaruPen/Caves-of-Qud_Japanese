"""Tests for release identity validation."""

from __future__ import annotations

import json
import shutil
import subprocess
import zipfile
from typing import TYPE_CHECKING

import pytest

from scripts.release_identity import ReleaseIdentityError, parse_release_tag, validate_release_identity

if TYPE_CHECKING:
    from pathlib import Path


def _write_release_project(root: Path, *, version: str = "1.2.3") -> None:
    """Create a minimal release project tree."""
    mod_dir = root / "Mods" / "QudJP"
    mod_dir.mkdir(parents=True)
    (mod_dir / "manifest.json").write_text(json.dumps({"Version": version}), encoding="utf-8")
    (root / "CHANGELOG.md").write_text(
        "# Changelog\n\n"
        "## [Unreleased]\n\n"
        "---\n\n"
        f"## [{version}] - 2026-05-05\n\n"
        "### Fixed\n\n"
        "- Fix release validation.\n",
        encoding="utf-8",
    )


def _write_release_zip(root: Path, *, version: str = "1.2.3", manifest_version: str | None = None) -> Path:
    """Create a minimal release ZIP with a manifest."""
    zip_path = root / "dist" / f"QudJP-v{version}.zip"
    zip_path.parent.mkdir(parents=True)
    with zipfile.ZipFile(zip_path, "w") as zf:
        zf.writestr("QudJP/manifest.json", json.dumps({"Version": manifest_version or version}))
    return zip_path


def _git(args: list[str], *, cwd: Path) -> None:
    git = shutil.which("git")
    assert git is not None
    subprocess.run([git, *args], cwd=cwd, check=True, capture_output=True, text=True)  # noqa: S603


def test_parse_release_tag_accepts_semver_tag() -> None:
    """Release tags use vX.Y.Z and expose the bare version."""
    assert parse_release_tag("v1.2.3") == "1.2.3"


def test_parse_release_tag_rejects_non_release_tag() -> None:
    """Non-semver tags do not trigger release identity validation."""
    with pytest.raises(ReleaseIdentityError, match=r"vX\.Y\.Z"):
        parse_release_tag("v1.2")


def test_validate_release_identity_rejects_manifest_mismatch(tmp_path: Path) -> None:
    """Manifest Version must match the release tag."""
    _write_release_project(tmp_path, version="1.2.2")

    with pytest.raises(ReleaseIdentityError, match=r"manifest\.json Version"):
        validate_release_identity(project_root=tmp_path, tag="v1.2.3")


def test_validate_release_identity_rejects_missing_changelog_entry(tmp_path: Path) -> None:
    """CHANGELOG must contain the released version entry."""
    _write_release_project(tmp_path, version="1.2.3")
    (tmp_path / "CHANGELOG.md").write_text("# Changelog\n\n## [Unreleased]\n", encoding="utf-8")

    with pytest.raises(ReleaseIdentityError, match=r"CHANGELOG\.md"):
        validate_release_identity(project_root=tmp_path, tag="v1.2.3")


def test_validate_release_identity_rejects_zip_manifest_mismatch(tmp_path: Path) -> None:
    """Release ZIP manifest Version must match the tag."""
    _write_release_project(tmp_path, version="1.2.3")
    zip_path = _write_release_zip(tmp_path, version="1.2.3", manifest_version="1.2.2")

    with pytest.raises(ReleaseIdentityError, match="Release ZIP manifest Version"):
        validate_release_identity(project_root=tmp_path, tag="v1.2.3", release_zip=zip_path)


def test_validate_release_identity_accepts_matching_zip(tmp_path: Path) -> None:
    """A matching tag, manifest, changelog, and ZIP pass validation."""
    _write_release_project(tmp_path, version="1.2.3")
    zip_path = _write_release_zip(tmp_path, version="1.2.3")

    validate_release_identity(project_root=tmp_path, tag="v1.2.3", release_zip=zip_path, check_git=False)


def test_validate_release_identity_rejects_tag_not_on_main(tmp_path: Path) -> None:
    """Release tags must be reachable from the configured main ref."""
    _write_release_project(tmp_path, version="1.2.3")
    _git(["init", "-b", "main"], cwd=tmp_path)
    _git(["config", "user.email", "test@example.invalid"], cwd=tmp_path)
    _git(["config", "user.name", "Release Test"], cwd=tmp_path)
    _git(["add", "."], cwd=tmp_path)
    _git(["commit", "-m", "base"], cwd=tmp_path)
    _git(["checkout", "-b", "release-branch"], cwd=tmp_path)
    (tmp_path / "extra.txt").write_text("branch only\n", encoding="utf-8")
    _git(["add", "extra.txt"], cwd=tmp_path)
    _git(["commit", "-m", "branch only"], cwd=tmp_path)
    _git(["tag", "-a", "v1.2.3", "-m", "release"], cwd=tmp_path)

    with pytest.raises(ReleaseIdentityError, match="not reachable"):
        validate_release_identity(project_root=tmp_path, tag="v1.2.3", main_ref="main")
