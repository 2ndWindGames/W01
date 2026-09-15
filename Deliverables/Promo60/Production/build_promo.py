"""Render localized VioletTap promos from Unity captures and shipped UI/audio."""
from __future__ import annotations

import json
import math
import os
from pathlib import Path
import subprocess
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Deliverables" / "Promo60"
PROD = OUT / "Production"
TEMP = PROD / "render-work"
TEMP.mkdir(parents=True, exist_ok=True)
FFMPEG = Path(r"C:\KMPlayer\ffmpeg.exe")
FPS = 30
SIZE = (1080, 1920)
SR = 48000
SLOTS = [4, 5, 6, 6, 6, 6, 6, 6, 7, 8]
STARTS = [sum(SLOTS[:i]) for i in range(len(SLOTS))]
assert sum(SLOTS) == 60

QA_NEW = ROOT / "output/qa-2026-09-13/gameplay-video/run-20260914-191417/violet-tap-gameplay.mp4"
QA_OLD = ROOT / "output/qa-2026-09-13/gameplay-video/run-20260914-183722/violet-tap-gameplay.mp4"
CAPTURE_ROOTS = [ROOT / "output/promo-production/Project/output/promo-production",
                 ROOT / "output/promo-production"]
FONTS = ROOT / "Assets/Resources/Fonts/Candidates"
FONT_KO_TITLE = FONTS / "Kor/dohyeon/DoHyeon-Regular.ttf"
FONT_KO_BODY = FONTS / "Kor/ibmplexsanskr/IBMPlexSansKR-Regular.ttf"
FONT_EN_TITLE = FONTS / "Eng/orbitron/Orbitron-Regular.ttf"
FONT_EN_BODY = FONTS / "Eng/exo2/Exo2-Regular.ttf"

TITLES = {
    "KO": ["네온을 잡아라", "네온터치", "10콤보 / 점수 2배", "피버 / 타겟 증가", "+시간을 잡아라", "폭탄은 피하자", "점점 빨라진다", "1 - 2 - 3", "50콤보 / 점수 3배", "네온터치에 도전"],
    "EN": ["CHASE THE NEON", "VIOLETTAP", "10 COMBO / 2X POINTS", "FEVER / MORE TARGETS", "GRAB MORE TIME", "AVOID THE BOMB", "THE PACE PICKS UP", "1 - 2 - 3", "50 COMBO / 3X POINTS", "TRY VIOLETTAP"],
}
ACCENTS = ["#28e7ff", "#d989ff", "#77fcba", "#2af7ee", "#ffd252", "#ff6d92", "#37dfff", "#b394ff", "#ffd252", "#d989ff"]


def run(args: list[str], **kwargs) -> None:
    print("RUN", " ".join(str(x) for x in args[:8]), flush=True)
    subprocess.run([str(x) for x in args], check=True, **kwargs)


def font(path: Path, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(path), size)


def text_width(draw: ImageDraw.ImageDraw, value: str, face: ImageFont.FreeTypeFont) -> int:
    b = draw.textbbox((0, 0), value, font=face)
    return b[2] - b[0]


def wrap(value: str, face: ImageFont.FreeTypeFont, limit: int, ko: bool) -> list[str]:
    test = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    words = list(value) if ko else value.split(" ")
    joiner = "" if ko else " "
    lines: list[str] = []
    line = ""
    for word in words:
        candidate = line + (joiner if line else "") + word
        if line and text_width(test, candidate, face) > limit and not (ko and word in "!?,.。"):
            lines.append(line.strip())
            line = word
        else:
            line = candidate
    if line:
        lines.append(line.strip())
    return lines


