from pathlib import Path
import re
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).resolve().parents[2]
source = Path('C:/Users/rlatmdwp5/.codex/generated_images/01a083c4-9867-7690-bd0b-26bd52d742a8')
assets = [
    ('exec-b193828a-af8d-427a-8665-e526dc7987dd.png', 'Assets/Resources/UI/Intro/Buttons/btn_back_normal.png', True),
    ('exec-a0b21170-9515-45aa-abc8-3b31d53e7317.png', 'Assets/Resources/UI/Intro/Icons/icon_ranking.png', False),
]
for original, target, fill in assets:
    rgb = np.array(Image.open(source / original).convert('RGB'))
    chroma = rgb.max(axis=2).astype(int) - rgb.min(axis=2).astype(int)
    mask = chroma > 20
    mask_image = Image.fromarray(mask.astype(np.uint8) * 255).filter(ImageFilter.MedianFilter(3))
    if fill:
        ImageDraw.floodfill(mask_image, (0, 0), 128)
        mask = np.array(mask_image) != 128
    else:
        mask = np.array(mask_image) > 0
    # Keep colored art, remove the neutral checkerboard including trophy handle holes.
    rgba = np.dstack((rgb, mask.astype(np.uint8) * 255))
    result = Image.fromarray(rgba).resize((512, 512), Image.Resampling.LANCZOS)
    path = root / target
    result.save(path)
    meta = path.with_suffix('.png.meta')
    text = meta.read_text(encoding='utf-8')
    for key, value in {'enableMipMap': 0, 'wrapU': 1, 'wrapV': 1,
                       'nPOTScale': 0, 'spriteMode': 1, 'alphaIsTransparency': 1,
                       'textureType': 8}.items():
        text = re.sub(rf'(?m)^(\s*{key}:) .*$', rf'\g<1> {value}', text)
    meta.write_text(text, encoding='utf-8')
    alpha = np.array(result.getchannel('A'))
    assert alpha[0, 0] == 0 and alpha.max() == 255
    print(f'{target}: {result.size}, {result.mode}, transparent pixels={np.sum(alpha == 0)}')
