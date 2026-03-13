#!/usr/bin/env bash
# Double-click this file to launch Caves of Qud under Rosetta 2.
# Delegates to scripts/launch_rosetta.sh for all logic.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CURRENT_TTY="$(tty)"

set +e
"${SCRIPT_DIR}/scripts/launch_rosetta.sh"
EXIT_CODE=$?
set -e

if [[ ${EXIT_CODE} -eq 0 ]]; then
  osascript <<EOF >/dev/null 2>&1 || true
tell application "Terminal"
  repeat with terminalWindow in windows
    try
      if tty of selected tab of terminalWindow is "${CURRENT_TTY}" then
        close terminalWindow saving no
        exit repeat
      end if
    end try
  end repeat
end tell
EOF
fi

exit ${EXIT_CODE}
