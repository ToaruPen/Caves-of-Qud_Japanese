# 2026-04-29 Issue 376 Runtime Final-Smoke Evidence

## RunID

`20260428-2325-issue376`

## Conclusion

**Go/No-Go: No-Go for Workshop readiness.**

The requested `final-smoke` automation could not run because the macOS console
session was locked. Exact checker failure:

```text
Error: macOS console session is locked. Unlock the Mac before running translation_checker.py.
```

After the blocked checker attempt, a boot-only Rosetta fallback produced a fresh
`Player.log` with QudJP enabled and the expected QudJP runtime markers. It did
not load a save, capture screenshots, drive combat, or exercise
death/display-name routes, so treat it as fresh QudJP-enabled boot evidence
only.

## Evidence Root

All local evidence for this run is under:

```text
.sisyphus/evidence/issue-376-runtime-final-smoke-20260428T232521Z/
```

Key files:

- `worktree-status.txt` -- branch `codex/issue-376-runtime-final-smoke`, HEAD `bf7e859c89eba76bc8f14b1130537ef6a2f1d4b0`
- `console-lock-state.txt` -- `true`
- `prerequisites-tools.txt` -- Rosetta and required tool path checks
- `game-paths.txt` -- default Steam game binary and `Assembly-CSharp.dll` paths present
- `dotnet-build.txt` / `.exit.txt` -- build evidence
- `sync-mod-dry-run.txt` / `.exit.txt` -- dry-run sync evidence
- `sync-mod-real.txt` / `.exit.txt` -- real sync evidence
- `translation-checker-final-smoke.txt` / `.exit.txt` -- blocked final-smoke run
- `direct-rosetta-launch.stdout.txt`, `.stderr.txt`, `.exit.txt` -- boot-only Rosetta fallback
- `runtime-logs-after-direct/Player.log` -- copied fresh boot-only runtime log
- `player-log-marker-counts-after-direct.txt` -- QudJP marker/probe counts
- `runtime-triage-after-direct.json` -- fresh boot-only triage output

## Preconditions / Environment

- Time: `2026-04-28T23:25:21Z` / `2026-04-29T08:25:21+0900`
- Worktree: `/Users/toarupen/Dev/coq-japanese_stable/.worktrees/issue-376-runtime-final-smoke`
- Branch: `codex/issue-376-runtime-final-smoke`
- Rosetta check: `arch -x86_64 /usr/bin/true` exited `0`
- Game binary: `$HOME/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/MacOS/CoQ` present and executable
- Console session: blocked, `IOConsoleLocked=true`
- Save roots discovered: two directories under `$HOME/Library/Application Support/com.FreeholdGames.CavesOfQud/Synced/Saves`
- Freehold `ModSettings.json`: not present at `$HOME/Library/Application Support/Freehold Games/CavesOfQud/Local/ModSettings.json`

## Build and Sync

| Command | Result |
| --- | --- |
| `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` | exit `0`; build succeeded with `0` warnings and `0` errors |
| `python3.12 scripts/sync_mod.py --dry-run` | exit `0`; `Transfer starting: 124 files` |
| `python3.12 scripts/sync_mod.py` | exit `0`; synced the same 124-file game-essential set |

The dry-run listed only the expected shipped mod assets: `manifest.json`,
`preview.png`, `Bootstrap.cs`, `Assemblies/QudJP.dll`, localization assets, and
fonts. No source `.cs` files outside `Bootstrap.cs` were deployed.

## Runtime Attempt

Exact command:

```bash
python3.12 scripts/translation_checker.py \
  --skip-sync \
  --flow final-smoke \
  --input-backend osascript \
  --flow-screenshot-dir .sisyphus/evidence/issue-376-runtime-final-smoke-20260428T232521Z/final-smoke \
  --attack-sequence ctrl+numpad6 \
  --attack-sequence backslash,right \
  --death-attack-count 30 \
  --require-combat-evidence
```

