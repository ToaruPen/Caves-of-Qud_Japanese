# Markov Corpus Translation Task

## What to do
1. Read `scripts/corpus_en_for_translation.json` — 9,371 English sentences as `[{id, en}]`
2. Read `scripts/translation_glossary.txt` — mandatory term mappings
3. Translate every sentence to Japanese
4. Write output to `scripts/corpus_ja_translated.json` as `[{id, ja}]`

## Mandatory Glossary (from translation_glossary.txt)
Use these EXACT Japanese terms whenever the English term appears:
- Eaters = 喰らう者
- Sultan/Sultans = スルタン
- Chrome = クローム
- Resheph = レシェフ
- Spindle/The Spindle = スピンドル
- Joppa = ジョッパ
- Golgotha = ゴルゴタ
- Barathrum = バラサラム
- Chavvah = チャヴァ
- Qud = クッド
- Baetyls = ベテル
- salt = 塩 (when referring to the lore substance)
- The Six Day Stilt = 六日のスティルト
- Chrome Pyramid = クローム・ピラミッド
- Tomb of the Eaters = 喰らう者の墓所

## Translation Rules
1. Match the original register — literary stays literary, scientific stays analytical, aphorisms stay pithy
2. Each English sentence = one Japanese sentence (do NOT merge or split)
3. End sentences with ASCII period `.` NOT `。`
4. Transliterate proper names to katakana (use ・ for multi-part names)
5. Keep Qud lore concepts faithful — do NOT domesticate to real-world equivalents
6. Output ONLY the JSON array `[{id, ja}]`, no commentary
7. Process ALL sentences — do not skip any
