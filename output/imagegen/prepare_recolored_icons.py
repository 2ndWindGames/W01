from pathlib import Path
import shutil
import numpy as np
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parents[2]
src = Path('C:/Users/rlatmdwp5/.codex/generated_images/01a083c4-9867-7690-bd0b-26bd52d742a8')
dest = root / 'Assets/Resources/UI/Intro/Icons'
backup = root / 'output/imagegen/icons-before-recolor'
backup.mkdir(exist_ok=True)
assets = [
 ('exec-e3199728-2d09-40c5-96d0-d71b52094b9d.png', 'icon_ranking.png'),
 ('exec-dc92a35e-dd2e-4c2b-a13d-6e2abb87eaec.png', 'icon_settings.png'),
 ('exec-3e298b37-c83b-4292-906d-99664ebe7686.png', 'icon_sound_on.png'),
 ('exec-fc132a02-79d5-4257-8dcf-96ae312236e4.png', 'icon_sound_off.png'),
]
preview = Image.new('RGBA', (1024,256), '#191422')
for i, (source, name) in enumerate(assets):
    path = dest / name
    size = Image.open(path).size
    if not (backup / name).exists():
        shutil.copy2(path, backup / name)
    rgb = np.array(Image.open(src / source).convert('RGB'))
    v = rgb.astype(np.int16)
    green = (v[:,:,1] > v[:,:,0]+25) & (v[:,:,1] > v[:,:,2]+25)
    alpha = Image.fromarray((~green).astype(np.uint8)*255).filter(ImageFilter.MinFilter(3))
    image = Image.fromarray(rgb).convert('RGBA')
    image.putalpha(alpha)
    image = image.resize(size, Image.Resampling.LANCZOS)
    image.save(path)
    assert image.getpixel((0,0))[3] == 0
    preview.alpha_composite(image.resize((256,256), Image.Resampling.LANCZOS), (i*256,0))
    print(f'{name}: {image.size} {image.mode}; existing .meta preserved')
preview.convert('RGB').save(root / 'output/imagegen/recolored_icons_preview.png')
