"""Original VioletTap synth score and cues; no external recordings or samples.

Run with bundled Python + NumPy. Existing WAV paths/GUIDs remain stable.
Music uses circular event tails and tempo-synced delays for continuous looping.
"""
import argparse
import hashlib
import json
import math
import wave
from pathlib import Path

import numpy as np

SAMPLE_RATE = 44100
ROOT = Path(__file__).resolve().parents[2] / "Assets" / "Resources" / "Sounds"


def midi(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


def window(count, attack=0.004, release=0.03):
    env = np.ones(count, dtype=np.float64)
    a = min(count, max(2, round(attack * SAMPLE_RATE)))
    r = min(count, max(2, round(release * SAMPLE_RATE)))
    env[:a] *= np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    env[-r:] *= np.cos(np.linspace(0, np.pi / 2, r)) ** 2
    return env


def tone(note, duration, kind="glass"):
    count = round(duration * SAMPLE_RATE)
    t = np.arange(count) / SAMPLE_RATE
    f = midi(note)
    phase = 2 * np.pi * f * t
    if kind == "pad":
        signal = (np.sin(phase) + 0.34 * np.sin(phase * 1.003)
                  + 0.23 * np.sin(phase * 0.998) + 0.1 * np.sin(phase * 2))
        signal *= window(count, min(0.28, duration / 3), min(0.48, duration / 2))
    elif kind == "bass":
        signal = np.sin(phase) + 0.32 * np.sin(phase * 2) * np.exp(-t * 9)
        signal += 0.10 * np.sin(phase * 3) * np.exp(-t * 14)
        signal *= np.exp(-t * 2.4) * window(count, 0.003, min(0.065, duration / 3))
    elif kind == "pluck":
        signal = np.sin(phase + 1.15 * np.exp(-t * 22) * np.sin(phase * 2))
        signal *= np.exp(-t * 8) * window(count, 0.002, min(0.075, duration / 3))
    elif kind == "lead":
        signal = np.sin(phase) + 0.27 * np.sin(phase * 2) + 0.1 * np.sin(phase * 3)
        signal += 0.12 * np.sin(phase * 1.004)
        signal *= np.exp(-t * 3.6) * window(count, 0.008, min(0.10, duration / 3))
    else:
        signal = np.sin(phase) + 0.28 * np.sin(phase * 2) * np.exp(-t * 14)
        signal += 0.09 * np.sin(phase * 3) * np.exp(-t * 26)
        signal *= np.exp(-t * 5.5) * window(count, 0.0025, min(0.12, duration / 3))
    return signal


def sweep(start_hz, end_hz, duration, strength=0.16):
    t = np.arange(round(duration * SAMPLE_RATE)) / SAMPLE_RATE
    k = math.log(end_hz / start_hz) / duration
    phase = 2 * np.pi * start_hz * np.expm1(k * t) / k
    return strength * (np.sin(phase) + 0.18 * np.sin(phase * 2)) * window(len(t), 0.008, 0.065)


class Mix:
    def __init__(self, name, duration, loop=False):
        self.name = name
        self.loop = loop
        self.data = np.zeros((round(duration * SAMPLE_RATE), 2), dtype=np.float64)
        seed = int.from_bytes(hashlib.blake2b(name.encode(), digest_size=8).digest(), "little")
        self.rng = np.random.default_rng(seed)

    def add(self, signal, start, gain=1.0, pan=0.0):
        stereo = signal[:, None] * np.array([math.sqrt((1 - pan) / 2),
                                             math.sqrt((1 + pan) / 2)]) * gain
        begin = round(start * SAMPLE_RATE)
        if self.loop:
            # Split at the boundary instead of truncating pads, releases, or pickup notes.
            pos = 0
            while pos < len(stereo):
                dst = (begin + pos) % len(self.data)
                count = min(len(stereo) - pos, len(self.data) - dst)
                self.data[dst:dst + count] += stereo[pos:pos + count]
                pos += count
        else:
            src = max(0, -begin)
            dst = max(0, begin)
            count = min(len(stereo) - src, len(self.data) - dst)
            if count > 0:
                self.data[dst:dst + count] += stereo[src:src + count]

    def note(self, note, start, duration, gain, pan=0.0, kind="glass"):
        self.add(tone(note, duration, kind), start, gain, pan)

    def drum(self, kind, start, gain=1.0, pan=0.0):
        duration = {"kick": 0.26, "snare": 0.18, "hat": 0.065, "open": 0.18, "shaker": 0.05}[kind]
        t = np.arange(round(duration * SAMPLE_RATE)) / SAMPLE_RATE
        noise = self.rng.normal(0, 0.35, len(t))
        high = np.concatenate(([0.0], np.diff(noise)))
        if kind == "kick":
            phase = 2 * np.pi * (49 * t + 105 * 0.022 * (1 - np.exp(-t / 0.022)))
            signal = np.sin(phase) * np.exp(-t * 16)
            signal += high * 0.13 * np.exp(-t * 220)
            signal *= 0.75
        elif kind == "snare":
            signal = high * np.exp(-t * 30) * 0.52
            signal += np.sin(2 * np.pi * 185 * t) * np.exp(-t * 32) * 0.30
            signal += noise * np.exp(-((t - 0.012) / 0.007) ** 2) * 0.2
        else:
            decay = 22 if kind == "open" else 70
            signal = high * np.exp(-t * decay) * (0.28 if kind == "shaker" else 0.4)
        signal *= window(len(t), 0.0006, min(0.025, duration / 3))
        self.add(signal, start, gain, pan)

    def delay(self, seconds, amount=0.18, repeats=3):
        dry = self.data.copy()
        for repeat in range(1, repeats + 1):
            offset = round(seconds * SAMPLE_RATE * repeat)
            echo = dry[:, ::-1] if repeat % 2 else dry
            if self.loop:
                self.data += np.roll(echo, offset, axis=0) * amount ** repeat
            elif offset < len(dry):
                self.data[offset:] += echo[:-offset] * amount ** repeat

    def finish(self, peak=0.72):
        self.data -= self.data.mean(axis=0)
        # Gentle saturation rounds drum transients; fixed output ceiling keeps mix headroom.
        self.data = np.tanh(self.data * 1.12)
        self.data *= peak / max(1e-9, np.abs(self.data).max())
        if not self.loop:
            self.data *= window(len(self.data), 0.001, 0.025)[:, None]
        return self.data

    def save(self, root, peak=0.72):
        data = self.finish(peak)
        pcm = np.round(data * 32767).astype("<i2")
        path = root / (self.name + ".wav")
        path.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(path), "wb") as output:
            output.setnchannels(2)
            output.setsampwidth(2)
            output.setframerate(SAMPLE_RATE)
            output.writeframes(pcm.tobytes())
        return {"file": self.name + ".wav", "seconds": round(len(data) / SAMPLE_RATE, 4),
                "loop": self.loop, "sample_rate": SAMPLE_RATE,
                "peak_dbfs": round(20 * np.log10(np.abs(data).max() + 1e-12), 2),
                "rms_dbfs": round(20 * np.log10(np.sqrt(np.mean(data ** 2)) + 1e-12), 2),
                "seam_step": round(float(np.max(np.abs(data[0] - data[-1]))), 7)}


