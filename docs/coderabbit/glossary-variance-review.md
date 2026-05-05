# CodeRabbit Glossary And Translation Variance Review Rules

This CodeRabbit guideline gives CodeRabbit review rules for terminology consistency and
translation variance in QudJP.

It does not contain decompiled source or raw upstream English prose. It points
reviewers to deterministic glossary policy and asset checks.

## Canonical Sources

| Source | Review role |
| --- | --- |
| `docs/glossary.csv` | Canonical player-facing terms when `Status` is `confirmed` or `approved`. |
| `docs/glossary-policy.md` | Policy for status meanings and promotion to `approved`. |
| `scripts/check_glossary_consistency.py` | Deterministic scan for approved/confirmed English residue in visible localized values. |
| `scripts/check_translation_tokens.py` | Deterministic scan for token preservation and duplicate source-key conflicts. |
| `Mods/QudJP/Assemblies/QudJP.Tests/` | Source of truth for runtime translation behavior and route-specific Japanese output. |

Glossary rows with `draft` status are useful review context but are not
authoritative. `confirmed` and `approved` rows should be followed unless fresh
tests or runtime evidence prove that the row is wrong.

## What Counts As A Variance Risk

CodeRabbit should treat these as review risks:

- the same English source key receives different Japanese text in different JSON
  dictionaries without a documented scoped reason;
- a proper noun or fixed term conflicts with a `confirmed` or `approved`
  glossary row;
- a visible localized value retains an approved English term when the glossary
  has a Japanese equivalent;
- a route translator introduces a new Japanese form for an existing term without
  updating `docs/glossary.csv` or explaining why the glossary does not apply;
- a PR changes only an asset leaf for text that the owner matrix classifies as
  dynamic/procedural;
- a reviewer-visible "style improvement" changes particles, word order, or
  verb form in a way that contradicts route tests.

## Deterministic Gates

Use these commands for asset and glossary changes:

```bash
just localization-check
just translation-token-check
python3.12 scripts/check_glossary_consistency.py Mods/QudJP/Localization
python3.12 scripts/check_translation_tokens.py Mods/QudJP/Localization
```

For runtime C# changes, require the relevant `just test-l1`, `just test-l2`, and
when target resolution changed, `just test-l2g`.

## Visible Text Scope

The glossary checker covers visible XML attributes and JSON dictionary entries
that QudJP treats as player-facing localization assets. It intentionally ignores
implementation-only identifiers and route metadata.

CodeRabbit should not demand glossary consistency for hidden IDs, route names,
test fixture names, or internal diagnostic fields unless those values are shown
to players.

## Review Rules For New Terms

When a PR adds or changes a player-facing term:

1. Check `docs/glossary.csv` first.
2. If a `confirmed` or `approved` row exists, use that Japanese form or cite the
   fresh evidence that supersedes it.
3. If no row exists and the term is stable/player-facing, ask for a glossary row
   when the term is likely to recur.
4. If the term is route-specific or procedural, require a route translator test
   instead of broad glossary enforcement.

## CodeRabbit Should Flag

Flag changes that:

- bypass `docs/glossary.csv` for proper nouns and stable fixed terminology;
- add broad duplicate dictionary entries instead of a scoped route dictionary;
- hide English residue by deleting tokens or source fragments;
- update checker baselines without explaining the accepted variance;
- claim terminology consistency without running glossary/token gates;
- alter existing Japanese grammar in runtime templates without tests proving
  the assembled sentence.

## Acceptable Variance

Different Japanese forms can be acceptable when the difference is route-owned
and semantic, for example UI noun labels versus full message sentences. The PR
should document the route distinction and include tests or asset evidence that
make the distinction reviewable.
