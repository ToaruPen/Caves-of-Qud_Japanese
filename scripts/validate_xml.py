"""Validate translation XML files for structure and common content issues."""

import argparse
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class ValidationResult:
    """Validation outcome for one XML file.

    Attributes:
        path: Validated XML file path.
        errors: Fatal validation errors.
        warnings: Non-fatal validation findings.
    """

    path: Path
    errors: list[str]
    warnings: list[str]


def _collect_xml_files(paths: list[Path]) -> list[Path]:
    xml_files: set[Path] = set()
    for input_path in paths:
        if not input_path.exists():
            msg = f"Path not found: {input_path}"
            raise FileNotFoundError(msg)

        if input_path.is_dir():
            xml_files.update(path for path in input_path.rglob("*.xml") if path.is_file())
            continue

        if input_path.is_file():
            xml_files.add(input_path)

    return sorted(xml_files, key=str)


def _find_unbalanced_color_line(text: str) -> int | None:
    open_lines: list[int] = []
    line_number = 1
    index = 0

    while index < len(text):
        if text.startswith("{{", index):
            open_lines.append(line_number)
            index += 2
            continue

        if text.startswith("}}", index):
            if not open_lines:
                return line_number
            open_lines.pop()
            index += 2
            continue

        if text[index] == "\n":
            line_number += 1
        index += 1

    if open_lines:
        return open_lines[0]
    return None


def _format_element_descriptor(element: ET.Element) -> str:
    if identifier := element.attrib.get("ID"):
        return f"'{element.tag}' ID=\"{identifier}\""
    if name := element.attrib.get("Name"):
        return f"'{element.tag}' Name=\"{name}\""
    return f"'{element.tag}'"


def _find_duplicate_siblings(root: ET.Element) -> list[str]:
    warnings: list[str] = []
    for parent in root.iter():
        duplicate_ids = _collect_duplicate_values(parent, attribute="ID")
        duplicate_names = _collect_duplicate_values(parent, attribute="Name")

        warnings.extend(
            f"Duplicate sibling ID=\"{duplicate_id}\" under parent '{parent.tag}'" for duplicate_id in duplicate_ids
        )
        warnings.extend(
            f"Duplicate sibling Name=\"{duplicate_name}\" under parent '{parent.tag}'"
            for duplicate_name in duplicate_names
        )
    return warnings


def _collect_duplicate_values(parent: ET.Element, *, attribute: str) -> list[str]:
    counts: dict[str, int] = {}
    for child in parent:
        if value := child.attrib.get(attribute):
            counts[value] = counts.get(value, 0) + 1
    return sorted(value for value, count in counts.items() if count > 1)


def _find_empty_text_elements(root: ET.Element) -> list[str]:
    warnings: list[str] = []

    for element in root.iter():
        if len(element) > 0:
            continue

        if element.text is None:
            if element.tag == "text":
                warnings.append(f"Empty text in element {_format_element_descriptor(element)}")
            continue

        if element.text.strip() == "":
            warnings.append(f"Empty text in element {_format_element_descriptor(element)}")

    return warnings


def validate_xml_file(path: Path) -> ValidationResult:
    """Validate one XML file and return its findings.

    Args:
        path: XML file path.

    Returns:
        Validation result with errors and warnings.
    """
    errors: list[str] = []
    warnings: list[str] = []

    try:
        root = ET.parse(path).getroot()  # noqa: S314 -- local repository XML validation tool
    except ET.ParseError as exc:
        errors.append(f"XML parse failed: {exc}")
        return ValidationResult(path=path, errors=errors, warnings=warnings)

    raw_text = path.read_text(encoding="utf-8", errors="ignore")
    if line_number := _find_unbalanced_color_line(raw_text):
        warnings.append(f"Unbalanced color code at line {line_number}")

    warnings.extend(_find_duplicate_siblings(root))
    warnings.extend(_find_empty_text_elements(root))
    return ValidationResult(path=path, errors=errors, warnings=warnings)


def _print_result(result: ValidationResult) -> None:
    if result.errors:
        print(f"Checking {result.path}... ERROR")  # noqa: T201
        for error in result.errors:
            print(f"  ERROR: {error}")  # noqa: T201
        return

    if result.warnings:
        warning_count = len(result.warnings)
        suffix = "warning" if warning_count == 1 else "warnings"
        print(f"Checking {result.path}... {warning_count} {suffix}")  # noqa: T201
        for warning in result.warnings:
            print(f"  WARNING: {warning}")  # noqa: T201
        return

    print(f"Checking {result.path}... OK")  # noqa: T201


def run_validation(paths: list[Path], *, strict: bool) -> int:
    """Run validation for input paths.

    Args:
        paths: File or directory paths to validate.
        strict: Treat warnings as failures.

    Returns:
        Exit code (0 on success, 1 on failure).
    """
    try:
        xml_files = _collect_xml_files(paths)
    except FileNotFoundError as exc:
        print(f"Error: {exc}", file=sys.stderr)  # noqa: T201
        return 1

    if not xml_files:
        print("Error: No XML files found in the provided paths.", file=sys.stderr)  # noqa: T201
        return 1

    total_errors = 0
    total_warnings = 0
    for file_path in xml_files:
        result = validate_xml_file(file_path)
        _print_result(result)
        total_errors += len(result.errors)
        total_warnings += len(result.warnings)

    if total_errors > 0:
        return 1
    if strict and total_warnings > 0:
        return 1
    return 0


def main(argv: list[str] | None = None) -> int:
    """Run the XML validation CLI.

    Args:
        argv: Command-line arguments. Defaults to ``sys.argv[1:]``.

    Returns:
        Exit code (0 on pass, 1 on failure).
    """
    parser = argparse.ArgumentParser(
        description="Validate translation XML files for parse errors and content warnings.",
    )
    parser.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="One or more XML files or directories to validate.",
    )
    parser.add_argument(
        "--strict",
        action="store_true",
        help="Treat warnings as errors (exit code 1).",
    )

    args = parser.parse_args(argv)
    return run_validation(args.paths, strict=args.strict)


if __name__ == "__main__":
    sys.exit(main())
