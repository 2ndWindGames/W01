"""Build only the revised failure and pace cues; preserve the other audio assets."""
from pathlib import Path
import json
from generate_violettap_audio import Mix, sweep

root = Path(__file__).resolve().parents[2] / "Assets/Resources/Sounds/SFX"
miss = Mix("Target_Miss", .32)
miss.add(sweep(540, 140, .27, .3), 0)
miss.drum("snare", 0, .32)
broken = Mix("Combo_Break", .65)
broken.drum("kick", 0, .5)
broken.drum("snare", .03, .42)
broken.add(sweep(950, 110, .42, .4), .01)
for i, note in enumerate((76, 69, 60)):
    broken.note(note, .05 + i * .06, .35, .27, (i - 1) * .25)
broken.delay(.07, .12, 2)
speed = Mix("Speed_Up", .48)
for i, note in enumerate((76, 81, 88)):
    speed.note(note, i * .065, .24, .3, (i - 1) * .2, "pluck")
speed.add(sweep(280, 1400, .27, .13), 0)
print(json.dumps([clip.save(root, .68) for clip in (miss, broken, speed)], indent=2))