Result:

- exit: `1`
- blocker: locked macOS console session
- screenshots: none; the checker stopped before launch/input, so
  `final-smoke/` contains no screenshot files
- combat/death/display-name evidence: not captured

## Boot-Only Rosetta Fallback

To capture fresh runtime state, `scripts/launch_rosetta.sh` was run directly for
45 seconds and then terminated with SIGTERM.

- launcher exit: `143`, expected from the scripted termination window
- `Player.log` before fallback: `Apr 29 00:12:50 2026`, size `29567`
- `Player.log` after fallback: `Apr 29 08:28:20 2026`, size `30697`
- copied log: `.sisyphus/evidence/issue-376-runtime-final-smoke-20260428T232521Z/runtime-logs-after-direct/Player.log`

Fresh marker counts after the fallback:

```text
[QudJP] Build marker=1
Enabled mods=1
Caves of Qud 日本語化=1
DynamicTextProbe=18
SinkObserve=2
FinalOutputProbe=12
missing key=0
MODWARN=0
mprotect=0
ERROR=0
LLMOfQud=0
```

Fresh boot-only triage:

```json
{
  "total": 0,
  "static_leaf": 0,
  "route_patch": 0,
  "logic_required": 0,
  "preserved_english": 0,
  "unexpected_translation_of_preserved_token": 0,
  "unresolved": 0
}
```

The triage report also contains 23 Phase F entries:

- `dynamic_text_probe`: 12
- `sink_observe`: 1
- `final_output_probe`: 10

## Verification Commands

| Command | Result |
| --- | --- |
| `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L1` | exit `0`; `1646` passed |
| `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2` | exit `0`; `766` passed |
| `uv run pytest scripts/tests/test_triage_log_parser.py scripts/tests/test_triage_models.py scripts/tests/test_triage_classifier.py scripts/tests/test_triage_integration.py -q` | exit `0`; `76` passed |
| `uv run pytest scripts/tests/test_triage_integration.py -q -k sample_log_smoke` | exit `0`; `1` passed, `12` deselected |
| `uv run pytest scripts/tests/test_translation_checker.py -q` | exit `0`; `51` passed |
| `ruff check scripts/` | exit `0`; all checks passed |
| `python3.12 scripts/check_encoding.py Mods/QudJP/Localization scripts` | exit `0`; `293` files OK |
| `python3.12 scripts/validate_xml.py Mods/QudJP/Localization --strict --warning-baseline scripts/validate_xml_warning_baseline.json` | exit `0`; passed with the established warning baseline |

The L2 test run emitted a transient copy retry warning because L1 and L2 were
running concurrently against the same test output directory. The command still
exited `0` and all selected L2 tests passed.

## Issue Disposition

| Issue | Disposition |
| --- | --- |
| #376 display-name/color-tag runtime evidence | **Do not close.** Static/non-GUI gates are already complete, but this run captured no death popup, combat death, screenshot, or display-name color-tag route evidence. |
| #363 runtime untranslated triage | Keep open. Boot-only triage is clean (`summary.total=0`), but the requested save/combat final-smoke runtime path did not run. |
| #400 release roll-up | Still No-Go. QudJP-enabled boot evidence exists, but Workshop readiness remains blocked on unlocked-console final-smoke with screenshots and combat/death evidence. |

## Exact Blocker

The machine must be unlocked before `translation_checker.py` can proceed.

Observed blocker evidence:

- `console-lock-state.txt`: `true`
- `translation-checker-final-smoke.exit.txt`: `1`
- `translation-checker-final-smoke.txt`:

```text
Error: macOS console session is locked. Unlock the Mac before running translation_checker.py.
```

## Next Required Steps

- Unlock the macOS console session.
- Re-run the same `translation_checker.py --flow final-smoke --input-backend osascript --require-combat-evidence` command from this report.
- Run `combat-smoke` only if final-smoke launches but does not produce combat,
  death, or display-name evidence.
