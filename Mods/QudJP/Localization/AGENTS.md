# Localization/ — XML Translation Files

Translation XML files for Caves of Qud. The game's merge system loads these
at startup and overlays them onto the base game data.

## File Naming

`Category.jp.xml` — category name matches the game's base XML category:

```
Creatures.jp.xml
Conversations.jp.xml
Items.jp.xml
Factions.jp.xml
```

Never use spaces or uppercase in the `.jp.xml` suffix.

## Encoding and Line Endings

- Encoding: **UTF-8 without BOM**
- Line endings: **LF** (Unix-style)

BOM (byte order mark, `EF BB BF`) causes the game's XML parser to fail
silently on some platforms. Always save without BOM.

Verify with:

```bash
file Creatures.jp.xml          # should say "UTF-8 Unicode text"
hexdump -C Creatures.jp.xml | head -1  # first bytes must NOT be ef bb bf
```

## Load Attribute

Most translation files use attribute-level merge:

```xml
<objects Load="Merge">
  <object Name="Snapjaw">
    <part Name="Render" DisplayName="スナップジョー" />
  </object>
</objects>
```

`Load="Merge"` tells the engine to merge by matching `Name` attributes rather
than replacing the entire block. Use this for all creature, item, and faction
translations unless the game's base file uses a different load mode.

## Color Codes — PRESERVE EXACTLY

The game uses three color code formats. All must pass through translation
unchanged:

### Pre-markup shorthand (most common in XML)

```
{{W|text}}    white foreground
{{R|text}}    red foreground
{{G|text}}    green foreground
{{y|text}}    dark gray foreground
```

### Foreground color escape

```
&W            switch to white
&R            switch to red
&G            switch to green
```

### Background color escape

```
^r            red background
^b            blue background
^y            yellow background
```

### Literal escapes

```
&&            literal ampersand
^^            literal caret
```

**Wrong** (breaks rendering):

```xml
<part Name="Render" DisplayName="{{W|スナップジョー" />   <!-- unclosed -->
<part Name="Render" DisplayName="&Wスナップジョー" />     <!-- missing reset -->
```

**Correct**:

```xml
<part Name="Render" DisplayName="{{W|スナップジョー}}" />
```

## Variable Placeholders

The game substitutes `=variable.name=` at runtime. Preserve the format exactly:

```xml
<!-- Source -->
<text>=creature.name= attacks you!</text>

<!-- Translation: keep =creature.name= intact -->
<text>=creature.name=があなたを攻撃した！</text>
```

Never translate, reformat, or add spaces inside `=...=`.

## ID Matching

Blueprint and Conversation IDs must exactly match game version **2.0.4**.
A mismatched ID silently fails — the translation is ignored.

```xml
<!-- Correct: matches game's internal ID -->
<object Name="Snapjaw">

<!-- Wrong: game has no object named "SnapJaw" (case-sensitive) -->
<object Name="SnapJaw">
```

When in doubt, check the game's base XML files for the exact ID string.

## Mojibake Prevention

Never introduce Shift-JIS corruption sequences. Common mojibake patterns
that must NOT appear in any file:

```
繧  縺  驕  蜒  繝  縺ゅ  縺励
```

If you see these characters, the file was saved in Shift-JIS or re-encoded
incorrectly. Discard and re-translate from the source.

## Validation Checklist

Before committing a translation file:

1. UTF-8 without BOM confirmed
2. LF line endings confirmed
3. All color codes balanced (every `{{X|` has a matching `}}`)
4. All `=variable.name=` placeholders preserved verbatim
5. Object/blueprint IDs match game version 2.0.4
6. No mojibake sequences present
7. XML is well-formed (`xmllint --noout file.jp.xml`)
