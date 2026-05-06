from __future__ import annotations

import zipfile
from typing import TYPE_CHECKING

from scripts.verify_release_dll import verify_release_dll

if TYPE_CHECKING:
    from pathlib import Path


def test_verify_release_dll_accepts_required_markers(tmp_path: Path) -> None:
    """Accept a DLL that contains all required runtime markers."""
    dll = tmp_path / "QudJP.dll"
    dll.write_bytes(
        b"\0".join(
            [
                b"Unity.TextMeshPro",
                b"TextMeshProUguiFontPatch",
                b"TmpInputFieldFontPatch",
                b"InventoryLineFontFixer",
                b"DelayedInventoryLineRepairScheduler",
                b"ShouldPreserveActiveReplacementForTests",
            ],
        ),
    )

    assert verify_release_dll(dll) == []


def test_verify_release_dll_reports_missing_markers(tmp_path: Path) -> None:
    """Report every missing required runtime marker."""
    dll = tmp_path / "QudJP.dll"
    dll.write_bytes(b"Unity.TextMeshPro")

    assert verify_release_dll(dll) == [
        "TextMeshProUguiFontPatch",
        "TmpInputFieldFontPatch",
        "InventoryLineFontFixer",
        "DelayedInventoryLineRepairScheduler",
        "ShouldPreserveActiveReplacementForTests",
    ]


def test_verify_release_dll_reads_release_zip(tmp_path: Path) -> None:
    """Read QudJP.dll from a release ZIP before checking markers."""
    release_zip = tmp_path / "QudJP-v0.0.0.zip"
    with zipfile.ZipFile(release_zip, "w") as archive:
        archive.writestr(
            "QudJP/Assemblies/QudJP.dll",
            b"\0".join(
                [
                    b"Unity.TextMeshPro",
                    b"TextMeshProUguiFontPatch",
                    b"TmpInputFieldFontPatch",
                    b"InventoryLineFontFixer",
                    b"DelayedInventoryLineRepairScheduler",
                    b"ShouldPreserveActiveReplacementForTests",
                ],
            ),
        )

    assert verify_release_dll(release_zip) == []