# A minor / Fmaj7 / Cmaj9 / G6: shared palette across menus, play, and reward cues.
CHORDS = [(45, 57, 60, 64, 71), (41, 57, 60, 64, 69),
          (48, 55, 59, 62, 67), (43, 55, 59, 62, 64)]
HOOK = [(0, 76, .65), (.75, 72, .35), (1.5, 71, .5), (2.5, 67, .35), (3, 69, .75)]


def make_music(name, bpm, bars, energy):
    beat = 60 / bpm
    mix = Mix("BGM/" + name, bars * 4 * beat, loop=True)
    # Melodic and harmonic bus gets delay; dry drums stay focused.
    for bar in range(bars):
        root, *chord = CHORDS[bar % 4]
        start = bar * 4 * beat
        section = bar // 4
        for i, note in enumerate(chord):
            mix.note(note, start, 4.6 * beat, 0.055 if energy < 2 else 0.036,
                     (i - 1.5) * .30, "pad")
        bass_steps = [0, 1.5, 2, 3.25] if energy else [0, 2.5]
        for j, step in enumerate(bass_steps):
            mix.note(root - 12 if j != 3 else root, start + step * beat,
                     (.7 if energy else 1.25) * beat, .27 if energy == 2 else .19, kind="bass")
        arp_steps = [0, .75, 1.5, 2.25, 3, 3.5] if energy else [.5, 1.5, 2.5, 3.25]
        for j, step in enumerate(arp_steps):
            note = chord[(j + bar) % 4] + 12
            mix.note(note, start + step * beat, .58 * beat,
                     .065 if energy == 2 else .08, (-1) ** j * .5, "pluck")
        # Call/response leaves silence around the hook; second half opens the arrangement.
        if (energy == 0 and bar % 2 == 0) or (energy > 0 and (bar % 4 < 2 or section % 2)):
            for step, note, length in HOOK:
                shift = [0, -4, -5, -2][bar % 4]
                if bar % 2: step, note = step + .25, note - 7
                mix.note(note + shift, start + step * beat, (length + .2) * beat,
                         .105 if energy == 2 else .075, .16 if bar % 2 else -.16,
                         "lead" if energy == 2 else "glass")
        if energy == 2 and section % 2 == 1:
            for step in [.5, 2.5]:
                for i, note in enumerate(chord[:3]):
                    mix.note(note + 12, start + step * beat, .42 * beat, .045, (i - 1) * .4, "pluck")
    mix.delay(beat * .75, .23 if energy == 0 else .17, repeats=4)
    for bar in range(bars):
        start = bar * 4 * beat
        breakdown = energy == 2 and bar in (8, 9)
        if energy == 0:
            mix.drum("kick", start, .20)
            mix.drum("shaker", start + 2 * beat, .12, .3)
        else:
            kicks = [0, 2.5] if energy == 1 else ([0, 2] if breakdown else [0, 1, 2, 3])
            for step in kicks:
                mix.drum("kick", start + step * beat, .46 if energy == 2 else .34)
            for step in [1, 3]:
                mix.drum("snare", start + step * beat, .35 if energy == 2 else .22)
            for j in range(8):
                mix.drum("open" if j % 2 and energy == 2 and not breakdown else "hat",
                         start + j * beat / 2, (.22 if j % 2 else .12), (-1) ** j * .35)
            if energy == 2 and not breakdown:
                for j in range(8):
                    mix.drum("shaker", start + (j * .5 + .25) * beat, .09, (-1) ** j * .65)
            if bar % 4 == 3:
                for j in range(3):
                    mix.drum("snare", start + (3.25 + j * .25) * beat, .10 + .055 * j, (j - 1) * .25)
                mix.add(sweep(210, 840, .6 * beat, .055), start + 3.4 * beat)
    return mix


