"""Restore large editable Blender originals without Git LFS; never overwrite work."""
from pathlib import Path
import hashlib
import json
import argparse

root = Path(__file__).resolve().parents[1]
archive = root / 'ArtSource/PortableArchives20261014'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--verify-only', action='store_true')
args = parser.parse_args()

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1048576), b''):
            h.update(block)
    return h.hexdigest()

for item in json.loads((archive / 'manifest.json').read_text(encoding='utf-8')):
    target = (root / item['original']).resolve()
    if not target.is_relative_to(root.resolve()):
        raise RuntimeError('Invalid output path')
    combined = hashlib.sha256()
    size = 0
    for part in item['parts']:
        path = (archive / part['file']).resolve()
        if not path.is_relative_to(archive.resolve()):
            raise RuntimeError('Invalid part path')
        data = path.read_bytes()
        if len(data) != part['bytes'] or hashlib.sha256(data).hexdigest() != part['sha256']:
            raise RuntimeError('Damaged part: ' + str(path))
        combined.update(data)
        size += len(data)
    if size != item['bytes'] or combined.hexdigest() != item['sha256']:
        raise RuntimeError('Combined checksum mismatch')
    if args.verify_only:
        print('VERIFIED', item['original'])
        continue
    if target.exists():
        if digest(target) != item['sha256']:
            raise RuntimeError('Existing edited original preserved: ' + str(target))
        print('ALREADY PRESENT', item['original'])
        continue
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_suffix(target.suffix + '.restoring')
    with temporary.open('xb') as output:
        for part in item['parts']:
            with (archive / part['file']).open('rb') as stream:
                for block in iter(lambda: stream.read(1048576), b''):
                    output.write(block)
    if digest(temporary) != item['sha256']:
        raise RuntimeError('Restored checksum mismatch; temporary file retained')
    temporary.rename(target)
    print('RESTORED', item['original'])
