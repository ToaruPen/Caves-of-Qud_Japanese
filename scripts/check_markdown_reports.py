"""Check repository Markdown reports for recurring rendering pitfalls."""

from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from collections.abc import Sequence

_INLINE_CODE_RE = re.compile(r"(?<!`)`([^`\n]*)`(?!`)")
_LEADING_ISSUE_REFERENCE_RE = re.compile(r"^#\d+\b")
_MIN_TABLE_PIPE_COUNT = 2


class MarkdownReportError(Exception):
    """Raised when Markdown report inputs cannot be scanned."""


@dataclass(frozen=True)
class MarkdownReportIssue:
    """A single Markdown report issue."""

    path: Path
    line_number: int
    kind: str
    detail: str


def check_file(path: Path) -> list[MarkdownReportIssue]:
    """Check a Markdown report file for known rendering hazards."""
    issues: list[MarkdownReportIssue] = []
    for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        stripped = line.lstrip()
        if _LEADING_ISSUE_REFERENCE_RE.match(stripped):
            issues.append(
                MarkdownReportIssue(
                    path=path,
                    line_number=line_number,
                    kind="leading_issue_reference",
                    detail="Prefix issue references with prose, for example: Issue `#525`.",
                ),
            )

        if not _is_table_row(line):
            continue

        for match in _INLINE_CODE_RE.finditer(line):
            if "|" not in match.group(1):
                continue
            issues.append(
                MarkdownReportIssue(
                    path=path,
                    line_number=line_number,
                    kind="raw_pipe_in_table_code_span",
                    detail="Use HTML code plus an encoded pipe, for example: <code>{{K&#124;</code>.",
                ),
            )

    return issues


def check_paths(paths: Sequence[Path]) -> list[MarkdownReportIssue]:
    """Check all Markdown report files under the given paths."""
    issues: list[MarkdownReportIssue] = []
    for path in _iter_markdown_files(paths):
        issues.extend(check_file(path))
    return issues


def git_changed_report_files(base_ref: str, head_ref: str) -> list[Path]:
    """Return existing changed Markdown report paths between two git refs."""
    git = shutil.which("git")
    if git is None:
        msg = "git executable not found"
        raise MarkdownReportError(msg)
    try:
        result = subprocess.run(  # noqa: S603
            [git, "diff", "--name-only", f"{base_ref}...{head_ref}", "--", "docs/reports"],
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as exc:
        detail = (exc.stderr or "").strip() or str(exc)
        msg = f"git diff failed for {base_ref}...{head_ref}: {detail}"
        raise MarkdownReportError(msg) from exc
    return sorted(
        Path(line)
        for line in result.stdout.splitlines()
        if line.endswith(".md") and Path(line).is_file()
    )


def main(argv: Sequence[str] | None = None) -> int:
    """Run the Markdown report checker CLI."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="*", type=Path, help="Markdown files or directories to scan.")
    parser.add_argument("--base-ref", help="Base git ref for changed-report scanning.")
    parser.add_argument("--head-ref", help="Head git ref for changed-report scanning.")
    args = parser.parse_args(argv)

    try:
        paths = _resolve_cli_paths(args.paths, args.base_ref, args.head_ref)
        issues = check_paths(paths)
    except MarkdownReportError as exc:
        sys.stderr.write(f"error: {exc}\n")
        return 2

    if not issues:
        sys.stdout.write("Markdown report checks passed.\n")
        return 0

    for issue in issues:
        sys.stderr.write(
            f"{issue.path}:{issue.line_number}: {issue.kind}: {issue.detail}",
        )
        sys.stderr.write("\n")
    return 1


def _resolve_cli_paths(paths: Sequence[Path], base_ref: str | None, head_ref: str | None) -> list[Path]:
    """Resolve explicit CLI paths or changed Markdown report paths."""
    if bool(base_ref) != bool(head_ref):
        msg = "--base-ref and --head-ref must be provided together"
        raise MarkdownReportError(msg)
    if base_ref and head_ref:
        if paths:
            msg = "pass explicit paths or --base-ref/--head-ref, not both"
            raise MarkdownReportError(msg)
        return git_changed_report_files(base_ref, head_ref)
    if not paths:
        msg = "at least one path or --base-ref/--head-ref is required"
        raise MarkdownReportError(msg)
    return list(paths)


def _is_table_row(line: str) -> bool:
    """Return whether a line is a pipe-table row in the report style."""
    stripped = line.lstrip()
    return stripped.startswith("|") and stripped.count("|") >= _MIN_TABLE_PIPE_COUNT


def _iter_markdown_files(paths: Sequence[Path]) -> list[Path]:
    """Collect Markdown files from explicit file and directory inputs."""
    files: set[Path] = set()
    for path in paths:
        if not path.exists():
            msg = f"Path not found: {path}"
            raise MarkdownReportError(msg)
        if path.is_file():
            if path.suffix.lower() == ".md":
                files.add(path)
            continue
        if path.is_dir():
            files.update(path for path in path.rglob("*.md") if path.is_file())
            continue

        msg = f"Path is not a regular file or directory: {path}"
        raise MarkdownReportError(msg)
    return sorted(files, key=str)


if __name__ == "__main__":
    raise SystemExit(main())
