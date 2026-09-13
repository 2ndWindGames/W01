"""Read only the current build's signing values; pass them privately over stdin."""
import ast
import base64
import pathlib
import re
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
gradle = (root / 'Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build.gradle').read_text(encoding='utf-8')

def configured(name):
    match = re.search(r'^\s*' + re.escape(name) + r"\s+('(?:\\.|[^'\\])*')\s*$", gradle, re.MULTILINE)
    if not match:
        raise RuntimeError('Current signing field could not be parsed: ' + name)
    return ast.literal_eval(match.group(1))

payload = '\n'.join(base64.b64encode(configured(field).encode('utf-8')).decode('ascii')
                    for field in ('storePassword', 'keyPassword')) + '\n'
java = pathlib.Path('C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe')
result = subprocess.run([str(java), str(pathlib.Path(__file__).with_name('VerifySigningKey.java')),
                         str(root / 'user.keystore'), configured('keyAlias')],
                        input=payload, text=True, capture_output=True, timeout=40)
print(result.stdout.strip())
if result.returncode:
    print('Signing verifier failed with exit code', result.returncode)
    raise SystemExit(result.returncode)
