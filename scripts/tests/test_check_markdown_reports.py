"""Tests for Markdown report quality checks."""

from __future__ import annotations

from typing import TYPE_CHECKING

import pytest

from scripts.check_markdown_reports import MarkdownReportError, check_paths, main

if TYPE_CHECKING:
    from pathlib import Path


def test_reports_raw_pipe_inside_table_code_span(tmp_path: Path) -> None:
    """Table rows must not contain raw pipes inside inline code spans."""
    report = tmp_path / "report.md"
    report.write_text(
        "| Count | Producer |\n"
        "| ---: | --- |\n"
        "| 1 | `_IncrementProgress()` inserts `}}>{{K|` |\n",
        encoding="utf-8",
    )

    issues = check_paths([report])

    assert len(issues) == 1
    assert issues[0].kind == "raw_pipe_in_table_code_span"
    assert issues[0].line_number == 3


def test_accepts_html_code_entity_for_table_pipe(tmp_path: Path) -> None:
    """HTML code with an encoded pipe is safe in Markdown table cells."""
    report = tmp_path / "report.md"
    report.write_text(
        "| Count | Producer |\n"
        "| ---: | --- |\n"
        "| 1 | inserts <code>}}>{{K&#124;</code> |\n",
        encoding="utf-8",
    )

    assert check_paths([report]) == []


def test_reports_leading_issue_reference_that_can_parse_as_heading(tmp_path: Path) -> None:
    """Issue references at the start of prose lines must be prefixed with text."""
    report = tmp_path / "report.md"
    report.write_text("#459 tracks broad Restore ownership gaps.\n", encoding="utf-8")

    issues = check_paths([report])

    assert len(issues) == 1
    assert issues[0].kind == "leading_issue_reference"
    assert issues[0].line_number == 1


def test_accepts_issue_references_in_normal_prose(tmp_path: Path) -> None:
    """Issue references are fine when the line starts as prose."""
    report = tmp_path / "report.md"
    report.write_text("Issue `#459` tracks broad Restore ownership gaps.\n", encoding="utf-8")

    assert check_paths([report]) == []


def test_main_reports_issues_without_traceback(tmp_path: Path, capsys: pytest.CaptureFixture[str]) -> None:
    """The CLI returns a nonzero status and prints stable issue locations."""
    report = tmp_path / "report.md"
    report.write_text("#525 is narrower.\n", encoding="utf-8")

    assert main([str(report)]) == 1

    captured = capsys.readouterr()
    assert "leading_issue_reference" in captured.err
    assert "report.md:1" in captured.err


def test_main_raises_for_missing_paths(tmp_path: Path) -> None:
    """Missing paths are reported as CLI errors by the caller."""
    with pytest.raises(MarkdownReportError, match="Path not found"):
        check_paths([tmp_path / "missing.md"])
