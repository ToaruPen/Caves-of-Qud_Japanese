"""Contract tests for CodeRabbit static-analysis guidance."""

from __future__ import annotations

import json
from pathlib import Path

_REPO_ROOT = Path(__file__).resolve().parents[2]
_ROUTE_FIXTURE = _REPO_ROOT / "scripts" / "tests" / "fixtures" / "coderabbit_route_ownership.cases.json"


def test_coderabbit_global_guideline_stays_root_scoped_and_raw_free() -> None:
    """CodeRabbit must receive the root entrypoint, not full generated inventories."""
    config = (_REPO_ROOT / ".coderabbit.yaml").read_text(encoding="utf-8")
    file_patterns = config.split("filePatterns:", maxsplit=1)[1].split("\n\n", maxsplit=1)[0]
    assert '      - "CODERABBIT.md"' in file_patterns
    assert "docs/coderabbit/*.md" not in file_patterns
    assert "roslyn-text-construction-inventory.json" not in file_patterns

    root_guideline = (_REPO_ROOT / "CODERABBIT.md").read_text(encoding="utf-8")
    assert "CodeRabbit-specific global guideline" in root_guideline
    assert "full generated family inventory JSON" in root_guideline
    assert "Do not use `scripts/legacies/scan_text_producers.py`" in root_guideline


def test_route_ownership_fixture_is_reflected_in_root_coderabbit_guideline() -> None:
    """Route ownership review defaults should be test data, not only prose."""
    fixture = json.loads(_ROUTE_FIXTURE.read_text(encoding="utf-8"))
    assert fixture["schema_version"] == 1
    route_rows = fixture["routes"]
    assert len(route_rows) >= 7

    guideline = (_REPO_ROOT / "CODERABBIT.md").read_text(encoding="utf-8")
    for row in route_rows:
        assert row["route_family"] in guideline
        assert row["review_stance"] in guideline
        assert row["flag_when"] in guideline
        assert row["surfaces"], row


def test_static_analysis_contract_names_required_token_and_glossary_gates() -> None:
    """Critical color/token/glossary checks must be visible from the root guideline."""
    guideline = (_REPO_ROOT / "CODERABBIT.md").read_text(encoding="utf-8")
    for token in ("{{W|text}}", "&G", "^r", "&&", "^^", "<color=#44ff88>text</color>", "=variable.name=", "{0}"):
        assert token in guideline
    for gate in ("just localization-check", "just translation-token-check", "docs/glossary.csv"):
        assert gate in guideline
