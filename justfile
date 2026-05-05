# QudJP task runner

python := "uv run python"

default:
  just --list

# Build the shipped QudJP assembly.
build:
  dotnet build Mods/QudJP/Assemblies/QudJP.csproj

# Run fast C# L1 tests.
test-l1:
  dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L1

# Run C# L2 tests.
test-l2:
  dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2

# Run C# L2 tests that require the game DLL reference.
test-l2g:
  dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2G

# Run Python static checks.
python-check:
  ruff check scripts/

# Run Python tests.
python-test:
  uv run pytest scripts/tests/

# Run localization asset checks.
localization-check:
  {{python}} scripts/check_encoding.py Mods/QudJP/Localization scripts
  {{python}} scripts/check_glossary_consistency.py Mods/QudJP/Localization
  {{python}} scripts/validate_xml.py Mods/QudJP/Localization --strict --warning-baseline scripts/validate_xml_warning_baseline.json

# Check placeholder and markup-token parity in JSON localization assets.
translation-token-check:
  {{python}} scripts/check_translation_tokens.py Mods/QudJP/Localization

# Require release-note fragments for localization changes.
release-note-check base_ref="origin/main" head_ref="HEAD":
  {{python}} scripts/release_notes.py check-fragment --base-ref "{{base_ref}}" --head-ref "{{head_ref}}"

# Render release and Workshop changenote drafts from unreleased fragments.
render-release-notes version git_hash date:
  {{python}} scripts/release_notes.py render --version "{{version}}" --git-hash "{{git_hash}}" --date "{{date}}" --changelog-output /tmp/qudjp-changelog-entry.md --workshop-output /tmp/qudjp-workshop-changenote.txt

# Build the release ZIP under dist/.
build-release:
  {{python}} scripts/build_release.py

# Spot-check required files in a release ZIP.
release-zip-check release_zip="":
  #!/usr/bin/env bash
  set -euo pipefail
  export QUDJP_RELEASE_ZIP="{{release_zip}}"
  {{python}} - <<'PY'
  import os
  import zipfile
  from pathlib import Path

  requested = os.environ.get("QUDJP_RELEASE_ZIP", "")
  if requested:
      zip_path = Path(requested)
  else:
      release_archives = sorted(
          Path("dist").glob("QudJP-v*.zip"),
          key=lambda path: (path.stat().st_mtime, path.name),
      )
      if not release_archives:
          raise SystemExit("dist/: no QudJP-v*.zip release archive found")
      zip_path = release_archives[-1]

  required = {
      "QudJP/manifest.json",
      "QudJP/preview.png",
      "QudJP/LICENSE",
      "QudJP/NOTICE.md",
      "QudJP/Bootstrap.cs",
      "QudJP/Assemblies/QudJP.dll",
  }
  required_prefixes = {
      "QudJP/Localization/",
      "QudJP/Fonts/",
  }
  with zipfile.ZipFile(zip_path) as zf:
      names = set(zf.namelist())
  missing = sorted(required - names)
  missing_prefixes = sorted(
      prefix for prefix in required_prefixes if not any(name.startswith(prefix) for name in names)
  )
  allowed_exact = {
      *required,
      "QudJP/",
      "QudJP/Assemblies/",
      "QudJP/Localization/",
      "QudJP/Fonts/",
  }
  extra = sorted(
      name for name in names if name not in allowed_exact and not any(name.startswith(prefix) for prefix in required_prefixes)
  )
  if missing or missing_prefixes or extra:
      raise SystemExit(
          f"{zip_path}: missing files={missing}, missing dirs={missing_prefixes}, extra files={extra}"
      )
  print(f"{zip_path}: required release files present")
  PY

# Run the Workshop shipping preflight for an already-tagged release.
workshop-preflight version:
  #!/usr/bin/env bash
  set -euo pipefail
  git status --short --branch
  if [ -n "$(git status --porcelain --untracked-files=all)" ]; then \
    echo "workshop-preflight requires a clean worktree before building release artifacts" >&2; \
    exit 1; \
  fi
  test "$(git rev-list -n1 v{{version}})" = "$(git rev-parse HEAD)"
  just build
  just python-check
  uv run pytest scripts/tests/test_build_release.py scripts/tests/test_build_workshop_upload.py scripts/tests/test_sync_mod.py scripts/tests/test_tokenize_corpus.py -q
  just localization-check
  just translation-token-check
  just build-release
  just release-zip-check dist/QudJP-v{{version}}.zip

# Build Steam Workshop staging and the steamcmd VDF.
build-workshop-upload release_zip="" changenote_file="/tmp/qudjp-workshop-changenote.txt":
  if [ -n "{{release_zip}}" ]; then \
    {{python}} scripts/build_workshop_upload.py --release-zip "{{release_zip}}" --changenote-file "{{changenote_file}}"; \
  else \
    {{python}} scripts/build_workshop_upload.py --changenote-file "{{changenote_file}}"; \
  fi

