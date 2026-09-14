"""Decode and inspect the finished Korean and English promo media."""
from __future__ import annotations

import json
from pathlib import Path
import re
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Deliverables/Promo60"
PROD = OUT / "Production"
TEMP = PROD / "render-work"
TEMP.mkdir(parents=True, exist_ok=True)
FFMPEG = Path(r"C:\KMPlayer\ffmpeg.exe")
TIMES = [2, 6.5, 12, 18, 24, 30, 36, 42, 48.5, 56]


def call(args: list[str], text: bool = False) -> subprocess.CompletedProcess:
    return subprocess.run([str(x) for x in args], capture_output=True, check=True, text=text)


def inspect(locale: str) -> dict:
    src = OUT / f"VioletTap_Promo_{locale}_1080x1920_60s.mp4"
    probe = call([FFMPEG, "-hide_banner", "-i", src, "-f", "null", "NUL"], text=True)
    meta = probe.stderr
    duration = float(re.search(r"Duration: (\d+):(\d+):(\d+\.\d+)", meta).group(3))
    video = re.search(r"Video: h264[^\n]*", meta).group(0)
    audio = re.search(r"Audio: aac[^\n]*", meta).group(0)
    assert 59 <= duration <= 60, duration
    assert "1080x1920" in video and "yuv420p" in video and "24 fps" in video, video
    assert "48000 Hz" in audio and "stereo" in audio, audio
    pcm = call([FFMPEG, "-hide_banner", "-loglevel", "error", "-i", src, "-vn", "-ar", "48000", "-ac", "2", "-f", "s16le", "-"]).stdout
    samples = np.frombuffer(pcm, dtype="<i2").astype(np.float32) / 32768
    peak = float(np.max(np.abs(samples)))
    rms = float(np.sqrt(np.mean(samples**2)))
    assert peak < 0.99 and rms > 0.01, (peak, rms)
    block = 48000 // 4 * 2  # 250 ms, stereo interleaved
    windows = samples[:len(samples) // block * block].reshape(-1, block)
    window_rms = np.sqrt(np.mean(windows**2, axis=1))
    longest_silent_blocks = 0
    current_silent_blocks = 0
    for level in window_rms:
        current_silent_blocks = current_silent_blocks + 1 if level < 0.001 else 0
        longest_silent_blocks = max(longest_silent_blocks, current_silent_blocks)
    assert longest_silent_blocks < 2, longest_silent_blocks
    srt = (OUT / f"VioletTap_Promo_{locale}.srt").read_text(encoding="utf-8-sig")
    assert srt.count(" --> ") == 10
    cards = []
    for i, t in enumerate(TIMES):
        frame = TEMP / f"check-{locale}-{i}.jpg"
        call([FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-ss", str(t), "-i", src, "-frames:v", "1", frame])
        picture = Image.open(frame).convert("RGB")
        assert picture.size == (1080, 1920)
        cards.append(picture.resize((300, 533), Image.Resampling.LANCZOS))
    sheet = Image.new("RGB", (1520, 1128), (8, 12, 25))
    draw = ImageDraw.Draw(sheet)
    face = ImageFont.truetype(str(ROOT / "Assets/Resources/Fonts/Candidates/Eng/exo2/Exo2-Regular.ttf"), 22)
    for i, card in enumerate(cards):
        x, y = (i % 5) * 304, (i // 5) * 564
        sheet.paste(card, (x, y))
        draw.text((x + 8, y + 539), f"{i+1:02} / {TIMES[i]:.1f}s", font=face, fill="white")
    sheet.save(PROD / f"Preview_{locale}.jpg", quality=91, optimize=True)
    return {"duration_seconds": duration, "resolution": "1080x1920", "fps": 24, "video": "H.264 yuv420p", "audio": "AAC stereo 48 kHz", "decoded": True, "audio_peak": round(peak, 3), "audio_rms": round(rms, 3), "longest_silent_window_seconds": longest_silent_blocks * 0.25, "srt_cues": 10, "bytes": src.stat().st_size}


def main() -> None:
    info = {locale: inspect(locale) for locale in ("KO", "EN")}
    (PROD / "validation.json").write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding="utf-8")
    report = """# 홍보 영상 검증 기록

두 최종 MP4를 전체 디코딩해 오류가 없는지 확인하고, 각 컷의 대표 프레임과 최종 AAC 오디오를 검사했습니다.

| 항목 | 한국어 | English |
|---|---:|---:|
| 길이 | {ko_d:.2f}초 | {en_d:.2f}초 |
| 화면 | 1080×1920, 9:16 | 1080×1920, 9:16 |
| 비디오 | H.264 / yuv420p / 24fps | H.264 / yuv420p / 24fps |
| 오디오 | AAC / 스테레오 / 48kHz | AAC / 스테레오 / 48kHz |
| 전체 디코딩 | 성공 | 성공 |
| 음성·음악 포함 최종 오디오 피크 | {ko_p:.3f} | {en_p:.3f} |
| 오디오 RMS | {ko_r:.3f} | {en_r:.3f} |
| 연속 무음 구간 | {ko_s:.2f}초 | {en_s:.2f}초 |
| 자막 SRT | 10개 음성 구간 | 10개 음성 구간 |

대표 프레임: [한국어](Preview_KO.jpg), [English](Preview_EN.jpg). 제목과 자막은 프레임 경계 안에 있으며, 포인트·콤보·타겟을 가리는 위치를 확인했습니다. 화면은 원본 종횡비를 유지한 실제 게임 캡처와 클로즈업을 사용합니다. 중요 자막은 플랫폼의 하단 버튼 영역보다 위에 배치했습니다.

한국어 음성은 Windows Microsoft Heami Desktop, 영어 음성은 Microsoft Zira Desktop으로 합성했습니다. 두 영상에 프로젝트 내 Gameplay 음악과 게임 효과음을 믹스했습니다. 최종 AAC에서 무음이나 디지털 클리핑은 검출되지 않았습니다. 사람 성우 녹음이나 전문 성우 감수를 의미하지 않습니다.

촬영 원본: `output/qa-2026-09-13/gameplay-video/`의 Unity Game View 플레이 영상 2개 및 `output/play-store-3.7.1/`의 한·영 게임 캡처. QA 플레이 영상은 촬영 당시 타겟 수명을 메모리에서 연장한 기록이 있어, 화면은 실제 게임 렌더링이지만 일반 플레이 속도의 실측 자료로 사용하면 안 됩니다. 한국어 50콤보 컷은 해당 영상의 실제 프레임을 추출했습니다. 영문 50콤보 컷 등 일부 이미지는 기존 스토어용 QA 캡처입니다. 캐릭터는 현재 게임 플레이에 존재하지 않아 임의로 추가하지 않았습니다.

프로필의 실제 ‘네온터치(VioletTap)’ 다운로드 링크가 스토어로 연결되는지는 제공된 계정 정보만으로 검증할 수 없었습니다. 게시 전에 채널·인스타그램 각각에서 해당 게임 링크를 눌러 확인해야 합니다.
""".format(ko_d=info["KO"]["duration_seconds"], en_d=info["EN"]["duration_seconds"], ko_p=info["KO"]["audio_peak"], en_p=info["EN"]["audio_peak"], ko_r=info["KO"]["audio_rms"], en_r=info["EN"]["audio_rms"], ko_s=info["KO"]["longest_silent_window_seconds"], en_s=info["EN"]["longest_silent_window_seconds"])
    (PROD / "Validation.md").write_text(report, encoding="utf-8")
    print(json.dumps(info, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
