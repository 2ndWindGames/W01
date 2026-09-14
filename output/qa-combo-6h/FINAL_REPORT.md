# Six-hour Editor QA result

- Source: soak-status.json and sessions.log, inspected 2026-09-14 09:02 KST.
- State: COMPLETED_6H; completed at 2026-09-14 05:58:52 KST.
- Verified full-speed execution: 21600.139973 seconds (6 hours). Original recorded time: 21660.139973 seconds; 60 seconds conservatively excluded for legacy startup accounting. Maintenance/compilation wall time was not added.
- Rounds: 484. EventSystem target contacts: 52,483. Assertions: 54,106. Recorded frames: 1,295,477. Runner error field: empty.
- During QA, Ready text overlap and a speed notice persisting over the Result title were observed and fixed. Focused recheck passed 88 assertions before resuming. Details and before/after evidence are in WORKLOG.md.
- Tested scope: local Unity Editor, Korean/English gameplay, combo and fever transitions, deliberately missed targets, pacing, pauses, target/UI cleanup, ranking text bounds with local data. No purchases or leaderboard submissions were made.
- This is not a guarantee of bug-free gameplay or Android validation. Physical Android performance, haptic feel, audio playback and store-connected purchases were not verified. The release AAB was not rebuilt with these changes.

## Subsequent user-reported Editor error

- At 08:55:08 KST the W01 Console showed `[Package Manager Window] Operation cancelled` (one error).
- overnight-editor.log records Package Manager exit code 3221226505 followed by automatic restart and successful IPC reconnection. At inspection, its replacement process was running with the W01 Editor as parent (started 08:55:07 KST).
- The native process's underlying crash cause is not established. There is no evidence here that the combo/gameplay code caused it.
- A fresh `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors and 0 warnings. No game code, package version, lock file or cache was changed for this report.
- Package Manager window re-query was blocked by an unrelated r01.exe Windows network-access security dialog. No security setting was changed and no permission button was pressed. The user must handle that dialog before UI verification can continue.
- The six-hour result predates this Package Manager error; it does not certify the later Editor operation.
