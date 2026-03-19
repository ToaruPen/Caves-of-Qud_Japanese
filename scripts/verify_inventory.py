"""Automate local inventory screenshot verification for Caves of Qud."""

import argparse
import ctypes
import functools
import json
import plistlib
import shutil
import subprocess
import sys
import tempfile
import time
from collections.abc import Callable
from ctypes import util
from datetime import UTC, datetime
from pathlib import Path

_PLAYER_LOG = Path.home() / "Library" / "Logs" / "Freehold Games" / "CavesOfQud" / "Player.log"
_BUILD_MARKER = "[QudJP] Build marker:"
_TITLE_READY_PROBE = "MainMenuLocalizationPatch"
_LOAD_READY_PROBE = "[QudJP] QudMenuBottomContextProbe/RefreshButtonsAfter/v1:"
_DEFAULT_INVENTORY_PATTERNS: tuple[str, ...] = (
    "[QudJP] DescriptionInventoryActionProbe:",
    "[QudJP] InventoryLineReplacement/v1:",
    "[QudJP] InventoryLineReplacementStateNextFrame/v1:",
    "[QudJP] EquipmentLineProbe/v1:",
)
_SPECIAL_KEY_CODES = {
    "down": 125,
    "enter": 76,
    "escape": 53,
    "left": 123,
    "return": 36,
    "right": 124,
    "space": 49,
    "tab": 48,
    "up": 126,
}
_CHARACTER_KEY_CODES = {
    " ": 49,
    "b": 11,
    "c": 8,
    "h": 4,
    "i": 34,
    "q": 12,
    "x": 7,
    "y": 16,
}
_MODIFIER_FLAGS = {
    "command": 0x00100000,
    "control": 0x00040000,
    "option": 0x00080000,
    "shift": 0x00020000,
}
_MOUSE_EVENT_LEFT_DOWN = 1
_MOUSE_EVENT_LEFT_UP = 2
_MOUSE_EVENT_MOVE = 5
_HAMMERSPOON_APP_CANDIDATES = (
    Path("/Applications/Hammerspoon.app"),
    Path.home() / "Applications" / "Hammerspoon.app",
    Path("/Applications/Setapp/Hammerspoon.app"),
)
_COQ_BUNDLE_IDENTIFIER = "com.FreeholdGames.CavesOfQud"


class _CGPoint(ctypes.Structure):
    _fields_ = [("x", ctypes.c_double), ("y", ctypes.c_double)]


def _default_screenshot_path() -> Path:
    timestamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    return Path(tempfile.gettempdir()) / f"qudjp-inventory-{timestamp}.png"


def _find_matching_patterns(text: str, patterns: tuple[str, ...]) -> list[str]:
    return [pattern for pattern in patterns if pattern in text]


def _read_log_delta(path: Path, offset: int) -> str:
    with path.open("rb") as handle:
        size = path.stat().st_size
        handle.seek(0 if offset > size else offset)
        return handle.read().decode("utf-8", errors="replace")


def _current_log_offset(path: Path) -> int:
    if not path.exists():
        return 0
    return path.stat().st_size


def _wait_for_log_matches(
    path: Path,
    offset: int,
    timeout: float,
    matcher: Callable[[str], list[str]],
) -> tuple[list[str], str]:
    deadline = time.monotonic() + timeout
    latest = ""
    while time.monotonic() < deadline:
        latest = _read_log_delta(path, offset)
        matches = matcher(latest)
        if matches:
            return matches, latest
        time.sleep(1.0)
    return [], latest


def _io_console_locked_from_root(root: dict[str, object]) -> bool:
    value = root.get("IOConsoleLocked")
    return bool(value)


def _wait_for_patterns(path: Path, offset: int, patterns: tuple[str, ...], timeout: float) -> tuple[list[str], str]:
    return _wait_for_log_matches(path, offset, timeout, lambda text: _find_matching_patterns(text, patterns))


def _title_ready_matches(text: str) -> list[str]:
    if _TITLE_READY_PROBE in text:
        return [_TITLE_READY_PROBE]
    return []


