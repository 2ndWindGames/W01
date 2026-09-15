"""Generate bilingual narration locally from Voice/script.json.

Uses the open-weight Supertonic 3 model. Installation/download details:
https://github.com/supertone-oss-archive/supertonic#quick-start

Install: python -m pip install supertonic==1.3.1
Download the pinned model snapshot to .voice_st_models/ at the repository root:
  hf download supertone-oss-archive/supertonic-3 \
    --revision aafc6e32416a594460b32413efc49d7fe4ce6d46 \
    --local-dir .voice_st_models
Once the model is present, this script runs without network access or text uploads.
The model is OpenRAIL-M licensed; publishing the output requires an AI-generated
content disclosure under license section 5(e).
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import subprocess
import tempfile
import wave

from supertonic import TTS


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
VOICE_DIR = HERE / "Voice"
MODEL_DIR = ROOT / ".voice_st_models"
FFMPEG = Path(r"C:\KMPlayer\ffmpeg.exe")
SLOTS = [4, 5, 6, 6, 6, 6, 6, 6, 7, 8]
LANGS = {"KO": "ko", "EN": "en"}
VOICE_STYLE = "F1"
SPEED = {"KO": 1.0, "EN": 1.0}


def duration(path: Path) -> float:
    with wave.open(str(path), "rb") as reader:
        assert reader.getframerate() == 48000
        assert reader.getnchannels() == 1
        return reader.getnframes() / reader.getframerate()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", nargs="*", metavar="LOCALE-NN", help="Regenerate only named cues, e.g. KO-06 EN-08")
    selected = set(parser.parse_args().only or [])
    valid = {f"{locale}-{i:02}" for locale in LANGS for i in range(len(SLOTS))}
    if selected - valid:
        parser.error(f"Unknown cue(s): {', '.join(sorted(selected - valid))}")
    if not FFMPEG.is_file():
        raise FileNotFoundError(FFMPEG)
    if not (MODEL_DIR / "onnx" / "tts.json").is_file():
        raise FileNotFoundError(f"Download Supertonic 3 model to {MODEL_DIR}")
    script = json.loads((VOICE_DIR / "script.json").read_text(encoding="utf-8-sig"))
    for locale in LANGS:
        if len(script[locale]) != len(SLOTS):
            raise ValueError(f"{locale}: expected {len(SLOTS)} lines")
    engine = TTS(model="supertonic-3", model_dir=MODEL_DIR, auto_download=False)
    style = engine.get_voice_style(VOICE_STYLE)
    with tempfile.TemporaryDirectory(prefix="violettap-voice-") as tmp:
        for locale, lang in LANGS.items():
            for i, phrase in enumerate(script[locale]):
                if selected and f"{locale}-{i:02}" not in selected:
                    continue
                raw = Path(tmp) / f"{locale}-{i:02}.wav"
                dest = VOICE_DIR / f"{locale}-{i:02}.wav"
                samples, _ = engine.synthesize(
                    phrase,
                    voice_style=style,
                    lang=lang,
                    total_steps=10,
                    speed=SPEED[locale],
                    verbose=False,
                )
                engine.save_audio(samples, str(raw))
                subprocess.run(
                    [str(FFMPEG), "-y", "-hide_banner", "-loglevel", "error", "-i", str(raw),
                     "-af", "highpass=f=85,lowpass=f=14500", "-ac", "1", "-ar", "48000",
                     "-acodec", "pcm_s16le", str(dest)],
                    check=True,
                )
                seconds = duration(dest)
                print(f"{dest.name}: {seconds:.2f}s / {SLOTS[i]}s", flush=True)
                if seconds > SLOTS[i] - 0.35:
                    raise ValueError(f"{dest.name}: {seconds:.2f}s exceeds {SLOTS[i]}s slot")


if __name__ == "__main__":
    main()
