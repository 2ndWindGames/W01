"""Read-only Unity resource/reference audit. No asset imports or modifications."""
from pathlib import Path
from collections import defaultdict
import json
import re
import sys

root = Path(__file__).resolve().parents[2]
resource_root = root / "Assets/Resources"
guid_pattern = re.compile(r"\bguid: ([0-9a-f]{32})\b")
guid_paths = defaultdict(list)
for folder in (root / "Assets", root / "LocalPackages", root / "Library/PackageCache"):
    for meta in folder.rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8", errors="replace"), re.M)
        if match:
            guid_paths[match[1]].append(meta.with_suffix("").relative_to(root).as_posix())

resources = [p for p in resource_root.rglob("*") if p.is_file() and p.suffix != ".meta"]
keys = defaultdict(list)
for path in resources:
    keys[path.relative_to(resource_root).with_suffix("").as_posix().lower()].append(path.relative_to(root).as_posix())

reference_roots = [
    "Assets/Resources/References",
    "Assets/Resources/UI/NeonSignalPack/Concepts",
    "Assets/Resources/UI/NeonSignalPack/Sources",
]
def is_reference(path):
    return any(path == folder or path.startswith(folder + "/") for folder in reference_roots)

reference_guids = {guid: paths[0] for guid, paths in guid_paths.items() if is_reference(paths[0])}
external_reference_uses = []
missing_guids = []
serialized_extensions = {".unity", ".prefab", ".asset", ".mat", ".controller", ".anim", ".playable"}
for path in (root / "Assets").rglob("*"):
    if not path.is_file() or path.suffix not in serialized_extensions:
        continue
    relative = path.relative_to(root).as_posix()
    content = path.read_text(encoding="utf-8", errors="replace")
    if not content.startswith("%YAML"):
        continue
    for guid in set(guid_pattern.findall(content)):
        if guid in reference_guids and not is_reference(relative):
            external_reference_uses.append({"from": relative, "to": reference_guids[guid]})
        if guid not in guid_paths and not guid.startswith("0000000000000000"):
            missing_guids.append({"from": relative, "guid": guid})

literal_loads = []
missing_literal_loads = []
for path in (root / "Assets/01.Scripts").rglob("*.cs"):
    for key in re.findall(r'Resources\.Load(?:<[^>]+>)?\(\s*"([^"]+)"\s*\)', path.read_text(encoding="utf-8")):
        item = {"from": path.relative_to(root).as_posix(), "key": key}
        literal_loads.append(item)
        if key.lower() not in keys:
            missing_literal_loads.append(item)

report = {
    "resource_file_count": len(resources),
    "source_bytes": sum(p.stat().st_size for p in resources),
    "duplicate_resource_keys": {key: paths for key, paths in keys.items() if len(paths) > 1},
    "missing_meta": [p.relative_to(root).as_posix() for p in resources if not Path(str(p) + ".meta").exists()],
    "duplicate_asset_guids": {guid: [p for p in paths if p.startswith("Assets/")]
        for guid, paths in guid_paths.items() if sum(p.startswith("Assets/") for p in paths) > 1},
    "missing_serialized_guids": missing_guids,
    "literal_resource_loads": literal_loads,
    "missing_literal_loads": missing_literal_loads,
    "reference_sources": [{"path": folder,
        "files": sum(p.relative_to(root).as_posix().startswith(folder + "/") for p in resources),
        "bytes": sum(p.stat().st_size for p in resources if p.relative_to(root).as_posix().startswith(folder + "/"))}
        for folder in reference_roots],
    "reference_sources_used_elsewhere": external_reference_uses,
}
destination = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "output/qa-2026-09-13/resource-audit.json"
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
print(json.dumps({k: v for k, v in report.items() if k != "literal_resource_loads"}, indent=2, ensure_ascii=False))