def _wait_for_title_ready(path: Path, offset: int, timeout: float) -> tuple[list[str], str]:
    return _wait_for_log_matches(path, offset, timeout, _title_ready_matches)


def _load_ready_matches(text: str) -> list[str]:
    matches: list[str] = []
    if _LOAD_READY_PROBE in text:
        matches.append(_LOAD_READY_PROBE)

    for line in text.splitlines():
        if "[QudJP] Translator:" not in line:
            continue
        if "MainMenuLocalizationPatch" in line:
            continue
        matches.append("[QudJP] Translator:<non-main-menu>")
        break

    return matches


def _wait_for_load_ready(path: Path, offset: int, timeout: float) -> tuple[list[str], str]:
    return _wait_for_log_matches(path, offset, timeout, _load_ready_matches)


def _key_code_for(key: str) -> int:
    normalized_key = key.lower()
    if normalized_key in _SPECIAL_KEY_CODES:
        return _SPECIAL_KEY_CODES[normalized_key]

    if len(key) == 1 and key in _CHARACTER_KEY_CODES:
        return _CHARACTER_KEY_CODES[key]

    msg = f"Unsupported key: {key}"
    raise ValueError(msg)


def _modifier_mask(modifiers: tuple[str, ...]) -> int:
    invalid_modifiers = [modifier for modifier in modifiers if modifier not in _MODIFIER_FLAGS]
    if invalid_modifiers:
        msg = f"Unsupported modifier(s): {', '.join(invalid_modifiers)}"
        raise ValueError(msg)

    mask = 0
    for modifier in modifiers:
        mask |= _MODIFIER_FLAGS[modifier]
    return mask


@functools.lru_cache(maxsize=1)
def _application_services() -> ctypes.CDLL:
    path = util.find_library("ApplicationServices")
    if path is None:
        msg = "ApplicationServices framework not found."
        raise RuntimeError(msg)

    library = ctypes.cdll.LoadLibrary(path)
    library.CGEventCreateKeyboardEvent.restype = ctypes.c_void_p
    library.CGEventCreateKeyboardEvent.argtypes = [ctypes.c_void_p, ctypes.c_uint16, ctypes.c_bool]
    library.CGEventCreateMouseEvent.restype = ctypes.c_void_p
    library.CGEventCreateMouseEvent.argtypes = [ctypes.c_void_p, ctypes.c_uint32, _CGPoint, ctypes.c_uint32]
    library.CGEventSetFlags.argtypes = [ctypes.c_void_p, ctypes.c_uint64]
    library.CGEventPost.argtypes = [ctypes.c_uint32, ctypes.c_void_p]
    library.CFRelease.argtypes = [ctypes.c_void_p]
    return library


def _build_focus_script(pid: int) -> str:
    return (
        'tell application "System Events"\n'
        f"  set frontmost of first application process whose unix id is {pid} to true\n"
        "end tell"
    )


def _build_activate_application_script(bundle_identifier: str) -> str:
    return f'tell application id "{_escape_osascript_string(bundle_identifier)}" to activate'


def _escape_osascript_string(text: str) -> str:
    return text.replace("\\", "\\\\").replace('"', '\\"')


def _build_hammerspoon_focus_lua(pid: int, output_path: Path) -> str:
    escaped_output = output_path.as_posix().replace("\\", "\\\\").replace('"', '\\"')
    return f"""local outPath = \"{escaped_output}\"
local lines = {{}}
local function add(...)
  local parts = {{}}
  for i = 1, select("#", ...) do
    parts[#parts + 1] = tostring(select(i, ...))
  end
  lines[#lines + 1] = table.concat(parts, " ")
end
local ok, err = pcall(function()
  local wins = hs.window.allWindows()
  local target = nil
  for _, win in ipairs(wins) do
    local app = win:application()
    if app and app:pid() == {pid} then
      target = win
      break
    end
  end
  add("found=", tostring(target ~= nil))
  if target then
    local focused = target:focus()
    add("focus_result=", tostring(focused))
    hs.timer.usleep(700000)
    local front = hs.application.frontmostApplication()
    local focusedWindow = hs.window.focusedWindow()
    add("front=", front and front:name() or "nil")
    add("focused_title=", focusedWindow and (focusedWindow:title() or "") or "nil")
  end
end)
if not ok then
  add("error=", err)
end
local file = assert(io.open(outPath, "w"))
file:write(table.concat(lines, "\n"))
file:close()
"""


