from pathlib import Path
import re
import uuid
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).resolve().parents[2]
source = Path('C:/Users/rlatmdwp5/.codex/generated_images/01a083c4-9867-7690-bd0b-26bd52d742a8')
destination = root / 'Assets/Resources/UI/Popup'
template = (root / 'Assets/Resources/UI/Intro/Buttons/btn_back_normal.png.meta').read_text(encoding='utf-8')
assets = [
    ('exec-8b83b363-0599-4511-a772-b8734b513366.png', 'checkbox_background.png', 448),
    ('exec-80b40dc2-93e3-44f1-acda-692537efc16e.png', 'checkbox_checkmark.png', 290),
]
images = []
for original, name, width in assets:
    rgb = np.array(Image.open(source / original).convert('RGB'))
    chroma = rgb.max(axis=2).astype(int) - rgb.min(axis=2).astype(int)
    mask = Image.fromarray((chroma > 25).astype(np.uint8) * 255).filter(ImageFilter.MedianFilter(3))
    ImageDraw.floodfill(mask, (0, 0), 128)
    alpha = (np.array(mask) != 128).astype(np.uint8) * 255
    rgba = Image.fromarray(np.dstack((rgb, alpha)))
    rgba = rgba.crop(rgba.getbbox())
    rgba = rgba.resize((width, round(rgba.height * width / rgba.width)), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (512, 512))
    canvas.alpha_composite(rgba, ((512-rgba.width)//2, (512-rgba.height)//2))
    path = destination / name
    canvas.save(path)
    meta = path.with_suffix('.png.meta')
    if not meta.exists():
        meta.write_text(re.sub(r'(?m)^guid: .*$', 'guid: '+uuid.uuid4().hex, template), encoding='utf-8')
    assert canvas.getpixel((0,0))[3] == 0
    images.append(canvas)
    print(f'{path}: 512x512 RGBA, bounds={canvas.getbbox()}')
preview = Image.new('RGBA', (1024, 512), '#171126')
preview.alpha_composite(images[0], (0, 0))
preview.alpha_composite(images[0], (512, 0))
preview.alpha_composite(images[1], (512, 0))
preview.convert('RGB').save(root / 'output/imagegen/checkbox_preview.png')
