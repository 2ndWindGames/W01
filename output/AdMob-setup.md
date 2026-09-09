# Android AdMob integration

- Google Mobile Ads Unity SDK 11.5.0 via the OpenUPM registry; Unity resolves native dependencies through EDM4U.
- App ID: `ca-app-pub-3765914942296716~5027638921` in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`.
- Banner: `ca-app-pub-3765914942296716/2410098921`.
- Interstitial: `ca-app-pub-3765914942296716/4041676531`.
- Editor and Development Builds use Google's test ad units. Non-development Android builds use the provided live units. iOS ads are not enabled.

Completed rounds accumulate across scene changes, but reset when the app process restarts. At round completion, show only if at least 3 rounds have accumulated and 180 seconds have passed since the previous interstitial opened. Only the opened callback resets the counter/timestamp. Missing ads skip display and retain eligibility; background loading never opens an ad during play.

A standard 320x50 dp banner is created at the bottom in GameScene. The camera viewport reserves its height plus the bottom safe-area inset so camera-space UI is above it. Scene exit destroys the banner. Loading failures retry with a delay. Interstitials hide the banner temporarily and pause audio; game start/retry/back are blocked while the interstitial is opening/displayed.

UMP updates consent before SDK initialization and presents a form if required. Configure messages in AdMob Privacy & messaging for relevant regions. The settings popup exposes an ads privacy button when UMP requires it.

Validation: C# compilation against the actual SDK, plus `output/Test-AdSchedule.ps1` for round count and cooldown boundaries. Android device ad display, native Gradle build, callback behavior, and banner layout still need device validation. Test with a Development Build before producing a release.

Official references: https://developers.google.com/admob/unity/quick-start and https://developers.google.com/admob/unity/privacy
