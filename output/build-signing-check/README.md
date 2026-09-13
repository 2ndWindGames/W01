# Android signing recovery — 2026-09-13

The failed W01 build ran from 21:05:42 to 21:11:00 KST. It reached `:launcher:signReleaseBundle`, then failed to read the `secondwindgames` private key from `user.keystore` with `Get Key failed: Given final block not properly padded`.

The read-only Java check used only the current failed build's configured store/key passwords. It confirmed that the store password was valid, the alias existed, the configured key password failed, and the already-configured store password also correctly unlocked the private key. No password values or private-key bytes were printed or saved by the diagnostic scripts.

Corrected Unity's in-memory key password to that verified store password, retaining the original keystore, alias, application identifier, release configuration, and version. The temporary Editor recovery hook has been removed from Assets; its source is retained here for audit. No game source change was needed for this build failure.

## Result

- Build: succeeded at 21:22:11 KST; 0 errors, 3 warnings.
- Output: `C:/SecondWindGames/Repositoires/Unity/W01/violettap.aab`.
- Size: 114,633,772 bytes.
- Package: `com.secondwindgames.violettap`.
- Version: `3.5`, version code `8`.
- SHA-256: `22659F93E33216FDD311BD46F39ACB8D848BDC267F370BF50F62F19A4517E41C`.
- Bundletool format validation passed; manifest was read from the finished AAB.
- Jarsigner reported `jar verified`; warnings are preserved in `bundle-signature-verification.txt`, including the existing self-signed certificate and JAR streaming-order observations. The Android bundle validation passed without rewriting the signed archive.
- Signer certificate SHA-256 matches the previous AAB. The tracked keystore is unchanged.
- A post-build check confirms the currently generated store password and key password both work.

The previous output was preserved as `violettap-before-signing-fix.aab`. No upload or publication was performed, and scheduled QA remains paused.