def create_base() -> Path:
    p = TEMP / "base.png"
    img = Image.new("RGB", SIZE, "#050613")
    d = ImageDraw.Draw(img)
    for y in range(1920):
        wave = math.sin(y / 110) * 3
        color = (5 + int(y / 750), 7 + int(wave + 3), 20 + int(y / 340))
        d.line((0, y, 1079, y), fill=color)
    glow = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.rounded_rectangle((79, 151, 1001, 1769), radius=22, outline=(39, 218, 255, 210), width=9)
    glow = glow.filter(ImageFilter.GaussianBlur(24))
    img = Image.alpha_composite(img.convert("RGBA"), glow).convert("RGB")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((77, 149, 1003, 1771), radius=20, outline="#235784", width=3)
    d.rounded_rectangle((82, 154, 998, 1766), radius=16, outline="#8d53d1", width=2)
    img.save(p, optimize=True)
    return p


def create_overlay(locale: str, i: int, caption: str) -> Path:
    p = TEMP / f"overlay-{locale}-{i:02}.png"
    img = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    accent = ACCENTS[i]
    d.rounded_rectangle((130, 27, 950, 132), radius=21, fill=(4, 8, 24, 228), outline=accent, width=3)
    d.line((182, 140, 898, 140), fill=accent, width=3)
    title_face = font(FONT_KO_TITLE if locale == "KO" else FONT_EN_TITLE, 53 if locale == "KO" else 45)
    title = TITLES[locale][i]
    while text_width(d, title, title_face) > 730:
        title_face = font(FONT_KO_TITLE if locale == "KO" else FONT_EN_TITLE, title_face.size - 2)
    tb = d.textbbox((0, 0), title, font=title_face)
    d.text(((1080 - (tb[2] - tb[0])) // 2, 51 - tb[1]), title, font=title_face, fill="#f8f5ff", stroke_width=1, stroke_fill=accent)
    size = 48 if locale == "KO" else 47
    face_path = FONT_KO_BODY if locale == "KO" else FONT_EN_BODY
    while True:
        body_face = font(face_path, size)
        lines = wrap(caption, body_face, 804, locale == "KO")
        if len(lines) <= 2:
            break
        size -= 2
        if size <= 34:
            raise ValueError(f"Caption too long for scene {locale}/{i}: {caption}")
    spacing = size + 10
    block_h = len(lines) * spacing
    panel_top = 1471 if len(lines) == 2 else 1500
    panel_bottom = 1658 if len(lines) == 2 else 1631
    d.rounded_rectangle((101, panel_top, 979, panel_bottom), radius=22, fill=(3, 8, 23, 210), outline=accent, width=2)
    d.line((168, panel_top + 8, 912, panel_top + 8), fill=accent, width=2)
    y = (panel_top + panel_bottom - block_h) // 2
    for line in lines:
        w = text_width(d, line, body_face)
        d.text(((1080 - w) // 2, y), line, font=body_face, fill="#ffffff", stroke_width=1, stroke_fill="#233650")
        y += spacing
    d.line((164, 1808, 916, 1808), fill=accent, width=2)
    img.save(p, optimize=True)
    return p


def timecode(t: float) -> str:
    ms = int(round(t * 1000))
    h, ms = divmod(ms, 3_600_000)
    m, ms = divmod(ms, 60_000)
    s, ms = divmod(ms, 1000)
    return f"{h:02}:{m:02}:{s:02},{ms:03}"


def voice_duration(path: Path) -> float:
    with wave.open(str(path), "rb") as w:
        return w.getnframes() / w.getframerate()


def make_srt(locale: str, phrases: list[str]) -> list[float]:
    starts: list[float] = []
    parts: list[str] = []
    for i, phrase in enumerate(phrases):
        dur = voice_duration(PROD / "Voice" / f"{locale}-{i:02}.wav")
        offset = max(0.0, min(0.48, (SLOTS[i] - dur) / 2))
        begin = STARTS[i] + offset
        end = min(STARTS[i] + SLOTS[i] - 0.05, begin + dur + 0.1)
        starts.append(begin)
        parts.append(f"{i+1}\n{timecode(begin)} --> {timecode(end)}\n{phrase}\n")
    (OUT / f"VioletTap_Promo_{locale}.srt").write_text("\n".join(parts), encoding="utf-8-sig")
    return starts


def capture_source(locale: str) -> tuple[Path, list[dict]] | None:
    """Find a complete, locale-specific Game View capture; never cross locales."""
    candidates = []
    for root in CAPTURE_ROOTS:
        if not (root / "capture-complete.txt").exists():
            continue
        for path in root.glob(f"capture-*/{locale.lower()}-gameplay.mp4"):
            events = path.with_name(f"{locale.lower()}-events.jsonl")
            if events.exists() and path.stat().st_size > 1_000_000:
                candidates.append((path.stat().st_mtime, path, events))
    if not candidates:
        return None
    _, path, event_file = max(candidates)
    events = [json.loads(line) for line in event_file.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
    return path, events


def event_time(events: list[dict], kind: str, combo: int = 0) -> float | None:
    for e in events:
        if kind == "combo" and e.get("kind") == "state" and e.get("combo", 0) >= combo:
            return float(e["time"])
        if kind == "bomb" and e.get("kind") == "tap" and e.get("target") == "Bomb":
            return float(e["time"])
        if kind == "time_bonus" and e.get("kind") == "tap" and e.get("target") == "TimeBonus" and e.get("time", 0) > 25:
            return float(e["time"])
        if kind == "sequence" and e.get("kind") == "tap" and e.get("seq") == 1:
            return float(e["time"])
        if kind == "fever_late" and e.get("kind") == "fever_start" and e.get("time", 0) > 25:
            return float(e["time"])
        if kind == "phase_late" and e.get("kind") == "phase" and e.get("time", 0) > 20:
            return float(e["time"])
        if kind != "combo" and e.get("kind") == kind:
            return float(e["time"])
    return None


def gameplay_shots(locale: str) -> dict[int, tuple[Path, float, bool]]:
    source = capture_source(locale)
    if source:
        path, events = source
        end = max(float(e["time"]) for e in events)
        def at(t: float | None, duration: int, before: float = 0.5) -> float:
            start = (t if t is not None else 9.0) - before
            return max(0.0, min(start, end - duration - 0.1))
        result = {
            0: (path, at(event_time(events, "round_start"), SLOTS[0], 0.1), True),
            2: (path, at(event_time(events, "combo", 10), SLOTS[2], 1.0), True),
            3: (path, at(event_time(events, "fever_late") or event_time(events, "fever_start"), SLOTS[3], 0.5), True),
            4: (path, at(event_time(events, "time_bonus"), SLOTS[4], 0.4), True),
            5: (path, at(event_time(events, "bomb"), SLOTS[5], 0.3), True),
            6: (path, at(event_time(events, "phase_late") or event_time(events, "phase"), SLOTS[6], 0.5), True),
            7: (path, at(event_time(events, "sequence"), SLOTS[7], 0.06), True),
        }
        combo_50 = event_time(events, "combo", 50)
        if combo_50 is not None:
            result[8] = (path, at(combo_50, SLOTS[8], 0.7), True)
        return result
    if locale == "EN":
        raise FileNotFoundError("English Unity gameplay capture is required; Korean QA footage is not an English source")
    return {0: (QA_NEW, 16, False), 2: (QA_NEW, 10, False),
            5: (QA_NEW, 18, False), 6: (QA_OLD, 10, False)}


def render_segment(locale: str, i: int, caption: str, base: Path,
                   footage: dict[int, tuple[Path, float, bool]]) -> Path:
    dur = SLOTS[i]
    pp = ROOT / "output/play-store-3.7.1" / ("ko-KR" if locale == "KO" else "en-US")
    stills = {
        1: pp / "phone-04-intro.png",
        3: pp / "phone-01-fever.png",
        4: pp / "phone-03-neon-targets.png",
        7: pp / "phone-05-guide.png",
        8: pp / "phone-02-combo-50.png",
        9: pp / "phone-04-intro.png",
    }
    if locale == "KO":
        stills[8] = PROD / "SourceFrame_KO_50Combo.png"
    if i in footage:
        stills.pop(i, None)
    overlay = create_overlay(locale, i, caption)
    dest = TEMP / f"clip-v2-{locale}-{i:02}.mp4"
    cmd = [FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-loop", "1", "-framerate", str(FPS), "-t", str(dur), "-i", base]
    if i in stills:
        cmd += ["-loop", "1", "-framerate", str(FPS), "-t", str(dur), "-i", stills[i]]
        body = "[1:v]zoompan=z='min(zoom+0.0004,1.05)':d=1:x='(iw-iw/zoom)/2':y='(ih-ih/zoom)/2':s=900x1600,fps=30,setsar=1[game]"
    else:
        path, ss, full_size = footage[i]
        cmd += ["-ss", str(ss), "-t", str(dur), "-i", path]
        if full_size and i == 7:
            # A real 1→2→3 sequence lasts less than a second under the capture's
            # automated taps. Editorial slow motion makes those targets readable.
            body = ("[1:v]split=2[a][b];"
                    "[a]trim=duration=0.45,setpts=(PTS-STARTPTS)*6.666667,crop=900:1600:90:230,"
                    "fps=30,setsar=1[slow];"
                    "[b]trim=start=0.45:duration=3,setpts=PTS-STARTPTS,crop=900:1600:90:230,"
                    "fps=30,setsar=1[fast];"
                    "[slow][fast]concat=n=2:v=1:a=0[game]")
        elif full_size and i in (3, 4, 5, 6):
            body = f"[1:v]crop=900:1600:90:230,fps={FPS},setsar=1,setpts=PTS-STARTPTS[game]"
        elif full_size:
            body = f"[1:v]scale=900:1600:flags=lanczos,fps={FPS},setsar=1,setpts=PTS-STARTPTS[game]"
        else:
            body = f"[1:v]crop=540:870:0:300,scale=900:1450:flags=lanczos,pad=900:1600:0:75:color=0x07091b,fps={FPS},setsar=1,setpts=PTS-STARTPTS[game]"
    cmd += ["-loop", "1", "-framerate", str(FPS), "-t", str(dur), "-i", overlay]
    fc = body + ";[0:v][game]overlay=(W-w)/2:157:shortest=1[scene];[scene][2:v]overlay=0:0:shortest=1,format=yuv420p,fade=t=in:st=0:d=0.12:color=0x101833,fade=t=out:st=" + str(dur - 0.12) + ":d=0.12:color=0x101833[v]"
    cmd += ["-filter_complex", fc, "-map", "[v]", "-an", "-frames:v", str(dur * FPS), "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-pix_fmt", "yuv420p", "-r", str(FPS), "-movflags", "+faststart", dest]
    run(cmd)
    return dest


def decode_audio(path: Path) -> np.ndarray:
    proc = subprocess.run([str(FFMPEG), "-hide_banner", "-loglevel", "error", "-i", str(path), "-ar", str(SR), "-ac", "2", "-f", "s16le", "-"], capture_output=True, check=True)
    return np.frombuffer(proc.stdout, dtype="<i2").astype(np.float32).reshape(-1, 2) / 32768.0


def mix_audio(locale: str, voice_starts: list[float]) -> Path:
    total = SR * 60
    music = np.zeros((total, 2), dtype=np.float32)
    all_audio = np.zeros_like(music)
    bgm_dir = ROOT / "Assets/Resources/Sounds/BGM"
    for start, end, name in [(0, 15, "Gameplay_NeonRush.wav"), (15, 35, "Gameplay_ComboDrive.wav"), (35, 52, "Gameplay_ComboRush.wav"), (52, 60, "Intro_NeonAwakening.wav")]:
        a = decode_audio(bgm_dir / name)
        count = (end - start) * SR
        tiled = np.tile(a, (math.ceil(count / len(a)), 1))[:count].copy()
        fade = min(SR // 2, count // 8)
        tiled[:fade] *= np.linspace(0, 1, fade)[:, None]
        tiled[-fade:] *= np.linspace(1, 0, fade)[:, None]
        music[start * SR:end * SR] += tiled
    music *= 0.20
    all_audio += music
    effects = [(0.08, "Target_NeonTap.wav"), (4.05, "Round_Start.wav"), (9.10, "Target_NeonTap.wav"), (11.0, "Target_Quick.wav"), (15.10, "Fever_Start.wav"), (21.15, "Target_TimeBonus.wav"), (27.2, "Target_Bomb.wav"), (33.08, "Speed_Up.wav"), (39.08, "Target_NeonTap.wav"), (45.13, "Target_Quick.wav"), (52.08, "Button_Click.wav")]
    sfx_dir = ROOT / "Assets/Resources/Sounds/SFX"
    for when, name in effects:
        a = decode_audio(sfx_dir / name) * 0.24
        at = round(when * SR)
        n = min(len(a), total - at)
        all_audio[at:at+n] += a[:n]
    phrases = json.loads((PROD / "Voice/script.json").read_text(encoding="utf-8-sig"))[locale]
    for i, _ in enumerate(phrases):
        a = decode_audio(PROD / "Voice" / f"{locale}-{i:02}.wav")
        at = round(voice_starts[i] * SR)
        n = min(len(a), total - at)
        if n <= 0:
            raise ValueError("Narration exceeds video")
        # Duck the music while the narrator speaks without attenuating SFX.
        all_audio[at:at+n] -= music[at:at+n] * 0.38
        all_audio[at:at+n] += a[:n] * (1.03 if locale == "KO" else 0.95)
    peak = float(np.max(np.abs(all_audio)))
    if peak > 0.96:
        all_audio *= 0.96 / peak
    all_audio[-SR // 3:] *= np.linspace(1, 0, SR // 3)[:, None]
    pcm = (np.clip(all_audio, -0.999, 0.999) * 32767).astype("<i2")
    dest = TEMP / f"mix-{locale}.wav"
    with wave.open(str(dest), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"AUDIO {locale}: peak={float(np.max(np.abs(pcm)))/32768:.3f}, rms={float(np.sqrt(np.mean((pcm.astype(np.float32)/32768)**2))):.3f}", flush=True)
    return dest


def finish_video(locale: str, clips: list[Path], audio: Path) -> None:
    listfile = TEMP / f"concat-{locale}.txt"
    listfile.write_text("".join(f"file '{p.as_posix()}'\n" for p in clips), encoding="utf-8")
    video = TEMP / f"video-{locale}.mp4"
    run([FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", listfile, "-c", "copy", "-movflags", "+faststart", video])
    final = OUT / f"VioletTap_Promo_{locale}_1080x1920_60s.mp4"
    run([FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-i", video, "-i", audio, "-map", "0:v:0", "-map", "1:a:0", "-c:v", "copy", "-c:a", "libvo_aacenc", "-b:a", "192k", "-ar", str(SR), "-ac", "2", "-t", "59.9", "-movflags", "+faststart", final])
    print(f"FINISHED {final} {final.stat().st_size:,} bytes", flush=True)


def render_locale(locale: str) -> None:
    create_base()
    phrases = json.loads((PROD / "Voice/script.json").read_text(encoding="utf-8-sig"))
    voice_starts = make_srt(locale, phrases[locale])
    footage = gameplay_shots(locale)
    clips = [render_segment(locale, i, phrases[locale][i], TEMP / "base.png", footage) for i in range(10)]
    audio = mix_audio(locale, voice_starts)
    finish_video(locale, clips, audio)


def main() -> None:
    for locale in ("KO", "EN"):
        render_locale(locale)


if __name__ == "__main__":
    main()
