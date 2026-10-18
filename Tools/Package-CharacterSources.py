from pathlib import Path
import zipfile,hashlib,json,shutil
P=Path(__file__).resolve().parents[1]
A=P/'ArtSources'/'20261019';A.mkdir(parents=True,exist_ok=True)
files=[P/'LocalArchive/CaoCaoHairEnemies20261016/Revision16/CaoCao_ArtRefinement.blend',P/'LocalArchive/SeolpyeongFace20261019/Seolpyeong_Face_Workbench.blend']
files+=sorted((P/'LocalArchive/CharacterParts20261018').glob('*/*_Parts_Workbench.blend'))
b=P/'LocalArchive/CaoCaoHairEnemies20261016/Revision2'
files+=sorted(b.glob('seol_face_*.py'))
for root in [P/'LocalArchive/SeolpyeongFace20261019',P/'LocalArchive/CaoCaoHairEnemies20261016/Revision16']:
 files+=sorted(f for f in root.iterdir() if f.is_file() and f.suffix in ('.py','.json','.txt'))
files=list(dict.fromkeys(files))
manifest={'format':'zip-split-v1','part_bytes':42*1024*1024,'files':[],'parts':[]}
z=P/'LocalArchive/characters-source-20261019.zip'
with zipfile.ZipFile(z,'w',zipfile.ZIP_DEFLATED,compresslevel=6,allowZip64=True) as out:
 for f in files:
  rel=f.relative_to(P).as_posix();out.write(f,rel)
  manifest['files'].append({'path':rel,'bytes':f.stat().st_size,'sha256':hashlib.file_digest(f.open('rb'),'sha256').hexdigest()})
  if f.suffix=='.blend':print('Packed '+rel,flush=True)
manifest['archive_sha256']=hashlib.file_digest(z.open('rb'),'sha256').hexdigest()
with z.open('rb') as src:
 i=1
 while chunk:=src.read(manifest['part_bytes']):
  name=f'characters-source.zip.part{i:03d}';(A/name).write_bytes(chunk)
  manifest['parts'].append({'name':name,'bytes':len(chunk),'sha256':hashlib.sha256(chunk).hexdigest()});i+=1
(A/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'blend_count':sum(f.suffix=='.blend' for f in files),'archive_bytes':z.stat().st_size,'parts':len(manifest['parts'])}))
