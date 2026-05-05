# CodeRabbit Color And Token Review Rules

This CodeRabbit guideline gives CodeRabbit concrete review rules for markup, color tags,
placeholders, and direct translation markers in QudJP.

It is derived review knowledge only. Do not include raw decompiled strings or
copied game source in comments.

## Token Classes To Preserve

CodeRabbit should treat these as semantic text tokens, not decoration:

| Token class | Examples / shape | Review expectation |
| --- | --- | --- |
| Qud color wrapper | `{{W|text}}`, `{{...|...}}` | Preserve opener/closer balance and intended inner text. |
| Bare Qud color span | `{{W}}`-style span | Preserve unless a route-specific test proves removal. |
| Legacy foreground color | `&G`, `&y` | Preserve single color code and do not confuse with escaped `&&`. |
| Legacy background color | `^r`, `^K` | Preserve single color code and do not confuse with escaped `^^`. |
| Escaped literals | `&&`, `^^` | Keep as escaped literal markers; do not normalize to single symbols. |
| TMP color tags | `<color=#44ff88>`, `</color>` | Preserve opening and closing tags as a balanced pair. |
| Numeric placeholders | `{0}`, `{12:format}` | Preserve the placeholder-index multiset unless a test proves a deliberate route transform. |
| Game variable tokens | `=subject.T=`, `=object.name=` | Preserve, replace only through a route-aware translator, or move to the correct variable-template asset. |
| Direct translation marker | `\x01` internal marker | Do not leak visibly; strip or preserve only according to the downstream route contract. |

## Deterministic Gates

Token and color review is not prompt-only. Use these gates as the source of
truth for asset changes:

```bash
just translation-token-check
just localization-check
python3.12 scripts/check_translation_tokens.py Mods/QudJP/Localization
python3.12 scripts/check_glossary_consistency.py Mods/QudJP/Localization
```

For C# route changes, relevant tests include:

- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorCodePreserverTests.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/Pbt/ColorCodePreserverPropertyTests.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorMarkupTokenizerTests.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorRouteInvariantCases.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorRestoreOwnershipTests.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorRouteCatalogTests.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorTagAllowlistCoverageTests.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/ColorTagStaticAnalysisTests.cs`
- route-specific L2 tests for popup, message log, UI text, description, display
  name, and active effect patches.

## Ownership

Color-aware restoration belongs to `ColorAwareTranslationComposer` and its
tested route catalog. CodeRabbit should flag ad hoc color restoration in random
patches when the shared helper already owns the route.

The review question is not "does the Japanese text look colored?". The review
question is "does the owner route preserve exactly the token contract that the
source route requires?".

## CodeRabbit Should Flag

Flag localization asset changes that:

- change a JSON `key` string to make lookup easier;
- drop a placeholder present in the source key;
- introduce a new placeholder index not present in the source key;
- change the count of Qud/TMP color tags without a route-specific reason;
- convert escaped `&&` or `^^` into unescaped color markers;
- add runtime variable markers to fixed-leaf dictionaries where they belong in
  variable-template assets;
- introduce divergent duplicate source keys across JSON files.

Flag C# changes that:

- strip color markup before owner translation has inspected it;
- restore color tags in a sink-wide fallback instead of an owner translator;
- preserve the direct marker into visible player text;
- forget fallback-to-English behavior for unsupported markup shapes;
- assert only that code runs, without asserting Japanese output and token
  preservation.

## Acceptable Exceptions

A token difference is acceptable only when the PR includes:

1. a route-specific explanation of why the token changes;
2. tests at the correct layer proving the new rendered behavior;
3. updated checker baseline only when the exception is intentionally broader
   than one route and is documented.
