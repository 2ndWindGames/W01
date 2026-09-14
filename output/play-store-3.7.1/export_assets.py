"""Export approved art and genuine Unity Game View captures to Play dimensions."""

from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent


def export_icon() -> None:
    source = ROOT / "Assets/Resources/Icon/VioletTapAppIcon.png"
    with Image.open(source) as image:
        icon = image.convert("RGBA").resize((512, 512), Image.Resampling.LANCZOS)
        assert icon.getchannel("A").getextrema() == (255, 255)
        icon.save(OUT / "icon-512.png", optimize=True)


def export_feature() -> None:
    source = OUT / "feature-source.png"
    with Image.open(source) as image:
        feature = image.convert("RGB").resize((1024, 500), Image.Resampling.LANCZOS)
        feature.save(OUT / "feature-1024x500.png", optimize=True)


def export_phone(locale: str, number: int, label: str, source: str, top: int) -> None:
    path = ROOT / source
    dest = OUT / locale
    dest.mkdir(parents=True, exist_ok=True)
    with Image.open(path) as image:
        assert image.size == (1080, 2340), (path, image.size)
        screenshot = image.crop((0, top, 1080, top + 1920)).convert("RGB")
        screenshot.save(dest / f"phone-{number:02d}-{label}.png", optimize=True)


def validate_and_package() -> None:
    icon_path = OUT / "icon-512.png"
    feature_path = OUT / "feature-1024x500.png"
    phone_paths = sorted((OUT / "en-US").glob("phone-*.png")) + sorted(
        (OUT / "ko-KR").glob("phone-*.png")
    )
    assert len(phone_paths) == 10, len(phone_paths)

    with Image.open(icon_path) as icon:
        icon.load()
        assert icon.size == (512, 512) and icon.mode == "RGBA"
        assert icon.getchannel("A").getextrema() == (255, 255)
    assert icon_path.stat().st_size <= 1024 * 1024

    with Image.open(feature_path) as feature:
        feature.load()
        assert feature.size == (1024, 500) and feature.mode == "RGB"

    for path in phone_paths:
        with Image.open(path) as screenshot:
            screenshot.load()
            width, height = screenshot.size
            assert screenshot.mode == "RGB" and (width, height) == (1080, 1920)
            assert 320 <= min(width, height) and max(width, height) <= 3840
            assert max(width, height) <= 2 * min(width, height)

    archive_path = OUT.parent / "play-store-3.7.1-upload.zip"
    with ZipFile(archive_path, "w", compression=ZIP_DEFLATED, compresslevel=6) as archive:
        for path in [icon_path, feature_path, *phone_paths]:
            archive.write(path, path.relative_to(OUT).as_posix())
    print(f"Validated 12 Play assets; upload archive: {archive_path}")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    export_icon()
    export_feature()
    localized = {
        "en-US": [
            (1, "fever", "output/qa-2026-09-13/run-20260914-204118/fever.png", 20),
            (2, "combo-50", "output/qa-2026-09-13/run-20260914-202952/combo-50-yellow.png", 20),
            (3, "neon-targets", "output/qa-2026-09-13/run-20260914-203014/caption-fever-english.png", 20),
            (4, "intro", "output/qa-2026-09-13/run-20260914-204118/intro.png", 0),
        ],
        "ko-KR": [
            (1, "fever", "output/qa-2026-09-13/run-20260914-214004/fever.png", 20),
            (2, "combo-50", "output/qa-2026-09-13/run-20260914-213844/combo-50-yellow.png", 20),
            (3, "neon-targets", "output/qa-2026-09-13/run-20260914-203014/caption-fever-korean.png", 20),
            (4, "intro", "output/qa-2026-09-13/run-20260914-214004/intro.png", 0),
        ],
    }
    for locale, captures in localized.items():
        for number, label, source, top in captures:
            export_phone(locale, number, label, source, top)
        guide = "help-compact-english.png" if locale == "en-US" else "help-compact-korean.png"
        export_phone(locale, 5, "guide", f"output/qa-2026-09-13/run-20260914-202832/{guide}", 300)
    validate_and_package()


if __name__ == "__main__":
    main()
