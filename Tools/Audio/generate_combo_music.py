"""Generate two phase-locked rhythm layers for the 140 BPM gameplay loop.

Run with bundled Python + NumPy. Both layers span the same 16 bars as
Gameplay_NeonRush, so the game can crossfade between them without changing
the song's speed, pitch, or playback position.
"""
import json

from generate_violettap_audio import CHORDS, Mix, ROOT

BPM = 140
BARS = 16
BEAT = 60 / BPM


def build_drive():
    mix = Mix("BGM/Gameplay_ComboDrive", BARS * 4 * BEAT, loop=True)
    for bar in range(BARS):
        start = bar * 4 * BEAT
        chord = CHORDS[bar % 4][1:]
        # A higher eighth-note hook increases motion over the base song's
        # four-on-the-floor kick without muddying its bass and lead.
        for step in range(8):
            note = chord[(step + bar) % len(chord)] + 12
            mix.note(note, start + (step + .5) * BEAT / 2,
                     .18 * BEAT, .12 if step % 2 else .075,
                     -.42 if step % 2 else .42, "pluck")
        for step in (1.5, 3.5):
            mix.drum("open", start + step * BEAT, .18, .35 if step < 2 else -.35)
        if bar % 4 == 3:
            for step in (3.5, 3.75):
                mix.drum("snare", start + step * BEAT, .085)
    return mix


def build_rush():
    mix = Mix("BGM/Gameplay_ComboRush", BARS * 4 * BEAT, loop=True)
    for bar in range(BARS):
        start = bar * 4 * BEAT
        chord = CHORDS[bar % 4][1:]
        # The 50-combo tier replaces the eighth-note line with a denser,
        # brighter sixteenth-note run and syncopated impacts.
        for step in range(16):
            note = chord[(step // 2 + bar) % len(chord)] + 12
            mix.note(note, start + (step + .45) * BEAT / 4,
                     .13 * BEAT, .13 if step % 4 == 3 else .075,
                     (-1 if step % 2 else 1) * .48, "pluck")
            mix.drum("hat", start + step * BEAT / 4,
                     .14 if step % 4 == 2 else .085,
                     (-1 if step % 2 else 1) * .35)
        for step in (.75, 1.75, 2.75, 3.75):
            mix.drum("kick", start + step * BEAT, .13)
        for step in (1.5, 3.5):
            mix.drum("open", start + step * BEAT, .19)
        if bar % 4 == 3:
            for step in (3.25, 3.5, 3.75):
                mix.drum("snare", start + step * BEAT, .10)
    return mix


if __name__ == "__main__":
    print(json.dumps([layer.save(ROOT, .68) for layer in
                      (build_drive(), build_rush())], indent=2))
