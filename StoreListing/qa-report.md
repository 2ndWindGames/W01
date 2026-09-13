# QA Report

## Scope

- Korean/English runtime UI localization
- Font coverage for Korean TMP text
- Intro, game HUD, result, sound, ranking, and nickname flows
- Ranking submission and current-player presentation
- Android identity and store-delivery prerequisites
- Store listing image dimensions and localization

## Issues found and fixed

1. Game popup initialization could execute twice and register duplicate button/event handlers. Initialization is now idempotent.
2. A new personal best could be submitted once as `NONAME` before the nickname prompt and then submitted again with the chosen nickname. Submission now occurs once, after confirmation or with the `NONAME` fallback.
3. Korean runtime TMP labels had no guaranteed Hangul font. A bundled Noto Sans KR font and centralized font application were added.
4. Dynamic gameplay and ranking labels contained English-only strings. Active user-facing strings now select Korean or English from the device language.
5. Ranking entries without nickname metadata exposed a raw player ID. The fallback is now `NONAME`, and loading, empty, and unavailable states are localized.
6. IAP failures were Korean-only and were not visible to the player. Store/product/purchase failures now use the active language and appear temporarily in the game status area.
7. Fever could trigger only once per round and had limited feedback. Fever can now be earned again after rebuilding a streak and includes a dedicated score multiplier, faster/more animated targets, duration bonuses, screen pulse, BGM acceleration, and synthesized entry/exit cues.

## Verification

- C# compiler pass for project response files
- Search for remaining active user-facing literals in modified UI/game scripts
- Required scene/resource/font/audio and Android app-name checks
- Feature graphics checked at the required 1024 × 500 size in both locales
- Earlier 1080 × 1920 promotional compositions were identified as non-gameplay captures and moved to `StoreListing/mockups`; they are intentionally excluded from upload-ready screenshot folders
- Latest available Unity Android build log reviewed separately; a fresh signed Play build should still be run from the Unity Editor after asset import

## Manual device matrix still recommended

- Korean device locale and English device locale
- First launch and returning player
- Sound/BGM toggles across scene changes
- Normal, quick, time-bonus, danger, Fever, pause/end-of-round behavior
- Nickname keyboard input, empty input fallback, and own-rank highlight
- Offline/network failure for leaderboard and IAP
- Ad-supported and remove-ads purchase/restore states
- Multiple Android aspect ratios and display cutouts
