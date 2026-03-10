# Caves of Qud Japanese Localization (QudJP)

Caves of Qud の会話・UI・自動生成テキストを日本語化し、CJK フォントを同梱する Mod です。

## Status

🚧 Under active development — not yet playable.

## Build

### C# Mod DLL

```bash
dotnet build Mods/QudJP/Assemblies/QudJP.csproj
```

### Python Tools

```bash
ruff check scripts/
pytest scripts/tests/
```

## Install

1. Build `QudJP.dll`
2. Copy `Mods/QudJP/` to your Caves of Qud mods directory
3. Enable "Caves of Qud 日本語化" in the mod manager

## License

TBD