def make_cues():
    cues = []
    def cue(name, seconds):
        result = Mix("SFX/" + name, seconds)
        cues.append(result)
        return result

    mix = cue("Button_Click", .16)
    mix.note(81, 0, .14, .30, -.12, "pluck")
    mix.note(88, .018, .12, .12, .12, "glass")

    mix = cue("Target_NeonTap", .20)
    mix.note(69, 0, .17, .39, 0, "pluck")
    mix.note(81, .002, .14, .28, -.1, "pluck")
    mix.add(sweep(520, 180, .12, .20), 0)
    mix.drum("snare", 0, .25)
    mix.delay(.040, .10, 2)

    mix = cue("Target_Quick", .25)
    for j, note in enumerate((88, 93)):
        mix.note(note, j * .036, .20, .26, -.2 + j * .4, "pluck")
    mix.add(sweep(640, 1280, .065, .08), 0)
    mix.note(76, 0, .17, .28, kind="pluck")
    mix.drum("snare", 0, .18)
    mix.delay(.06, .15, 2)

    mix = cue("Target_TimeBonus", .62)
    for j, note in enumerate((81, 88, 93)):
        mix.note(note, j * .057, .39, .26 / (1 + j * .2), -.3 + j * .3)
    mix.delay(.093, .22, 3)
    mix.note(69, 0, .22, .30, kind="pluck")
    mix.drum("hat", 0, .20)

    mix = cue("Target_Bomb", .50)
    mix.add(sweep(155, 42, .35, .46), 0)
    mix.drum("snare", .008, .42)
    mix.note(44, .035, .3, .14, kind="bass")
    # Midrange crunch keeps the impact audible through a small phone speaker.
    mix.add(sweep(620, 155, .24, .33), .002)
    mix.note(57, .005, .19, .28, kind="lead")

    for name, notes in (("Fever_Tap", (81, 88)), ("Fever_Quick", (88, 93)),
                        ("Fever_TimeBonus", (81, 88, 93))):
        mix = cue(name, .34 if name == "Fever_TimeBonus" else .23)
        mix.note(69, 0, .15, .33, kind="pluck")
        mix.add(sweep(720, 240, .10, .20), 0)
        mix.drum("snare", 0, .24)
        for j, note in enumerate(notes):
            mix.note(note, j * .033, .18, .28, (j - .5) * .15, "lead")
        mix.delay(.044, .12, 2)

    mix = cue("Target_Miss", .20)
    mix.add(sweep(380, 230, .16, .15), 0)

    mix = cue("Fever_Start", .95)
    mix.drum("kick", 0, .25)
    for j, note in enumerate((69, 76, 81, 84, 88, 93)):
        mix.note(note, j * .055, .5, .24 / (1 + j * .1), -.4 + j * .16, "lead")
    mix.delay(.115, .22, 3)

    mix = cue("Fever_End", .48)
    for j, note in enumerate((88, 81, 76)):
        mix.note(note, j * .045, .29, .19, .3 - j * .3)
    mix.delay(.085, .16, 2)

    mix = cue("Round_Start", .67)
    for j, note in enumerate((69, 76, 81)):
        mix.note(note, j * .085, .35, .24, -.25 + j * .25, "pluck")
    mix.drum("kick", .17, .19)
    mix.delay(.095, .15, 2)

    mix = cue("Round_Complete", 1.05)
    for j, note in enumerate((69, 72, 76, 83)):
        mix.note(note, j * .07, .7, .17, -.35 + j * .23)
    mix.delay(.13, .20, 3)

    mix = cue("New_Best", 1.40)
    for j, note in enumerate((76, 81, 84, 88, 93)):
        mix.note(note, j * .082, .55, .24, -.35 + j * .175, "lead")
    for j, note in enumerate((69, 72, 76, 83)):
        mix.note(note, .42, .78, .12, -.4 + j * .26)
    mix.delay(.14, .23, 3)

    mix = cue("Timer_Tick", .10)
    mix.note(88, 0, .08, .22, kind="pluck")
    return cues


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    tracks = [make_music("Intro_NeonAwakening", 104, 8, 0),
              make_music("Game_NeonLobby", 120, 8, 1),
              make_music("Gameplay_NeonRush", 140, 16, 2)]
    tracks += make_cues()
    report = [track.save(args.output, .72 if track.loop else .68) for track in tracks]
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
