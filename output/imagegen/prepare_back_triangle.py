from pathlib import Path
import shutil
import numpy as np
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parents[2]
target = root / 'Assets/Resources/UI/Intro/Buttons/btn_back_normal.png'
backup = root / 'output/imagegen/btn_back_before_triangle.png'
if not backup.exists():
    shutil.copy2(target, backup)
source = Path('C:/Users/rlatmdwp5/.codex/generated_images/01a083c4-9867-7690-bd0b-26bd52d742a8/exec-6f267097-a16f-4652-a159-4ef88a59b620.png')
rgb = np.array(Image.open(source).convert('RGB'))
v = rgb.astype(np.int16)
green = (v[:,:,1] > v[:,:,0]+25) & (v[:,:,1] > v[:,:,2]+25)
alpha = Image.fromarray((~green).astype(np.uint8)*255).filter(ImageFilter.MinFilter(3))
# Smoked-glass fill at 45% opacity; brighter outline remains 85% opaque.
coverage = np.array(alpha).astype(float)/255
brightness = rgb.max(axis=2).astype(float)
opacity = 0.45 + 0.40*np.clip((brightness-65)/45,0,1)
alpha = Image.fromarray(np.round(coverage*opacity*255).astype(np.uint8))
icon = Image.fromarray(rgb).convert('RGBA')
icon.putalpha(alpha)
icon = icon.crop(icon.getbbox())
icon = icon.resize((round(icon.width*360/icon.height),360), Image.Resampling.LANCZOS)
result = Image.new('RGBA',(512,512))
result.alpha_composite(icon,((512-icon.width)//2,76))
result.save(target)
assert result.getpixel((0,0))[3] == 0
preview = Image.new('RGBA',(256,256),'#171126')
preview.alpha_composite(result.resize((256,256),Image.Resampling.LANCZOS))
preview.convert('RGB').save(root/'output/imagegen/back_triangle_preview.png')
print('Saved 512x512 transparent RGBA. Existing Unity metadata preserved.')
