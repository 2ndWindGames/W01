from pathlib import Path
import shutil
import numpy as np
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parents[2]
target = root / 'Assets/Resources/UI/Intro/Buttons/btn_back_normal.png'
backup = root / 'output/imagegen/btn_back_before_redesign.png'
if not backup.exists():
    shutil.copy2(target, backup)
source = Path('C:/Users/rlatmdwp5/.codex/generated_images/01a083c4-9867-7690-bd0b-26bd52d742a8/exec-65cecf0a-4f54-421e-aa0f-a8fa2a7b4ec6.png')
rgb = np.array(Image.open(source).convert('RGB'))
v = rgb.astype(np.int16)
green = (v[:,:,1] > v[:,:,0]+25) & (v[:,:,1] > v[:,:,2]+25)
alpha = Image.fromarray((~green).astype(np.uint8)*255).filter(ImageFilter.MinFilter(3))
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
preview.convert('RGB').save(root/'output/imagegen/back_redesign_preview.png')
print('Saved 512x512 transparent RGBA. Existing Unity metadata preserved.')
