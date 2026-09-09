from pathlib import Path
import hashlib, json, re, subprocess, tempfile, zipfile

root = Path(__file__).resolve().parent.parent
android = Path('C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Data/PlaybackEngines/AndroidPlayer')
readelf = android / 'NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-readelf.exe'
aab = root / 'w01.aab'
destination = root / 'output/release-symbols'
destination.mkdir(parents=True, exist_ok=True)

def inspect(path):
    notes = subprocess.check_output([str(readelf), '-n', str(path)], text=True)
    match = re.search(r'Build ID: (\w+)', notes)
    sections = subprocess.check_output([str(readelf), '-S', str(path)], text=True)
    return (match.group(1) if match else None, '.symtab' in sections)

matched = []
missing = []
with tempfile.TemporaryDirectory() as temporary, zipfile.ZipFile(aab) as bundle:
    for entry in bundle.namelist():
        if not entry.startswith('base/lib/') or not entry.endswith('.so'):
            continue
        abi, name = entry.split('/')[-2:]
        binary = Path(temporary) / name
        binary.write_bytes(bundle.read(entry))
        build_id, _ = inspect(binary)
        candidates = list((root / 'Library/Bee/artifacts').rglob(name.replace('.so', '.sym.so')))
        candidates += list((android / 'Variations/il2cpp').glob(f'*/Symbols/{abi}/{name.replace(".so", ".sym.so")}'))
        candidates += list((root / 'Library/Bee/Android/Prj/IL2CPP/Gradle').glob(f'*/build/intermediates/merged_native_libs/release/*/out/lib/{abi}/{name}'))
        for candidate in candidates:
            candidate_id, has_symbols = inspect(candidate)
            if build_id and candidate_id == build_id and has_symbols:
                matched.append((f'{abi}/{name}', candidate, build_id))
                break
        else:
            missing.append(entry)

assert any(name.endswith('/libil2cpp.so') for name, _, _ in matched), 'No matching IL2CPP symbols'
assert any(name.endswith('/libunity.so') for name, _, _ in matched), 'No matching Unity symbols'
digest = hashlib.sha256(aab.read_bytes()).hexdigest()
output = destination / f'w01-{digest[:12]}.symbols.zip'
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
    for name, source, _ in matched:
        archive.write(source, name)
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None
report = {'aab': str(aab), 'aab_sha256': digest, 'symbols_zip': str(output),
          'verified_build_ids': {name: build_id for name, _, build_id in matched},
          'libraries_without_available_symbols': missing}
output.with_suffix('.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