def _parse_hammerspoon_focus_output(text: str) -> dict[str, str]:
    result: dict[str, str] = {}
    for line in text.splitlines():
        if "=" not in line:
            continue
        key, value = line.split("=", 1)
        result[key.strip()] = value.strip()
    return result


def _project_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _ensure_supported_environment() -> None:
    if sys.platform != "darwin":
        msg = "verify_inventory.py is supported on macOS only."
        raise RuntimeError(msg)

    missing_tools = [tool for tool in ("osascript", "screencapture") if shutil.which(tool) is None]
    if missing_tools:
        msg = f"Required tool(s) not found in PATH: {', '.join(missing_tools)}"
        raise RuntimeError(msg)

    if not _PLAYER_LOG.parent.is_dir():
        msg = f"Player.log directory not found: {_PLAYER_LOG.parent}"
        raise FileNotFoundError(msg)


def _ensure_unlocked_console() -> None:
    result = subprocess.run(["/usr/sbin/ioreg", "-a", "-n", "Root"], capture_output=True, check=True)
    root = plistlib.loads(result.stdout)
    if _io_console_locked_from_root(root):
        msg = "macOS console session is locked. Unlock the Mac before running verify_inventory.py."
        raise RuntimeError(msg)


def _locate_hammerspoon_app() -> Path | None:
    for path in _HAMMERSPOON_APP_CANDIDATES:
        if path.exists():
            return path
    return None


def _run_subprocess(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, capture_output=True, text=True, check=True)  # noqa: S603 -- trusted repo-local or system command


def _run_osascript(script: str) -> subprocess.CompletedProcess[str]:
    return _run_subprocess(["osascript", "-e", script])


def _sync_mod(*, skip_sync: bool) -> None:
    if skip_sync:
        return

    script = _project_root() / "scripts" / "sync_mod.py"
    _run_subprocess([sys.executable, str(script)])


def _launch_game() -> subprocess.Popen[bytes]:
    script = _project_root() / "scripts" / "launch_rosetta.sh"
    return subprocess.Popen(  # noqa: S603 -- trusted repo-local launcher script
        [str(script)],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )


def _send_key(pid: int, key: str, modifiers: tuple[str, ...] = ()) -> None:
    library = _application_services()
    key_code = _key_code_for(key)
    modifier_mask = _modifier_mask(modifiers)

    _stabilize_game_focus(pid)
    time.sleep(0.2)
    for is_key_down in (True, False):
        event = library.CGEventCreateKeyboardEvent(None, key_code, is_key_down)
        if not event:
            msg = f"Failed to create keyboard event for key: {key}"
            raise RuntimeError(msg)
        if modifier_mask:
            library.CGEventSetFlags(event, modifier_mask)
        library.CGEventPost(0, event)
        library.CFRelease(event)
        time.sleep(0.05)


def _focus_process(pid: int) -> None:
    script = _build_focus_script(pid)
    _run_osascript(script)


def _activate_game_application() -> None:
    _run_osascript(_build_activate_application_script(_COQ_BUNDLE_IDENTIFIER))


def _open_hammerspoon_console() -> None:
    app_path = _locate_hammerspoon_app()
    if app_path is None:
        msg = "Hammerspoon.app is not installed."
        raise RuntimeError(msg)

    _run_subprocess(["open", str(app_path)])
    time.sleep(0.5)
    _run_osascript(
        'tell application "System Events" to tell process "Hammerspoon" '
        'to click menu item "Console..." of menu 1 of menu bar item "File" of menu bar 1'
    )
    time.sleep(0.5)


def _close_hammerspoon_console() -> None:
    try:
        _run_osascript(
            'tell application "System Events" to tell process "Hammerspoon" '
            'to click menu item "Close" of menu 1 of menu bar item "Window" of menu bar 1'
        )
    except subprocess.CalledProcessError:
        return


