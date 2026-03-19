"""Tests for the verify_inventory module."""

import subprocess
from pathlib import Path

import pytest

from scripts.verify_inventory import (
    _build_activate_application_script,
    _build_focus_script,
    _build_hammerspoon_focus_lua,
    _escape_osascript_string,
    _find_matching_patterns,
    _io_console_locked_from_root,
    _key_code_for,
    _load_ready_matches,
    _modifier_mask,
    _parse_args,
    _parse_hammerspoon_focus_output,
    _read_log_delta,
    _stabilize_game_focus,
)


class TestInputMappings:
    def test_maps_special_key_codes(self) -> None:
        assert _key_code_for("return") == 36
        assert _key_code_for("up") == 126

    def test_maps_character_key_codes(self) -> None:
        assert _key_code_for("b") == 11
        assert _key_code_for("h") == 4
        assert _key_code_for("q") == 12
        assert _key_code_for("x") == 7
        assert _key_code_for(" ") == 49

    def test_rejects_unknown_modifier(self) -> None:
        with pytest.raises(ValueError, match="Unsupported modifier"):
            _modifier_mask(("hyper",))

    def test_builds_modifier_mask(self) -> None:
        assert _modifier_mask(("command", "shift")) > 0

    def test_builds_focus_only_script(self) -> None:
        script = _build_focus_script(55)
        assert "application process whose unix id is 55" in script
        assert "frontmost" in script

    def test_builds_activate_application_script(self) -> None:
        script = _build_activate_application_script("com.example.Game")
        assert script == 'tell application id "com.example.Game" to activate'


class TestFindMatchingPatterns:
    def test_returns_only_present_patterns(self) -> None:
        text = "alpha beta gamma"
        matches = _find_matching_patterns(text, ("beta", "delta", "alpha"))
        assert matches == ["beta", "alpha"]


class TestHammerspoonHelpers:
    def test_escapes_osascript_text(self) -> None:
        assert _escape_osascript_string('a"b\\c') == 'a\\"b\\\\c'

    def test_builds_hammerspoon_focus_lua_with_pid_and_path(self, tmp_path: Path) -> None:
        lua = _build_hammerspoon_focus_lua(123, tmp_path / "out.txt")
        assert "app:pid() == 123" in lua
        assert "out.txt" in lua

    def test_parses_hammerspoon_focus_output(self) -> None:
        parsed = _parse_hammerspoon_focus_output("found= true\nfront= CoQ\nfocused_title= CavesOfQud\n")
        assert parsed == {"found": "true", "front": "CoQ", "focused_title": "CavesOfQud"}

    def test_stabilize_game_focus_falls_back_to_activate_when_hammerspoon_command_fails(
        self,
        monkeypatch: pytest.MonkeyPatch,
    ) -> None:
        calls: list[str] = []

        monkeypatch.setattr(
            "scripts.verify_inventory._locate_hammerspoon_app",
            lambda: Path("/Applications/Hammerspoon.app"),
        )

        def fail_with_called_process_error(_pid: int) -> None:
            raise subprocess.CalledProcessError(returncode=1, cmd=["osascript"], stderr="denied")

        def record_activate() -> None:
            calls.append("activate")

        monkeypatch.setattr("scripts.verify_inventory._focus_process_with_hammerspoon", fail_with_called_process_error)
        monkeypatch.setattr("scripts.verify_inventory._activate_game_application", record_activate)
        monkeypatch.setattr("scripts.verify_inventory._focus_process", lambda pid: calls.append(f"focus:{pid}"))

        _stabilize_game_focus(123)

        assert calls == ["activate"]


class TestConsoleLockHelpers:
    def test_detects_locked_console(self) -> None:
        assert _io_console_locked_from_root({"IOConsoleLocked": True}) is True

    def test_detects_unlocked_console(self) -> None:
        assert _io_console_locked_from_root({"IOConsoleLocked": False}) is False


class TestReadLogDelta:
    def test_reads_only_bytes_after_offset(self, tmp_path: Path) -> None:
        log_path = tmp_path / "Player.log"
        log_path.write_text("line1\nline2\nline3\n", encoding="utf-8")
        offset = len(b"line1\n")
        assert _read_log_delta(log_path, offset) == "line2\nline3\n"

    def test_resets_to_start_when_file_rotates(self, tmp_path: Path) -> None:
        log_path = tmp_path / "Player.log"
        log_path.write_text("new\n", encoding="utf-8")
        assert _read_log_delta(log_path, 999) == "new\n"


class TestLoadReadyMatches:
    def test_detects_bottom_context_probe(self) -> None:
        text = "[QudJP] QudMenuBottomContextProbe/RefreshButtonsAfter/v1: buttons=2"
        assert _load_ready_matches(text) == ["[QudJP] QudMenuBottomContextProbe/RefreshButtonsAfter/v1:"]

    def test_detects_non_main_menu_translator_context(self) -> None:
        text = "[QudJP] Translator: missing key 'x' (context: UITextSkinTranslationPatch)"
        assert _load_ready_matches(text) == ["[QudJP] Translator:<non-main-menu>"]

    def test_ignores_main_menu_translator_context(self) -> None:
        text = "[QudJP] Translator: missing key 'x' (context: MainMenuLocalizationPatch)"
        assert _load_ready_matches(text) == []


class TestParseArgs:
    def test_defaults_match_observed_title_menu_flow(self) -> None:
        args = _parse_args([])
        assert args.menu_up_presses == 0
        assert args.menu_down_presses == 1
        assert args.menu_up_key == "up"
        assert args.menu_down_key == "down"
        assert args.continue_key == "space"
        assert args.continue_presses == 2
        assert args.continue_click_x is None
        assert args.continue_click_y is None
        assert args.load_ready_timeout == 45.0
        assert args.title_ready_wait == 12.0
