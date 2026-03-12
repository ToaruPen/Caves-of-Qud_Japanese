"""Download Noto Sans CJK JP and create a subset OTF for the QudJP mod.

Downloads NotoSansCJKjp-Regular.otf from the official Noto CJK GitHub release,
then runs fonttools subsetter to produce a compact subset containing only the
Unicode ranges defined in docs/glyphset.txt.

Usage:
    python scripts/subset_font.py [--help]
"""

import argparse
import sys
import tempfile
import urllib.request
import zipfile
from pathlib import Path

_RELEASE_URL = "https://github.com/notofonts/noto-cjk/releases/download/Sans2.004/06_NotoSansCJKjp.zip"
_SOURCE_FONT_NAME = "NotoSansCJKjp-Regular.otf"
_MAX_SIZE_BYTES = 12 * 1024 * 1024  # 12 MB


def _find_project_root() -> Path:
    """Locate the project root by traversing up to find pyproject.toml.

    Returns:
        Path to the project root directory.

    Raises:
        FileNotFoundError: If pyproject.toml cannot be found in any parent.
    """
    current = Path(__file__).resolve().parent
    for candidate in [current, *current.parents]:
        if (candidate / "pyproject.toml").exists():
            return candidate
    msg = "pyproject.toml not found; cannot determine project root."
    raise FileNotFoundError(msg)


def _download_zip(url: str, dest: Path) -> None:
    """Download a file from *url* to *dest*.

    Args:
        url: HTTP/HTTPS URL to download.
        dest: Local path to write the downloaded bytes.

    Raises:
        OSError: If the download fails.
    """
    print(f"Downloading {url} …")  # noqa: T201
    urllib.request.urlretrieve(url, dest)  # noqa: S310
    print(f"  → saved to {dest} ({dest.stat().st_size // 1024 // 1024} MB)")  # noqa: T201


def _extract_font(zip_path: Path, font_name: str, dest_dir: Path) -> Path:
    """Extract *font_name* from *zip_path* into *dest_dir*.

    Args:
        zip_path: Path to the ZIP archive.
        font_name: Filename to extract (e.g. ``NotoSansCJKjp-Regular.otf``).
        dest_dir: Directory where the font file will be written.

    Returns:
        Path to the extracted font file.

    Raises:
        FileNotFoundError: If *font_name* is not found inside the archive.
    """
    with zipfile.ZipFile(zip_path) as zf:
        members = zf.namelist()
        match = next((m for m in members if m.endswith(font_name)), None)
        if match is None:
            msg = f"'{font_name}' not found in {zip_path}. Available entries: {members[:10]}"
            raise FileNotFoundError(msg)
        zf.extract(match, dest_dir)
        extracted = dest_dir / match
        # Flatten to dest_dir/<font_name> if nested in a subdirectory.
        flat = dest_dir / font_name
        if extracted != flat:
            extracted.rename(flat)
        return flat


def _run_subset(
    source_font: Path,
    glyphset_file: Path,
    output_font: Path,
) -> None:
    """Run fonttools subsetter on *source_font*.

    Args:
        source_font: Path to the full Noto Sans CJK JP OTF.
        glyphset_file: Path to the Unicode range definition file.
        output_font: Destination path for the subset OTF.

    Raises:
        ImportError: If fonttools is not installed.
        RuntimeError: If subsetting fails.
    """
    try:
        from fontTools import subset as ft_subset  # noqa: PLC0415
    except ImportError as exc:
        msg = "fonttools is not installed. Run: pip install fonttools brotli"
        raise ImportError(msg) from exc

    output_font.parent.mkdir(parents=True, exist_ok=True)

    print(f"Subsetting {source_font.name} → {output_font} …")  # noqa: T201
    options = ft_subset.Options()
    options.layout_features = ["*"]
    options.output_file = str(output_font)

    unicodes = _parse_unicode_ranges(glyphset_file)

    font = ft_subset.load_font(str(source_font), options)
    subsetter = ft_subset.Subsetter(options=options)
    subsetter.populate(unicodes=unicodes)
    subsetter.subset(font)
    ft_subset.save_font(font, str(output_font), options)
    print("  → subsetting complete")  # noqa: T201