# Download and verify the GitHub Release ZIP used for Workshop staging.
download-release-zip version:
  #!/usr/bin/env bash
  set -euo pipefail
  version={{quote(version)}}
  if [[ ! "${version}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    printf 'error: version must be X.Y.Z: %q\n' "${version}" >&2
    exit 1
  fi
  tag="v${version}"
  zip_name="QudJP-${tag}.zip"
  checksum_name="${zip_name}.sha256"
  asset_dir="dist/release-assets/${tag}"
  mkdir -p "${asset_dir}"
  gh release download "${tag}" \
    --pattern "${zip_name}" \
    --pattern "${checksum_name}" \
    --dir "${asset_dir}" \
    --clobber
  (cd "${asset_dir}" && shasum -a 256 -c "${checksum_name}")
  printf '%s\n' "${asset_dir}/${zip_name}"

# Sync the built mod into the local game install.
sync-mod:
  {{python}} scripts/sync_mod.py

# Run the broad local verification gate.
check: build test-l1 test-l2 test-l2g python-check python-test localization-check translation-token-check

# Build all repo-local Roslyn tool projects.
roslyn-build:
  dotnet build scripts/tools/AnnalsPatternExtractor/AnnalsPatternExtractor.csproj --configuration Release --no-incremental
  dotnet build scripts/tools/StaticProducerInventoryScanner/StaticProducerInventoryScanner.csproj --configuration Release --no-incremental
  dotnet build scripts/tools/TextConstructionInventory/TextConstructionInventory.csproj --configuration Release --no-incremental

# Run focused tests for repo-local Roslyn tools and Python wrappers.
roslyn-test:
  uv run pytest scripts/tests/test_roslyn_extractor_smoke.py scripts/tests/test_scan_static_producer_inventory.py scripts/tests/test_extract_annals_patterns.py scripts/tests/test_roslyn_text_construction_inventory.py -q

# Run Python static checks for the currently type-clean Roslyn wrapper surface.
roslyn-python-check:
  ruff check scripts/scan_static_producer_inventory.py scripts/extract_annals_patterns.py scripts/tests/test_scan_static_producer_inventory.py scripts/tests/test_extract_annals_patterns.py scripts/tests/test_roslyn_extractor_smoke.py scripts/tests/test_roslyn_text_construction_inventory.py
  uvx basedpyright scripts/scan_static_producer_inventory.py scripts/tests/test_scan_static_producer_inventory.py scripts/tests/test_roslyn_extractor_smoke.py

# Run the non-regenerating local Roslyn verification gate.
roslyn-check: roslyn-build roslyn-test roslyn-python-check

# Preview static producer inventory against decompiled source without touching tracked docs.
static-producer-preview source_root="$HOME/dev/coq-decompiled_stable" output="/tmp/qudjp-static-producer-inventory.json":
  {{python}} scripts/scan_static_producer_inventory.py --source-root "{{source_root}}" --output "{{output}}"

# Intentionally regenerate the tracked static producer inventory artifact.
static-producer-regenerate-tracked source_root="$HOME/dev/coq-decompiled_stable":
  {{python}} scripts/scan_static_producer_inventory.py --source-root "{{source_root}}" --output docs/static-producer-inventory.json

# Preview Annals Roslyn candidates without touching the tracked pending artifact.
annals-pattern-preview source_root="$HOME/dev/coq-decompiled_stable/XRL.Annals" include="Resheph*.cs" output="/tmp/qudjp-annals-candidates.json":
  {{python}} scripts/extract_annals_patterns.py --source-root "{{source_root}}" --include "{{include}}" --output "{{output}}" --force

# Intentionally regenerate the tracked Annals pending candidates artifact.
annals-pattern-extract-tracked source_root="$HOME/dev/coq-decompiled_stable/XRL.Annals" include="Resheph*.cs":
  {{python}} scripts/extract_annals_patterns.py --source-root "{{source_root}}" --include "{{include}}" --output scripts/_artifacts/annals/candidates_pending.json --force

# Preview Roslyn text construction inventory without touching tracked docs.
text-construction-inventory source_root="$HOME/dev/coq-decompiled_stable" output="/tmp/qudjp-text-construction-inventory.json" summary="/tmp/qudjp-text-construction-inventory-summary.md":
  dotnet run --project scripts/tools/TextConstructionInventory/TextConstructionInventory.csproj -- --source-root "{{source_root}}" --output "{{output}}" --summary-output "{{summary}}"

# Verify agent-loop tools and dotfiles script availability.
tool-check:
  bash scripts/agent_cycle.sh tool-check

# Run ast-grep rule tests and scan using sgconfig.yml.
ast-grep-check:
  bash scripts/agent_cycle.sh ast-grep-check

# Run the ast-grep structural-search smoke fixture.
ast-grep-smoke:
  bash scripts/agent_cycle.sh ast-grep-smoke

# Run an ast-grep structural search.
sg lang pattern path=".":
  AST_GREP_PATTERN={{quote(pattern)}} AST_GREP_PATH={{quote(path)}} bash scripts/agent_cycle.sh sg {{quote(lang)}}

# Search C# structure. Defaults to the decompiled game source.
sg-cs pattern path="":
  AST_GREP_PATTERN={{quote(pattern)}} AST_GREP_PATH={{quote(path)}} bash scripts/agent_cycle.sh sg csharp

# Search Python structure.
sg-py pattern path="scripts":
  AST_GREP_PATTERN={{quote(pattern)}} AST_GREP_PATH={{quote(path)}} bash scripts/agent_cycle.sh sg python

# Render skill-eval prompts from this repo's manifest.
render-skill-evals skill="" scenario="":
  bash scripts/agent_cycle.sh render-skill-evals "{{skill}}" "{{scenario}}"

# Summarize recorded skill-eval JSONL results.
summarize-skill-evals results="skill-eval-results.jsonl":
  bash scripts/agent_cycle.sh summarize-skill-evals "{{results}}"

# Show open retrospective entries.
retrospective-open:
  bash scripts/agent_cycle.sh retrospective-open

# Run the local agent feedback loop: tools, ast-grep, skill-eval render, summary, retrospectives.
agent-cycle skill="" scenario="":
  bash scripts/agent_cycle.sh cycle "{{skill}}" "{{scenario}}"
