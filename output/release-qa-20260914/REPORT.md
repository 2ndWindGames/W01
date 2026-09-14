# VioletTap 3.7.1 release QA — 2026-09-14

## Release artifact

- Android App Bundle: `../../Builds/Android/violettap-3.7.1.aab`
- Final rebuild finished 22:40 KST with 0 errors and 1 Unity warning (the bundled Google Mobile Ads iOS framework is inapplicable to Android).
- Size: 110,048,539 bytes (about 104.9 MiB). SHA-256: `ABA07A01145D2444922825099A42E122B832FD8D0DBDFE41E517B0404EEA7826`.
- `bundletool validate` passed; `jarsigner -verify` reported `jar verified` (with the existing signing key's legacy-algorithm warning).
- `bundletool build-apks` produced an 89,683,932-byte universal APK from the final AAB; its APK-set CRC check passed. The unsigned QA artifact was removed after inspection, since it is unsuitable for installation or upload.
- This is comfortably below Google Play's current 500 MB compressed base-module limit and 200 MB mobile-data notice threshold; Play Console will calculate the actual per-device download size after upload.
- Manifest: `com.secondwindgames.violettap`, version `3.7.1` / code `11`, minimum API 25, target API 36. All six Arm64 native libraries have 16 KiB ELF load-segment alignment, and the bundle config specifies `PAGE_ALIGNMENT_16K`.
- The approved opaque icon concept is assigned to Unity's default and Android icons. The AAB's launcher PNGs contain the new art at all six Android densities.
- Both 48px mdpi and 192px xxxhdpi launcher exports were visually checked; the prism and neon ring remain identifiable at launcher size.
- Size audit: two unused icon concepts remain under `Assets/Resources/Icon`, so Unity packs them into the AAB (about 5.7 MiB compressed together). They are unreferenced by the current icon settings. Move them outside `Resources` in a later size-optimization build; the release candidate was kept stable during final QA.

## Automated and visual checks

- Latest full gameplay suite: 91 PASS, 0 FAIL (`../qa-2026-09-13/run-20260914-224225/results.txt`). The final result panel is readable after its entrance animation; `result-settled.png` records that state.
- Focused suites: 1,733 additional PASS, 0 FAIL, covering purchase UI callbacks, ranking layout/scrolling, settings and ranking popups, combo/fever scoring, pointer reuse, English/Korean help layout, music and mute, captions, HUD, modal and nickname lifecycle, 30 repeated rounds, scene transitions, pause/resume, status, feedback, signal phases, delayed nickname callbacks, and fresh muted startup. The latest signal-phase suite is 81 PASS, 0 FAIL (`../qa-2026-09-13/run-20260914-223305/results.txt`) and includes a cramped-screen fallback check. Other detailed results are in `../qa-2026-09-13/run-20260914-215*/results.txt` and `run-20260914-2213*/results.txt`.
- Fixed during QA: an intermittent same-frame target raycast, overlapping long ranking labels, a missing conditional privacy-options control, a numbered-burst fallback that could leave no playable targets on an extremely cramped field, and Windows Android build path overflow from Gradle/Ninja cache.
- One incremental rebuild logged a Google Mobile Ads placeholder-prefab save error in an immutable package directory. The immediate repeat completed with 0 errors; the transient messages are retained in `transient-incremental-build-messages.txt`.
- Store media: `../play-store-3.7.1-upload.zip` contains 12 validated images (512px icon, 1024 × 500 feature graphic, five 1080 × 1920 real Game View captures each for English and Korean). ZIP CRC check passed; the individual dimensions and color modes passed Google Play checks.

## Before live release

- No Android device or emulator is connected, and the user chose to omit this release's device test. Runtime startup, performance, advertisements, live consent, Google Play Billing, and leaderboard service responses are therefore unverified on hardware. Editor purchase QA exercised simulated local callbacks only; it did not grant real entitlements.
- The merged manifest contains the generic `FOREGROUND_SERVICE` permission and `androidx.work.impl.foreground.SystemForegroundService` from transitive AndroidX WorkManager 2.7.0. The game has no known foreground-service feature. Check the Play Console App content screen for a permission declaration request when uploading the AAB. The automated checks cannot confirm Play review outcome.
- A library-manifest removal rule was trial-built; Gradle still merged the permission and service, so that experiment was reverted. The canonical AAB is the verified 22:40 build. The trial report is retained as `foreground-manifest-trial-build-report.txt`.
- Store phone screenshots came from Unity Editor Game View at phone resolution. The layout has not been cross-checked on a physical Android phone for this release.

Google Play references: [preview image requirements](https://support.google.com/googleplay/android-developer/answer/9866151), [app size limits](https://support.google.com/googleplay/android-developer/answer/9859372), [foreground service declarations](https://support.google.com/googleplay/android-developer/answer/13392821).
