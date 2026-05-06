"""Python wrapper for the Roslyn semantic probe CLI."""
# ruff: noqa: S603 -- invokes the repo-local dotnet probe with explicit arguments

from __future__ import annotations

import argparse
import json
import shlex
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Final, cast

DEFAULT_SOURCE_ROOT: Final = Path("~/dev/coq-decompiled_stable").expanduser()
ROSLYN_PROBE_TIMEOUT_SECONDS: Final = 600
REPO_ROOT: Final = Path(__file__).resolve().parents[1]
PROJECT_PATH: Final = REPO_ROOT / "scripts" / "tools" / "RoslynSemanticProbe" / "RoslynSemanticProbe.csproj"
REQUIRED_TOP_LEVEL_KEYS: Final = {"schema_version", "query", "metrics", "hits"}
REQUIRED_METRIC_KEYS: Final = {
    "total_files",
    "resolved_matching_owner_hits",
    "candidate_matching_owner_hits",
    "unresolved_hits",
    "status_counts",
    "owner_counts",
    "string_argument_counts",
    "first_string_argument_counts",
    "string_risk_counts",
}
type JsonObject = dict[str, object]


def run_probe(args: list[str]) -> JsonObject:
    """Run the Roslyn semantic probe and return its JSON payload."""
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        msg = "dotnet 10.0.x SDK required to run the Roslyn semantic probe"
        raise RuntimeError(msg)
    if not PROJECT_PATH.is_file():
        msg = f"Roslyn semantic probe project is missing: {PROJECT_PATH}"
        raise RuntimeError(msg)

    output_path = _output_path_from_args(args)
    command = [dotnet, "run", "--project", str(PROJECT_PATH), "--", *args]
    try:
        result = subprocess.run(
            command,
            capture_output=True,
            text=True,
            check=False,
            timeout=ROSLYN_PROBE_TIMEOUT_SECONDS,
        )
    except subprocess.TimeoutExpired as exc:
        details = "\n".join(part for part in (_output_text(exc.stdout), _output_text(exc.stderr)) if part)
        msg = f"Roslyn semantic probe timed out after {ROSLYN_PROBE_TIMEOUT_SECONDS}s: {shlex.join(command)}"
        if details:
            msg = f"{msg}\n{details}"
        raise RuntimeError(msg) from exc

    if result.returncode != 0:
        details = "\n".join(part for part in (result.stdout.strip(), result.stderr.strip()) if part)
        msg = f"Roslyn semantic probe failed with exit {result.returncode}: {shlex.join(command)}"
        if details:
            msg = f"{msg}\n{details}"
        raise RuntimeError(msg)

    if result.stdout.strip():
        return _load_payload(result.stdout)
    if output_path is not None:
        return _load_payload(output_path.read_text(encoding="utf-8"))

    msg = "Roslyn semantic probe produced no JSON on stdout"
    raise RuntimeError(msg)


def main(argv: list[str] | None = None) -> int:
    """Run the wrapper CLI."""
    parser = argparse.ArgumentParser(description="Run the repo-local Roslyn semantic probe.")
    _ = parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    args, passthrough = parser.parse_known_args(argv)
    source_root = cast("Path", args.source_root).expanduser()
    if not source_root.is_dir():
        _ = sys.stderr.write(f"source root does not exist or is not a directory: {source_root}\n")
        return 1

    try:
        payload = run_probe(["--source-root", str(source_root), *passthrough])
    except RuntimeError as exc:
        _ = sys.stderr.write(f"{exc}\n")
        return 1

    _ = sys.stdout.write(json.dumps(payload, ensure_ascii=False, indent=2) + "\n")
    return 0


def _load_payload(json_text: str) -> JsonObject:
    try:
        payload = cast("JsonObject", json.loads(json_text))
    except json.JSONDecodeError as exc:
        msg = f"Roslyn semantic probe produced unreadable JSON: {exc}"
        raise RuntimeError(msg) from exc

    missing_top_level = sorted(REQUIRED_TOP_LEVEL_KEYS - payload.keys())
    if missing_top_level:
        msg = f"Roslyn semantic probe payload missing top-level keys: {missing_top_level}"
        raise RuntimeError(msg)

    raw_metrics = payload.get("metrics")
    if not isinstance(raw_metrics, dict):
        msg = "Roslyn semantic probe payload metrics must be an object"
        raise TypeError(msg)
    metrics = cast("JsonObject", raw_metrics)

    missing_metrics = sorted(REQUIRED_METRIC_KEYS - metrics.keys())
    if missing_metrics:
        msg = f"Roslyn semantic probe payload missing metric keys: {missing_metrics}"
        raise RuntimeError(msg)

    return payload


def _output_path_from_args(args: list[str]) -> Path | None:
    for index, arg in enumerate(args):
        if arg == "--output" and index + 1 < len(args):
            return Path(args[index + 1]).expanduser()
    return None


def _output_text(value: str | bytes | None) -> str:
    if value is None:
        return ""
    if isinstance(value, bytes):
        return value.decode(errors="replace").strip()
    return value.strip()


if __name__ == "__main__":
    raise SystemExit(main())
