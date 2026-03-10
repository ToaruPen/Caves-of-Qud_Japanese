# Learnings

## 2026-03-10 Session Start
- GitHub repo created: ToaruPen/Caves-of-Qud_Japanese (public)
- Default branch: main
- Protocol: SSH
- Initial commit: f26f8d7

## 2026-03-11 Task 0 PoC
- macOS + dotnet SDK 10.0.100 では `net10.0` の `dotnet test` が安定して実行可能。
- `Assembly-CSharp.dll` 参照で `ConsoleLib.Console.Markup` のメソッド署名取得が可能。
- ゲーム同梱 `0Harmony.dll` は `Version=2.2.2.0` を確認。
- Unity依存型のサンプル実体化では `TypeInitializationException` を再現できず（観測値は `False`）。
