# Issues

## 2026-03-11 Task 0 PoC
- `net9.0`/`net8.0` テスト実行はランタイム未導入で失敗（`Microsoft.NETCore.App 9/8` 不足）。
- `net48` テスト実行は `mono` ホスト未導入で失敗（ビルドのみ成功）。
- 既存 `Player.log` に他Mod由来の `mprotect returned EACCES` を確認し、macOS Harmony ランタイム失敗リスクは高い。