def _parse_unicode_ranges(glyphset_file: Path) -> list[int]:
    """Parse a Unicode range file and return a flat list of codepoints.

    Lines starting with ``#`` are comments; blank lines are ignored.
    Range lines have the form ``U+XXXX-YYYY`` or ``U+XXXX``.

    Args:
        glyphset_file: Path to the glyphset definition file.

    Returns:
        Sorted list of Unicode codepoints.

    Raises:
        FileNotFoundError: If *glyphset_file* does not exist.
        ValueError: If a line cannot be parsed as a Unicode range.
    """
    if not glyphset_file.exists():
        msg = f"Glyphset file not found: {glyphset_file}"
        raise FileNotFoundError(msg)

    codepoints: list[int] = []
    for lineno, raw in enumerate(glyphset_file.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if not line.startswith("U+"):
            msg = f"{glyphset_file}:{lineno}: unexpected line format: {line!r}"
            raise ValueError(msg)
        parts = line[2:].split("-")
        if len(parts) == 1:
            codepoints.append(int(parts[0], 16))
        elif len(parts) == 2:  # noqa: PLR2004
            start = int(parts[0], 16)
            end = int(parts[1], 16)
            codepoints.extend(range(start, end + 1))
        else:
            msg = f"{glyphset_file}:{lineno}: cannot parse range: {line!r}"
            raise ValueError(msg)
    return sorted(set(codepoints))


def _validate_output(output_font: Path) -> None:
    """Validate that the subset font exists and is within the size limit.

    Args:
        output_font: Path to the generated subset OTF.

    Raises:
        FileNotFoundError: If the output file does not exist.
        ValueError: If the file exceeds the maximum allowed size.
    """
    if not output_font.exists():
        msg = f"Expected output font not found: {output_font}"
        raise FileNotFoundError(msg)

    size = output_font.stat().st_size
    size_mb = size / 1024 / 1024
    print(f"Output font: {output_font} ({size_mb:.2f} MB)")  # noqa: T201

    if size > _MAX_SIZE_BYTES:
        msg = (
            f"Subset font is {size_mb:.2f} MB, exceeding the 6 MB limit. "
            "Consider narrowing the Unicode ranges in docs/glyphset.txt."
        )
        raise ValueError(msg)
    print("  → size OK")  # noqa: T201


def subset_font(
    *,
    project_root: Path | None = None,
) -> None:
    """Download Noto Sans CJK JP and produce a subset OTF for QudJP.

    This function is idempotent: if the output font already exists and is
    valid, it will be regenerated (to ensure freshness).

    Args:
        project_root: Override the auto-detected project root. Useful for
            testing. Defaults to ``None`` (auto-detect).

    Raises:
        FileNotFoundError: If required input files are missing.
        ValueError: If the output font exceeds the size limit.
        ImportError: If fonttools is not installed.
    """
    root = project_root if project_root is not None else _find_project_root()
    glyphset_file = root / "docs" / "glyphset.txt"
    output_font = root / "Mods" / "QudJP" / "Fonts" / "NotoSansCJKjp-Regular-Subset.otf"

    with tempfile.TemporaryDirectory() as tmp_str:
        tmp = Path(tmp_str)
        zip_path = tmp / "NotoSansCJKjp.zip"

        _download_zip(_RELEASE_URL, zip_path)
        source_font = _extract_font(zip_path, _SOURCE_FONT_NAME, tmp)
        _run_subset(source_font, glyphset_file, output_font)

    _validate_output(output_font)
    print("Done.")  # noqa: T201


def _build_parser() -> argparse.ArgumentParser:
    """Build the CLI argument parser.

    Returns:
        Configured ArgumentParser instance.
    """
    return argparse.ArgumentParser(
        description=("Download Noto Sans CJK JP and create a subset OTF for the QudJP mod."),
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "The subset is defined by docs/glyphset.txt and written to\n"
            "Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf.\n\n"
            "Requires: pip install fonttools brotli"
        ),
    )


def main() -> None:
    """Entry point for the subset_font CLI.

    Raises:
        SystemExit: On any fatal error.
    """
    parser = _build_parser()
    parser.parse_args()  # exits on --help or bad args

    try:
        subset_font()
    except (FileNotFoundError, ValueError, ImportError, OSError) as exc:
        print(f"Error: {exc}", file=sys.stderr)  # noqa: T201
        sys.exit(1)


if __name__ == "__main__":
    main()
