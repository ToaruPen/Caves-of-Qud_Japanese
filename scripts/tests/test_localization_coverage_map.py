from __future__ import annotations

from pathlib import Path

from scripts.localization_coverage_map import (
    REQUIRED_SURFACE_IDS,
    load_map,
    validate_map,
)

REPO_ROOT = Path(__file__).resolve().parents[2]
MAP_PATH = REPO_ROOT / "docs" / "localization-coverage-map.json"


def test_localization_coverage_map_is_valid_and_complete() -> None:
    """Coverage map must stay machine-valid and include every required surface lane."""
    errors = validate_map(REPO_ROOT)

    assert errors == []


def test_localization_coverage_map_keeps_runtime_and_sink_boundary_lanes_explicit() -> None:
    """The map must keep runtime and sink-boundary lanes separate from static coverage."""
    document = load_map(MAP_PATH)
    surfaces = {surface["id"]: surface for surface in document["surfaces"]}

    assert set(surfaces) >= REQUIRED_SURFACE_IDS
    assert "blueprint_xml_data_sources" not in surfaces
    assert surfaces["runtime_observability_triage"]["status"] == "runtime_evidence"
    assert surfaces["renderer_and_sink_boundaries"]["status"] == "boundary_observed"


def test_localization_coverage_map_does_not_treat_legacy_inventory_as_source_of_truth() -> None:
    """Legacy bridge artifacts must remain explicitly view-only."""
    document = load_map(MAP_PATH)
    legacy = next(surface for surface in document["surfaces"] if surface["id"] == "legacy_candidate_inventory")

    assert legacy["status"] == "legacy_view_only"
    assert legacy["category"] == "legacy_view"
    assert "not a source of truth" in " ".join(legacy["known_limits"])