def _run_hammerspoon_lua(lua_source: str, output_path: Path, timeout: float = 5.0) -> str:
    _open_hammerspoon_console()
    with tempfile.NamedTemporaryFile(
        "w", suffix=".lua", prefix="qudjp-hs-", delete=False, encoding="utf-8"
    ) as lua_file:
        lua_file.write(lua_source)
        lua_path = Path(lua_file.name)

    output_path.unlink(missing_ok=True)
    command = f'dofile("{_escape_osascript_string(lua_path.as_posix())}")'

    try:
        set_value_script = (
            'tell application "System Events" to tell process "Hammerspoon" '
            'to tell window "Hammerspoon Console" '
            f'to set value of text field 1 to "{_escape_osascript_string(command)}"'
        )
        _run_osascript(set_value_script)
        _run_osascript(
            'tell application "System Events" to tell process "Hammerspoon" '
            'to tell window "Hammerspoon Console" to click text field 1'
        )
        time.sleep(0.3)
        _run_osascript('tell application "System Events" to key code 36')

        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if output_path.exists():
                return output_path.read_text(encoding="utf-8")
            time.sleep(0.1)

        msg = f"Timed out waiting for Hammerspoon output at {output_path}."
        raise RuntimeError(msg)
    finally:
        lua_path.unlink(missing_ok=True)
        output_path.unlink(missing_ok=True)
        _close_hammerspoon_console()


def _focus_process_with_hammerspoon(pid: int) -> None:
    output_path = Path(tempfile.mkstemp(prefix="qudjp-hs-focus-", suffix=".txt")[1])
    output_path.unlink(missing_ok=True)
    lua_source = _build_hammerspoon_focus_lua(pid, output_path)
    try:
        result = _run_hammerspoon_lua(lua_source, output_path)
        parsed = _parse_hammerspoon_focus_output(result)
        if parsed.get("found") != "true":
            msg = f"Hammerspoon could not find CoQ window for pid {pid}. Output: {result}"
            raise RuntimeError(msg)
        if parsed.get("front") not in {"CoQ", "CavesOfQud"}:
            msg = f"Hammerspoon did not bring CoQ frontmost. Output: {result}"
            raise RuntimeError(msg)
    finally:
        output_path.unlink(missing_ok=True)


def _stabilize_game_focus(pid: int) -> None:
    if _locate_hammerspoon_app() is not None:
        try:
            _focus_process_with_hammerspoon(pid)
        except (RuntimeError, subprocess.CalledProcessError):
            pass
        else:
            return

    try:
        _activate_game_application()
    except subprocess.CalledProcessError:
        _focus_process(pid)


def _click_point(pid: int, x: int, y: int) -> None:
    library = _application_services()
    point = _CGPoint(float(x), float(y))

    _stabilize_game_focus(pid)
    time.sleep(0.2)
    for event_type in (_MOUSE_EVENT_MOVE, _MOUSE_EVENT_LEFT_DOWN, _MOUSE_EVENT_LEFT_UP):
        event = library.CGEventCreateMouseEvent(None, event_type, point, 0)
        if not event:
            msg = f"Failed to create mouse event at ({x}, {y})."
            raise RuntimeError(msg)
        library.CGEventPost(0, event)
        library.CFRelease(event)
        time.sleep(0.05)


