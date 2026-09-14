import math
import wave
from pathlib import Path

import numpy as np


SAMPLE_RATE = 22050
ROOT = Path(__file__).resolve().parents[2] / "Assets" / "Resources" / "Sounds"
RNG = np.random.default_rng(20260908)


def midi(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


def envelope(length, attack=0.01, release=0.12):
    env = np.ones(length, dtype=np.float32)
    a = min(length, int(attack * SAMPLE_RATE))
    r = min(length, int(release * SAMPLE_RATE))
    if a:
        env[:a] = np.linspace(0, 1, a, endpoint=False)
    if r:
        env[-r:] *= np.linspace(1, 0, r)
    return env


def add_tone(track, start, duration, frequency, volume, pan=0.0, kind="sine", detune=0.0):
    begin = int(start * SAMPLE_RATE)
    count = min(int(duration * SAMPLE_RATE), len(track) - begin)
    if count <= 0:
        return
    t = np.arange(count, dtype=np.float32) / SAMPLE_RATE
    phase = 2 * np.pi * frequency * t
    if kind == "pulse":
        signal = np.tanh(2.2 * (np.sin(phase) + 0.35 * np.sin(phase * 2.01 + detune)))
    elif kind == "glass":
        signal = np.sin(phase) + 0.34 * np.sin(phase * 2.01) + 0.13 * np.sin(phase * 3.98)
    elif kind == "bass":
        signal = np.sin(phase) + 0.28 * np.sin(phase * 0.5)
    else:
        signal = np.sin(phase)
    signal *= envelope(count, min(0.018, duration * 0.12), min(0.2, duration * 0.4)) * volume
    left = math.sqrt((1 - pan) * 0.5)
    right = math.sqrt((1 + pan) * 0.5)
    track[begin:begin + count, 0] += signal * left
    track[begin:begin + count, 1] += signal * right


def add_kick(track, start, volume=0.35):
    duration = 0.24
    begin = int(start * SAMPLE_RATE)
    count = min(int(duration * SAMPLE_RATE), len(track) - begin)
    if count <= 0:
        return
    t = np.arange(count, dtype=np.float32) / SAMPLE_RATE
    phase = 2 * np.pi * (78 * t - 33 * t * t)
    signal = np.sin(phase) * np.exp(-t * 17) * volume
    track[begin:begin + count, 0] += signal
    track[begin:begin + count, 1] += signal


def add_noise(track, start, duration, volume, pan=0.0, decay=24):
    begin = int(start * SAMPLE_RATE)
    count = min(int(duration * SAMPLE_RATE), len(track) - begin)
    if count <= 0:
        return
    t = np.arange(count, dtype=np.float32) / SAMPLE_RATE
    noise = RNG.normal(0, 1, count).astype(np.float32) * np.exp(-t * decay) * volume
    left = math.sqrt((1 - pan) * 0.5)
    right = math.sqrt((1 + pan) * 0.5)
    track[begin:begin + count, 0] += noise * left
    track[begin:begin + count, 1] += noise * right


def add_delay(track, seconds=0.19, amount=0.18):
    offset = int(seconds * SAMPLE_RATE)
    track[offset:, 1] += track[:-offset, 0] * amount
    track[offset:, 0] += track[:-offset, 1] * amount * 0.72


def normalize(track, peak=0.88):
    maximum = np.max(np.abs(track))
    if maximum > 0:
        track *= peak / maximum
    return np.tanh(track * 1.08) / np.tanh(1.08)


def save(path, track):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = (normalize(track) * 32767).astype("<i2")
    with wave.open(str(path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm.tobytes())


def make_intro():
    bpm, bars = 96, 8
    beat = 60 / bpm
    track = np.zeros((int(bars * 4 * beat * SAMPLE_RATE), 2), dtype=np.float32)
    chords = [(45, 52, 57, 60), (41, 48, 53, 57), (43, 50, 55, 59), (40, 47, 52, 55)]
    for bar in range(bars):
        chord = chords[bar % len(chords)]
        start = bar * 4 * beat
        for i, note in enumerate(chord):
            add_tone(track, start, 3.85 * beat, midi(note), 0.075, (i - 1.5) * 0.22, "glass")
        for step in range(8):
            note = chord[(step * 2 + bar) % len(chord)] + 12
            add_tone(track, start + step * beat / 2, beat * 0.42, midi(note), 0.085, -0.5 + step / 7, "glass")
        add_tone(track, start, 3.7 * beat, midi(chord[0] - 12), 0.11, 0, "bass")
        add_kick(track, start, 0.13)
        add_noise(track, start + 2 * beat, 0.1, 0.018, 0.25)
    add_delay(track, 0.23, 0.22)
    save(ROOT / "BGM" / "Intro_NeonAwakening.wav", track)


def make_game_idle():
    bpm, bars = 112, 8
    beat = 60 / bpm
    track = np.zeros((int(bars * 4 * beat * SAMPLE_RATE), 2), dtype=np.float32)
    roots = [45, 41, 48, 43]
    for bar in range(bars):
        root = roots[bar % 4]
        start = bar * 4 * beat
        for step in range(8):
            notes = [root + 12, root + 15, root + 19, root + 22]
            add_tone(track, start + step * beat / 2, beat * 0.34, midi(notes[step % 4]), 0.1, (-1) ** step * 0.45, "pulse")
        for b in range(4):
            add_tone(track, start + b * beat, beat * 0.7, midi(root - 12 if b < 2 else root - 5), 0.12, 0, "bass")
            add_kick(track, start + b * beat, 0.2 if b in (0, 2) else 0.11)
            add_noise(track, start + b * beat + beat / 2, 0.055, 0.025, (-1) ** b * 0.35, 42)
        for note in (root, root + 7, root + 12):
            add_tone(track, start, 3.7 * beat, midi(note), 0.038, 0, "glass")
    add_delay(track, 0.16, 0.14)
    save(ROOT / "BGM" / "Game_NeonLobby.wav", track)


def make_gameplay():
    bpm, bars = 144, 16
    beat = 60 / bpm
    track = np.zeros((int(bars * 4 * beat * SAMPLE_RATE), 2), dtype=np.float32)
    roots = [45, 48, 41, 43]
    for bar in range(bars):
        root = roots[bar % 4]
        start = bar * 4 * beat
        sequence = [12, 19, 15, 22, 12, 24, 19, 15, 12, 19, 27, 22, 19, 15, 12, 10]
        for step, interval in enumerate(sequence):
            add_tone(track, start + step * beat / 4, beat * 0.19, midi(root + interval), 0.085, -0.65 + (step % 8) / 7 * 1.3, "pulse")
        for b in range(4):
            add_kick(track, start + b * beat, 0.35)
            add_tone(track, start + b * beat, beat * 0.62, midi(root - 12 if b < 2 else root - 5), 0.16, 0, "bass")
            add_noise(track, start + b * beat + beat / 2, 0.11, 0.075, 0, 28)
            add_noise(track, start + b * beat, 0.035, 0.025, (-1) ** b * 0.6, 75)
        if bar % 4 == 3:
            for roll in range(4):
                add_noise(track, start + (3 + roll * 0.25) * beat, 0.045, 0.04 + roll * 0.012, 0.4, 58)
    add_delay(track, 0.105, 0.1)
    save(ROOT / "BGM" / "Gameplay_NeonRush.wav", track)


def make_button():
    duration = 0.14
    track = np.zeros((int(duration * SAMPLE_RATE), 2), dtype=np.float32)
    add_tone(track, 0, duration, 620, 0.44, -0.12, "glass")
    add_tone(track, 0.018, duration - 0.018, 930, 0.27, 0.12, "sine")
    add_noise(track, 0, 0.035, 0.08, 0, 70)
    save(ROOT / "SFX" / "Button_Click.wav", track)


def make_neon_tap():
    duration = 0.34
    track = np.zeros((int(duration * SAMPLE_RATE), 2), dtype=np.float32)
    for step, frequency in enumerate((740, 1110, 1480, 2220)):
        add_tone(track, step * 0.028, duration - step * 0.028, frequency, 0.27 / (1 + step * 0.22), -0.45 + step * 0.3, "glass")
    add_noise(track, 0, 0.075, 0.13, 0, 48)
    add_delay(track, 0.055, 0.18)
    save(ROOT / "SFX" / "Target_NeonTap.wav", track)


if __name__ == "__main__":
    make_intro()
    make_game_idle()
    make_gameplay()
    make_button()
    make_neon_tap()
    print("Generated VioletTap audio assets in", ROOT)
