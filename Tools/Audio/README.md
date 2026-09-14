# VioletTap audio

Original synth music and sound effects, generated from code with NumPy. No sampled recordings or external audio libraries are included. The existing five resource paths and Unity GUIDs are retained; thirteen new effect paths extend the set (3 BGM and 15 SFX in total).

## Music

| Resource | Tempo | Length | Arrangement |
| --- | --- | --- | --- |
| BGM/Intro_NeonAwakening | 104 BPM | 8 bars | Glass melody, soft pad, sparse low pulse |
| BGM/Game_NeonLobby | 120 BPM | 8 bars | Syncopated bass, restrained backbeat, answering plucks |
| BGM/Gameplay_NeonRush | 140 BPM | 16 bars | Four-on-the-floor drums, melodic hook, two-bar breakdown, fills |

All tracks share an A minor / Fmaj7 / Cmaj9 / G6 palette. Notes, releases and tempo-synced delays wrap around the loop boundary. Music playback crossfades over 0.28 seconds; requesting the currently playing track preserves its position. Fever uses a separate musical cue without detuning or speeding up the score.

## Effects

- Button_Click: small glass confirmation.
- Target_NeonTap: immediate midrange impact and short percussive pluck.
- Target_Quick: two-note bright chirp.
- Target_TimeBonus: ascending reward triplet.
- Target_Bomb: descending impact with midrange crunch for phone speakers.
- Fever_Tap / Fever_Quick / Fever_TimeBonus: punchy, brighter variants for each scoring target during fever.
- Target_Miss: quiet downward cue, only when a streak is lost.
- Fever_Start / Fever_End: rising arpeggio and falling release.
- Round_Start / Round_Complete / New_Best: opening pickup, resolution and record fanfare.
- Timer_Tick: restrained final-five-second cue.

Effects have independent voices so a later tap does not alter an earlier cue's gain or pitch. The pool is capped at 12 effects. Mute and stop apply to every voice, including fever and both sides of a music crossfade.

## Rebuild and validate

```powershell
python Tools/Audio/generate_violettap_audio.py --report output/qa-2026-09-13/audio-manifest.json
python Tools/Audio/validate_violettap_audio.py
```

Output is stereo PCM16 at 44.1 kHz. Unity imports BGM as streaming Vorbis and short effects as preloaded, decompressed PCM. GameScene preloads the tap cues before the first target. The validator checks resource references, peaks, DC, loop transitions, faded effect edges and mono compatibility. It also creates a gameplay mix preview at almost seven taps per second. Signal checks do not replace a listening pass on phone speakers and headphones.

In Unity, run **Tools > VioletTap > QA > Run regression suite** for import, voice overlap, mute, crossfade, gameplay state and resource-lifecycle checks. Reports and screenshots go to `output/qa-2026-09-13/run-*`. The suite restores the saved best score and audio preferences; it does not make purchases or submit test scores.

## Touch feedback and the in-game guide

Targets respond on pointer down, with separate cues for normal, quick, time bonus, bomb and fever variants. A bounded pool of ten short echo/spark bursts reinforces each contact. Gameplay music gain is 0.32, scoring taps 0.66 and bombs 0.75; the fever entry cue is 0.40 so it leaves space for subsequent taps.

Open **HELP / 도움말** in the upper right of the Game screen. The guide uses the actual target sprites and current GameConfig values, and pauses round time, target expiry and fever until closed. Tap a card to preview its audio/haptic cue without scoring. SOUND and VIBRATION buttons persist their individual preferences; the existing menu SFX switch controls the same sound preference.

Android haptics use [View.performHapticFeedback](https://developer.android.com/develop/ui/views/haptics/haptic-feedback) on the UI thread, with [system feedback constants](https://developer.android.com/reference/android/view/HapticFeedbackConstants) for tick, quick, reward, bomb and fever. They respect device touch-feedback settings and require no added vibration permission. Older Android versions use supported constants. Editor and other platforms have no haptic output; phone hardware feel remains a device check.

Run **Tools > VioletTap > QA > Check touch feedback and help** for per-contact audio-thread sample checks, pause/resume, raycast priority, Korean/English text fitting and preference controls. The Editor-only audio probe observes samples without changing playback. Local best, preferences and editor language are restored even on failure.