def _capture_screenshot(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    _run_subprocess(["screencapture", "-x", str(path)])


def _wait_for_exit(process: subprocess.Popen[bytes], timeout: float) -> bool:
    try:
        process.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        return False
    return True


def _stop_game(process: subprocess.Popen[bytes], quit_confirm_key: str, quit_timeout: float) -> None:
    if process.poll() is not None:
        return

    try:
        _send_key(process.pid, "q", ("command",))
    except subprocess.CalledProcessError:
        return
    time.sleep(1.0)
    if process.poll() is not None:
        return

    if quit_confirm_key:
        try:
            _send_key(process.pid, quit_confirm_key)
        except subprocess.CalledProcessError:
            return
        time.sleep(1.0)

    if _wait_for_exit(process, quit_timeout):
        return

    process.terminate()
    if _wait_for_exit(process, 5.0):
        return

    process.kill()
    process.wait(timeout=5.0)


def _parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Deploy QudJP, launch Caves of Qud via Rosetta, open inventory, and capture a screenshot.",
    )
    parser.add_argument(
        "--skip-sync",
        action="store_true",
        help="Skip `python scripts/sync_mod.py` before launching the game.",
    )
    parser.add_argument(
        "--screenshot-path",
        type=Path,
        default=_default_screenshot_path(),
        help="Write the screenshot to this path instead of a temp-file path.",
    )
    parser.add_argument(
        "--launch-timeout",
        type=float,
        default=90.0,
        help="Seconds to wait for the QudJP build marker after launch.",
    )
    parser.add_argument(
        "--title-ready-wait",
        type=float,
        default=12.0,
        help="Fallback seconds to wait before title-screen input if no title-ready log signal appears.",
    )
    parser.add_argument(
        "--title-ready-timeout",
        type=float,
        default=30.0,
        help="Seconds to wait for a title-ready log signal before falling back to title-ready-wait.",
    )
    parser.add_argument(
        "--menu-up-presses",
        type=int,
        default=0,
        help="How many times to press Up before selecting from the title menu.",
    )
    parser.add_argument(
        "--menu-down-presses",
        type=int,
        default=1,
        help="How many times to press Down before selecting Continue from the title menu.",
    )
    parser.add_argument(
        "--menu-navigation-interval",
        type=float,
        default=0.2,
        help="Seconds to wait between title-menu navigation key presses.",
    )
    parser.add_argument(
        "--menu-up-key",
        default="up",
        help="Key used for title-menu upward navigation (default: up).",
    )
    parser.add_argument(
        "--menu-down-key",
        default="down",
        help="Key used for title-menu downward navigation (default: down).",
    )
    parser.add_argument(
        "--continue-key",
        default="space",
        help="Key to press at the main menu after selecting Continue (default: space).",
    )
    parser.add_argument(
        "--continue-presses",
        type=int,
        default=2,
        help="How many times to press the continue key (default: 2 for Continue then first save).",
    )
    parser.add_argument(
        "--continue-interval",
        type=float,
        default=1.0,
        help="Seconds to wait between continue key presses.",
    )
    parser.add_argument(
        "--continue-click-x",
        type=int,
        help="Optional screen x coordinate to click instead of using the continue key.",
    )
    parser.add_argument(
        "--continue-click-y",
        type=int,
        help="Optional screen y coordinate to click instead of using the continue key.",
    )
    parser.add_argument(
        "--load-wait",
        type=float,
        default=12.0,
        help="Fallback seconds to wait before inventory if no load-ready log signal appears.",
    )
    parser.add_argument(
        "--load-ready-timeout",
        type=float,
        default=45.0,
        help="Seconds to wait for a post-Continue world-ready log signal before falling back to load-wait.",
    )
    parser.add_argument(
        "--inventory-key",
        default="i",
        help="Key to press to open inventory (default: i). Use an empty string to skip sending it.",
    )
    parser.add_argument(
        "--inventory-timeout",
        type=float,
        default=12.0,
        help="Seconds to wait for inventory-related probe lines after opening inventory.",
    )
    parser.add_argument(
        "--inventory-pattern",
        action="append",
        help=(
            "Additional Player.log pattern to treat as evidence that inventory opened. "
            "Can be repeated. Defaults to DescriptionInventoryActionProbe and InventoryLineReplacement/v1."
        ),
    )
    parser.add_argument(
        "--quit-confirm-key",
        default="y",
        help="Key to confirm the quit prompt after Cmd+Q (default: y). Use an empty string to skip.",
    )
    parser.add_argument(
        "--quit-timeout",
        type=float,
        default=15.0,
        help="Seconds to wait for a graceful shutdown before terminate/kill.",
    )
    return parser.parse_args(argv)


def _build_result(
    screenshot_path: Path,
    load_ready_matches: list[str],
    inventory_matches: list[str],
    log_delta: str,
) -> dict[str, object]:
    excerpt_lines = log_delta.splitlines()[-40:]
    return {
        "build_marker_found": True,
        "load_ready_found": bool(load_ready_matches),
        "load_ready_matches": load_ready_matches,
        "inventory_probe_found": bool(inventory_matches),
        "inventory_probe_matches": inventory_matches,
        "player_log_path": str(_PLAYER_LOG),
        "screenshot_path": str(screenshot_path),
        "log_excerpt": excerpt_lines,
    }


