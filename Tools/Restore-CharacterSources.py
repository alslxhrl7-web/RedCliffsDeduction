"""Verify and restore the lossless editable character sources shipped in Git."""
from pathlib import Path
import argparse,hashlib,json,tempfile,zipfile,shutil

def main():
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('--verify-only',action='store_true')
 parser.add_argument('--destination',type=Path)
 args=parser.parse_args()
 root=Path(__file__).resolve().parents[1]
 bundle=root/'ArtSources/20261019'
 m=json.loads((bundle/'manifest.json').read_text(encoding='utf-8'))
 destination=(args.destination or root).resolve()
 with tempfile.TemporaryFile() as merged:
  total=hashlib.sha256()
  for part in m['parts']:
   p=bundle/part['name']
   if p.parent!=bundle or p.stat().st_size!=part['bytes']:raise ValueError('Invalid archive part')
   h=hashlib.sha256()
   with p.open('rb') as src:
    while block:=src.read(1024*1024):h.update(block);total.update(block);merged.write(block)
   if h.hexdigest()!=part['sha256']:raise ValueError('Part checksum mismatch: '+p.name)
  if total.hexdigest()!=m['archive_sha256']:raise ValueError('Archive checksum mismatch')
  merged.seek(0)
  with zipfile.ZipFile(merged) as z:
   expected={f['path']:f for f in m['files']}
   if set(z.namelist())!=set(expected):raise ValueError('Unexpected archive entries')
   for name,item in expected.items():
    target=(destination/name).resolve()
    if not target.is_relative_to(destination):raise ValueError('Unsafe archive path')
    with z.open(name) as src: digest=hashlib.file_digest(src,'sha256').hexdigest()
    if digest!=item['sha256'] or z.getinfo(name).file_size!=item['bytes']:raise ValueError('Source checksum mismatch: '+name)
    if args.verify_only:continue
    if target.exists():
     with target.open('rb') as src:existing=hashlib.file_digest(src,'sha256').hexdigest()
     if existing!=digest:raise FileExistsError('Preserving different existing source: '+str(target))
     continue
    target.parent.mkdir(parents=True,exist_ok=True)
    with z.open(name) as src,target.open('xb') as dst:shutil.copyfileobj(src,dst)
 print(('Verified' if args.verify_only else 'Restored')+f" {len(m['files'])} files, including {sum(f['path'].endswith('.blend') for f in m['files'])} Blender originals.")
if __name__=='__main__':main()
