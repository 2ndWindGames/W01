"""Compile package boundaries against installed Unity references without relying on IDE refresh."""
from pathlib import Path
import copy, subprocess, xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
out = root / 'output/core-validation/compile'
out.mkdir(parents=True, exist_ok=True)
ns = {'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
ET.register_namespace('', ns['m'])
tag = lambda s: '{'+ns['m']+'}'+s
template = ET.parse(root/'Assembly-CSharp.csproj').getroot()
packages = root/'LocalPackages/SWGUnity2DCore/Packages'
jobs = [
    ('SWGUnity2DCore', packages/'com.secondwind.core/Runtime', []),
    ('SWGUnity2DCore.Ads.AdMob', packages/'com.secondwind.ads.admob/Runtime', []),
    ('SWGUnity2DCore.Leaderboards.UGS', packages/'com.secondwind.leaderboards.ugs/Runtime', []),
    ('W01.Content', root/'Assets/01.Scripts', ['SWGUnity2DCore','SWGUnity2DCore.Ads.AdMob','SWGUnity2DCore.Leaderboards.UGS'])
]
for name, source, dependencies in jobs:
    project = copy.deepcopy(template)
    for group in project.findall('m:ItemGroup',ns):
        for item in list(group):
            kind=item.tag.split('}')[-1]
            if kind not in ('Reference',):
                group.remove(item)
                continue
            hint=item.find('m:HintPath',ns)
            if hint is not None:
                p=Path(hint.text)
                if not p.is_absolute(): p=root/p
                hint.text=str(p)
                ref=item.attrib['Include']
                keep=not ref.startswith(('SWGUnity','Assembly-CSharp'))
                if name == 'SWGUnity2DCore':
                    keep=keep and (ref.startswith(('UnityEngine','UnityEditor','System','netstandard','mscorlib')) or ref in ('Unity.ugui','Unity.TextMeshPro'))
                if not keep or not p.exists(): group.remove(item)
    for e in project.findall('.//m:AssemblyName',ns): e.text=name
    for e in project.findall('.//m:OutputPath',ns): e.text=str(out/'bin'/name)+'/'
    props=ET.SubElement(project,tag('PropertyGroup'))
    ET.SubElement(props,tag('BaseIntermediateOutputPath')).text=str(out/'obj'/name)+'/'
    ET.SubElement(props,tag('IntermediateOutputPath')).text=str(out/'obj'/name)+'/'
    group=ET.SubElement(project,tag('ItemGroup'))
    for path in source.rglob('*.cs'): ET.SubElement(group,tag('Compile'),{'Include':str(path)})
    for dep in dependencies:
        ref=ET.SubElement(group,tag('Reference'),{'Include':dep})
        ET.SubElement(ref,tag('HintPath')).text=str(out/'bin'/dep/(dep+'.dll'))
    file=out/(name+'.csproj')
    ET.ElementTree(project).write(file,encoding='utf-8',xml_declaration=True)
    print('Compiling '+name,flush=True)
    subprocess.run(['dotnet','build',str(file),'--no-restore','--verbosity','quiet'],cwd=root,check=True)
