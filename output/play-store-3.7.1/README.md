# VioletTap 3.7.1 — Google Play images

Upload the contents of `../play-store-3.7.1-upload.zip` to **Play Console → Grow users → Store presence → Main store listing → Graphics**. The archive contains only uploadable images, arranged by locale.

| Play Console field | File | Size |
| --- | --- | --- |
| App icon | `icon-512.png` | 512 × 512, 32-bit PNG, fully opaque |
| Feature graphic | `feature-1024x500.png` | 1024 × 500, 24-bit PNG |
| Phone screenshots, English | `en-US/phone-01-*.png` through `phone-05-*.png` | 1080 × 1920, 24-bit PNG |
| Phone screenshots, Korean | `ko-KR/phone-01-*.png` through `phone-05-*.png` | 1080 × 1920, 24-bit PNG |

Screenshot order: Fever gameplay, 50-combo gameplay, neon targets, intro, help guide. The icon is the approved opaque concept 1 and matches the Android launcher icon in the 3.7.1 AAB. The feature graphic is original promotional artwork based on the same neon target palette. Phone screenshots are cropped from actual Unity Game View QA captures in the current English and Korean UI; no UI text or gameplay content was composited into them.

All 12 upload images have been checked for their dimensions, color mode, and Google Play file requirements by `export_assets.py`. Source captures and `feature-source.png` are retained for provenance and are not in the upload ZIP.

**Release note:** the phone images reflect Unity Editor Game View at phone resolution. Physical Android verification was omitted for this release at the user's request, so device-specific layout and rendering remain unverified. Only phone assets are included because this build's store listing is for the phone experience. Add tablet or TV assets only if those device types are intentionally supported and tested.

Google Play image specifications: https://support.google.com/googleplay/android-developer/answer/9866151
