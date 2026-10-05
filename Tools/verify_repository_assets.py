"""Verify changed PNG pixels and that removed reference GUIDs are unused."""
import io
import re
import subprocess
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
changes = subprocess.check_output(['git', 'diff', '--name-only', 'HEAD'], cwd=root).decode().splitlines()
verified = 0
removed_guids = []
for name in changes:
    path = root / name
    if name.endswith('.png') and path.is_file():
        before = subprocess.check_output(['git', 'show', 'HEAD:' + name], cwd=root)
        with Image.open(io.BytesIO(before)) as old, Image.open(path) as new:
            assert (old.mode, old.size, old.tobytes()) == (new.mode, new.size, new.tobytes()), name
            assert old.info == new.info, f'Metadata changed: {name}'
            verified += 1
    elif name.endswith('.png.meta') and not path.exists():
        old = subprocess.check_output(['git', 'show', 'HEAD:' + name], cwd=root).decode()
        removed_guids.extend(re.findall(r'^guid: (\w+)', old, re.M))
for path in (root / 'Assets').rglob('*'):
    if path.suffix not in ('.unity', '.prefab', '.asset', '.mat', '.cs'):
        continue
    content = path.read_bytes()
    for guid in removed_guids:
        assert guid.encode() not in content, f'Removed asset referenced by {path}'
print(f'REPOSITORY_ASSETS_PASS pixel_identical_pngs={verified} unused_removed_guids={len(removed_guids)}')
