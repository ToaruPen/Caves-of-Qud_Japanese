"""Validate that release tags, metadata, and artifacts identify one version."""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path
from typing import NoReturn, Self

try:
    from scripts.build_release import read_version
except ModuleNotFoundError:  # pragma: no cover - exercised by direct script execution
    from build_release import read_version

_RELEASE_TAG_RE = re.compile(r"^v(?P<version>\d+\.\d+\.\d+)$")


class ReleaseIdentityError(ValueError):
    """Raised when release identity checks fail."""


def parse_release_tag(tag: str) -> str:
    """Return the bare X.Y.Z version from a vX.Y.Z release tag."""
    match = _RELEASE_TAG_RE.fullmatch(tag.strip())
    if match is None:
        msg = f"Release tag must use vX.Y.Z format: {tag!r}"
        raise ReleaseIdentityError(msg)
    return match.group("version")


def _changelog_has_entry(changelog_path: Path, version: str) -> bool:
    """Return whether CHANGELOG.md contains a release entry for version."""
    heading = re.compile(rf"^## \[{re.escape(version)}\](?:\s+-\s+.+)?$", re.MULTILINE)
    return heading.search(changelog_path.read_text(encoding="utf-8")) is not None


def _zip_manifest_version(zip_path: Path) -> str:
    """Read QudJP/manifest.json Version from a release ZIP."""
    with zipfile.ZipFile(zip_path) as zf:
        try:
            raw_manifest = zf.read("QudJP/manifest.json")
        except KeyError as exc:
            msg = f"Release ZIP is missing QudJP/manifest.json: {zip_path}"
            raise ReleaseIdentityError(msg) from exc
    data = json.loads(raw_manifest.decode("utf-8"))
    version = str(data.get("Version", "")).strip()
    if not version:
        msg = f"Release ZIP manifest has no Version: {zip_path}"
        raise ReleaseIdentityError(msg)
    return version


def _git_executable() -> str:
    """Resolve git or raise the release-domain error used by this CLI."""
    git = shutil.which("git")
    if git is None:
        msg = "git executable not found"
        raise ReleaseIdentityError(msg)
    return git


def _git_output(args: list[str], *, cwd: Path) -> str:
    """Run git and return stripped stdout, converting failures to release errors."""
    git = _git_executable()
    try:
        result = subprocess.run(  # noqa: S603
            [git, *args],
            cwd=cwd,
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as exc:
        detail = (exc.stderr or exc.stdout or str(exc)).strip()
        msg = f"git {' '.join(args)} failed: {detail}"
        raise ReleaseIdentityError(msg) from exc
    return result.stdout.strip()


def _validate_git_identity(project_root: Path, *, tag: str, main_ref: str | None) -> None:
    """Validate tag target and optional main ancestry."""
    tag_sha = _git_output(["rev-list", "-n1", tag], cwd=project_root)
    head_sha = _git_output(["rev-parse", "HEAD"], cwd=project_root)
    if tag_sha != head_sha:
        msg = f"{tag} points at {tag_sha}, but HEAD is {head_sha}"
        raise ReleaseIdentityError(msg)

    if main_ref is None:
        return
    git = _git_executable()
    try:
        subprocess.run(  # noqa: S603
            [git, "merge-base", "--is-ancestor", tag_sha, main_ref],
            cwd=project_root,
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as exc:
        detail = (exc.stderr or exc.stdout or "").strip()
        suffix = f": {detail}" if detail else ""
        msg = f"{tag} target {tag_sha} is not reachable from {main_ref}{suffix}"
        raise ReleaseIdentityError(msg) from exc


def validate_release_identity(
    *,
    project_root: Path,
    tag: str,
    release_zip: Path | None = None,
    main_ref: str | None = None,
    check_git: bool = True,
) -> str:
    """Validate that a release tag, manifest, changelog, and optional ZIP match."""
    project_root = project_root.resolve()
    version = parse_release_tag(tag)

    manifest_path = project_root / "Mods" / "QudJP" / "manifest.json"
    manifest_version = read_version(manifest_path)
    if manifest_version != version:
        msg = f"manifest.json Version {manifest_version!r} does not match release tag {tag!r}"
        raise ReleaseIdentityError(msg)

    changelog_path = project_root / "CHANGELOG.md"
    if not changelog_path.is_file() or not _changelog_has_entry(changelog_path, version):
        msg = f"CHANGELOG.md has no release entry for {version}"
        raise ReleaseIdentityError(msg)

    if release_zip is not None:
        resolved_zip = release_zip if release_zip.is_absolute() else project_root / release_zip
        expected_name = f"QudJP-v{version}.zip"
        if resolved_zip.name != expected_name:
            msg = f"Release ZIP filename must be {expected_name}: {resolved_zip}"
            raise ReleaseIdentityError(msg)
        zip_version = _zip_manifest_version(resolved_zip)
        if zip_version != version:
            msg = f"Release ZIP manifest Version {zip_version!r} does not match release tag {tag!r}"
            raise ReleaseIdentityError(msg)

    if check_git:
        _validate_git_identity(project_root, tag=tag, main_ref=main_ref)
    return version


class _Parser(argparse.ArgumentParser):
    def error(self: Self, message: str) -> NoReturn:
        """Normalize argparse errors for release workflow logs."""
        raise ReleaseIdentityError(message)


def build_parser() -> argparse.ArgumentParser:
    """Build the release identity CLI parser."""
    parser = _Parser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    parser.add_argument("--tag", required=True)
    parser.add_argument("--release-zip", type=Path)
    parser.add_argument("--main-ref", help="Require the release tag target to be reachable from this ref.")
    parser.add_argument("--skip-git", action="store_true", help="Skip git tag/HEAD/main ancestry checks.")
    return parser


def main(argv: list[str] | None = None) -> int:
    """Run release identity validation."""
    parser = build_parser()
    try:
        args = parser.parse_args(argv)
        version = validate_release_identity(
            project_root=args.project_root,
            tag=args.tag,
            release_zip=args.release_zip,
            main_ref=args.main_ref,
            check_git=not args.skip_git,
        )
    except (ReleaseIdentityError, FileNotFoundError, ValueError, zipfile.BadZipFile) as exc:
        print(f"error: {exc}", file=sys.stderr)  # noqa: T201
        return 1

    print(version)  # noqa: T201
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
