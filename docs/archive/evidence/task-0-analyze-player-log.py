#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

DEFAULT_LOG = Path.home() / "Library/Logs/Freehold Games/CavesOfQud/Player.log"
PATTERNS = {
    "harmony": re.compile(r"harmony|patchall|0harmony", re.IGNORECASE),
    "poc_mod": re.compile(r"qudjp[_ .-]?poc|qudjp\.poc\.runtime", re.IGNORECASE),
    "memory_protect": re.compile(r"mprotect|eacces", re.IGNORECASE),
    "exceptions": re.compile(r"exception|error|failed", re.IGNORECASE),
}


def main() -> int:
    log_path = Path(sys.argv[1]).expanduser() if len(sys.argv) > 1 else DEFAULT_LOG
    if not log_path.exists():
        print(f"[ERROR] Log file not found: {log_path}")
        return 2

    lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    print(f"[INFO] Log path: {log_path}")
    print(f"[INFO] Total lines: {len(lines)}")

    for name, pattern in PATTERNS.items():
        matches: list[tuple[int, str]] = []
        for idx, line in enumerate(lines, start=1):
            if pattern.search(line):
                matches.append((idx, line))

        print(f"\n[{name}] matches: {len(matches)}")
        for idx, line in matches[:20]:
            print(f"{idx}: {line}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
