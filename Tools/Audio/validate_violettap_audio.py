"""Check shipped PCM, loop boundaries, and a busy gameplay mix without Unity."""
import argparse
import json
import re
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOUNDS = ROOT / "Assets/Resources/Sounds"


def read(path):
    with wave.open(str(path), "rb") as wav:
        assert wav.getnchannels() == 2 and wav.getsampwidth() == 2, f"Invalid PCM format: {path}"
        rate = wav.getframerate()
        assert rate == 44100, f"Unexpected sample rate: {path}"
        data = np.frombuffer(wav.readframes(wav.getnframes()), dtype="<i2").reshape(-1, 2) / 32768.0
    return data, rate


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path, default=ROOT / "output/qa-2026-09-13/audio-validation.json")
    args = parser.parse_args()
    report = {"assets": [], "references": [], "gameplay_mix": {}}
    audio = {}
    for path in sorted(SOUNDS.rglob("*.wav")):
        data, rate = read(path)
        key = path.relative_to(SOUNDS).with_suffix("").as_posix()
        audio[key] = data
        peak = float(np.abs(data).max())
        rms = float(np.sqrt(np.mean(data ** 2)))
        assert 0.1 < peak < .75 and rms > .005, f"Silent or clipped asset: {path}"
        assert np.max(np.abs(data.mean(axis=0))) < .001, f"DC offset: {path}"
        seam = float(np.max(np.abs(data[0] - data[-1])))
        if key.startswith("BGM/"):
            # A loop seam should be no larger than ordinary musical sample transitions.
            transition = float(np.quantile(np.abs(np.diff(data, axis=0)), .99))
            assert seam < max(.008, transition), f"Discontinuous loop: {path}, step={seam}"
            assert len(data) / rate >= 15, f"Music loop too short: {path}"
        else:
            assert np.max(np.abs(data[[0, -1]])) < .0001, f"Unfaded effect edge: {path}"
        mono_rms = float(np.sqrt(np.mean(data.mean(axis=1) ** 2)))
        assert mono_rms > rms * .65, f"Excessive cancellation on a mono speaker: {path}"
        report["assets"].append({"path": key, "seconds": len(data) / rate,
            "peak_dbfs": 20 * np.log10(peak), "rms_dbfs": 20 * np.log10(rms),
            "loop_step": seam, "mono_loss_db": 20 * np.log10(mono_rms / rms)})

    for path in (ROOT / "Assets/01.Scripts").rglob("*.cs"):
        for resource in re.findall(r'"((?:SFX|BGM)/[^"\r\n]+)"', path.read_text(encoding="utf-8-sig")):
            assert resource in audio, f"Missing referenced audio: {resource} in {path}"
            report["references"].append(resource)

    music = audio["BGM/Gameplay_NeonRush"]
    mix = music.copy() * .32
    def add(key, start, gain):
        offset = round(start * 44100)
        count = min(len(mix) - offset, len(audio[key]))
        if count > 0: mix[offset:offset + count] += audio[key][:count] * gain
    add("SFX/Round_Start", 0, .58)
    for i, start in enumerate(np.arange(.22, 25, .145)):
        cue = "Target_TimeBonus" if i % 7 == 0 else "Target_Quick" if i % 4 == 0 else "Target_NeonTap"
        if 1.65 < start < 9.6:
            cue = "Fever_TimeBonus" if i % 7 == 0 else "Fever_Quick" if i % 4 == 0 else "Fever_Tap"
        add("SFX/" + cue, float(start), .66)
    add("SFX/Fever_Start", 1.65, .40)
    add("SFX/Fever_End", 9.6, .43)
    add("SFX/Target_Bomb", 18.7, .75)
    for start in range(23, 27): add("SFX/Timer_Tick", start, .27)
    peak = float(np.abs(mix).max())
    assert peak < .98, f"Busy gameplay mix clips: peak={peak}"
    report["gameplay_mix"] = {"hits_per_second": 1 / .145, "peak_dbfs": 20 * np.log10(peak),
                              "clipped_samples": int(np.count_nonzero(np.abs(mix) >= 1))}
    for tier, name, gain in ((10, "BGM/Gameplay_ComboDrive", .23),
                             (50, "BGM/Gameplay_ComboRush", .28)):
        layer = audio[name]
        assert len(layer) == len(music), f"Combo layer must have the gameplay song's exact loop length: {name}"
        layered_peak = float(np.abs(mix + layer * gain).max())
        assert layered_peak < .98, f"Busy {tier}-combo mix clips: peak={layered_peak}"
        report["gameplay_mix"][f"combo_{tier}_peak_dbfs"] = 20 * np.log10(layered_peak)
    report["references"] = sorted(set(report["references"]))
    report["status"] = "PASS"
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2), encoding="utf-8")
    with wave.open(str(args.report.with_name("gameplay-audio-preview.wav")), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(44100)
        wav.writeframes(np.round(mix * 32767).astype("<i2").tobytes())
    print(f"PASS: {len(audio)} clips, {len(report['references'])} references, busy mix peak {20 * np.log10(peak):.2f} dBFS")


if __name__ == "__main__":
    main()