def _raise_build_marker_timeout(timeout: float) -> None:
    msg = (
        f"Timed out after {timeout:.1f}s waiting for '{_BUILD_MARKER}' in {_PLAYER_LOG}. "
        "Check Player.log and verify the mod bootstrapped under Rosetta."
    )
    raise RuntimeError(msg)


def _raise_missing_continue_click_coordinate() -> None:
    msg = "Both --continue-click-x and --continue-click-y are required together."
    raise ValueError(msg)


def _perform_title_navigation(process: subprocess.Popen[bytes], args: argparse.Namespace, title_offset: int) -> None:
    title_ready_matches, _ = _wait_for_title_ready(_PLAYER_LOG, title_offset, args.title_ready_timeout)
    if not title_ready_matches:
        time.sleep(args.title_ready_wait)

    for _ in range(args.menu_up_presses):
        _send_key(process.pid, args.menu_up_key)
        time.sleep(args.menu_navigation_interval)

    for _ in range(args.menu_down_presses):
        _send_key(process.pid, args.menu_down_key)
        time.sleep(args.menu_navigation_interval)


def _continue_from_title(process: subprocess.Popen[bytes], args: argparse.Namespace) -> None:
    if args.continue_click_x is not None or args.continue_click_y is not None:
        if args.continue_click_x is None or args.continue_click_y is None:
            _raise_missing_continue_click_coordinate()
        _click_point(process.pid, args.continue_click_x, args.continue_click_y)
        return

    for index in range(args.continue_presses):
        _send_key(process.pid, args.continue_key)
        if index + 1 < args.continue_presses:
            time.sleep(args.continue_interval)


def _main(argv: list[str] | None = None) -> int:
    args = _parse_args(argv)
    inventory_patterns = tuple(args.inventory_pattern or _DEFAULT_INVENTORY_PATTERNS)
    initial_offset = _current_log_offset(_PLAYER_LOG)
    process: subprocess.Popen[bytes] | None = None
    result: dict[str, object] | None = None

    try:
        _ensure_supported_environment()
        _ensure_unlocked_console()
        _sync_mod(skip_sync=args.skip_sync)
        process = _launch_game()

        build_matches, _ = _wait_for_patterns(
            _PLAYER_LOG,
            initial_offset,
            (_BUILD_MARKER,),
            args.launch_timeout,
        )
        if not build_matches:
            _raise_build_marker_timeout(args.launch_timeout)

        title_offset = _current_log_offset(_PLAYER_LOG)
        _stabilize_game_focus(process.pid)
        _perform_title_navigation(process, args, title_offset)
        _continue_from_title(process, args)

        post_continue_offset = _current_log_offset(_PLAYER_LOG)
        load_ready_matches, _ = _wait_for_load_ready(_PLAYER_LOG, post_continue_offset, args.load_ready_timeout)
        if not load_ready_matches:
            time.sleep(args.load_wait)

        pre_inventory_offset = _current_log_offset(_PLAYER_LOG)
        if args.inventory_key:
            _stabilize_game_focus(process.pid)
            _send_key(process.pid, args.inventory_key)
        inventory_matches, log_delta = _wait_for_patterns(
            _PLAYER_LOG,
            pre_inventory_offset,
            inventory_patterns,
            args.inventory_timeout,
        )
        _stabilize_game_focus(process.pid)
        _capture_screenshot(args.screenshot_path)
        result = _build_result(args.screenshot_path, load_ready_matches, inventory_matches, log_delta)
    except (FileNotFoundError, RuntimeError, subprocess.CalledProcessError, ValueError) as exc:
        print(f"Error: {exc}", file=sys.stderr)  # noqa: T201
        return 1
    finally:
        if process is not None:
            _stop_game(process, args.quit_confirm_key, args.quit_timeout)

    print(json.dumps(result, ensure_ascii=True, indent=2))  # noqa: T201
    return 0


if __name__ == "__main__":
    sys.exit(_main())
