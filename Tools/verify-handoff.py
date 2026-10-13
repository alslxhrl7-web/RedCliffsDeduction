"""Check the Git index, scene references, and included Blender sources before transfer.
Run from the repository root after Git LFS pull and Unity's initial package import.
Python standard library only; no files are edited.
"""
import json,re,subprocess,sys,zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
GUID=re.compile(rb'guid:\s*([0-9a-f]{32})')
git=lambda *args:subprocess.check_output(['git','-C',str(ROOT),*args])
tracked=set(git('ls-files','-z').decode('utf-8').split('\0'))-{''}
failures=[];guid_paths={};package_guids=set()
for relative in sorted(tracked):
    if relative.endswith('.meta'):
        path=ROOT/relative
        if not path.exists():failures.append('Missing tracked meta: '+relative);continue
        match=GUID.search(path.read_bytes())
        if match:
            guid=match.group(1)
            if relative.startswith('Assets/'):guid_paths[guid]=relative[:-5]
            else:package_guids.add(guid)
    if relative.startswith('Assets/') and not relative.endswith('.meta') and relative+'.meta' not in tracked:
        failures.append('Missing tracked meta: '+relative+'.meta')
for path in (ROOT/'Library/PackageCache').rglob('*.meta'):
    match=GUID.search(path.read_bytes())
    if match:package_guids.add(match.group(1))

queue=['Assets/Scenes/Investigation.unity'];visited=set();unresolved=set()
while queue:
    path=queue.pop()
    if path in visited:continue
    visited.add(path)
    if path not in tracked:failures.append('Untracked scene dependency: '+path);continue
    local=ROOT/path
    if not local.is_file():failures.append('Missing scene dependency: '+path);continue
    raw=local.read_bytes()
    if raw.startswith(b'version https://git-lfs.github.com/spec/v1'):
        failures.append('LFS pointer not downloaded: '+path);continue
    raw=(raw if raw.startswith(b'%YAML') or local.suffix in ('.mat','.prefab','.unity') else b'')
    meta=ROOT/(path+'.meta')
    if meta.exists():raw+=meta.read_bytes()
    for guid in GUID.findall(raw):
        if guid in guid_paths:
            target=guid_paths[guid]
            if (ROOT/target).is_file():queue.append(target)
        elif guid not in package_guids and not guid.startswith(b'0000000000000000'):
            unresolved.add(guid.decode())
if unresolved:failures.append('Unresolved GUIDs (complete Unity package import first): '+','.join(sorted(unresolved)))

masters=['CharacterDetail20261013/CharacterDetails.blend','RoleActivities20261013/RoleActivities.blend','HandRefinement20261013/Hands.blend','CommandDesk20261013/CommandDesk_Baked.blend','EnvironmentRefresh20261013/Environment_Set.blend']
for relative in masters:
    p=ROOT/'ArtSource'/relative
    if 'ArtSource/'+relative not in tracked or not p.exists() or p.stat().st_size<100000:
        failures.append('Missing Blender source / LFS download: '+relative)
archive=ROOT/'ArtSource/RoleActivities20261013/SourceInputs.zip'
try:
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None and len(z.namelist())==10
        for name in z.namelist():
            data=json.loads(z.read(name).decode('utf-8-sig'))
            for key in ('colorPath','normalPath'):
                if data[key] and (data[key] not in tracked or not (ROOT/data[key]).exists()):failures.append('Missing authoring texture: '+data[key])
except (OSError,ValueError,AssertionError,zipfile.BadZipFile) as e:failures.append('SourceInputs.zip invalid: '+str(e))
for p in tracked:
    if re.search(r'\.blend\d+$',p) or p.startswith(('Library/','Logs/','Temp/','Builds/')):failures.append('Generated file tracked: '+p)
print('Scene dependencies:',len(visited),'Blender masters:',len(masters))
for message in failures:print('FAIL',message)
print('COMPLETE failures='+str(len(failures)))
sys.exit(bool(failures))
