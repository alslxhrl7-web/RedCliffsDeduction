"""Losslessly recompress PNG assets; keep only smaller, pixel-identical results."""
from pathlib import Path
from io import BytesIO
from PIL import Image

root = Path(__file__).resolve().parents[1]
saved = changed = 0
for path in sorted((root / 'Assets/Art').rglob('*.png')):
    if path.name.startswith('Reference') and 'Characters' in path.parts:
        continue
    original = path.read_bytes()
    with Image.open(BytesIO(original)) as image:
        image.load()
        # Leave metadata-bearing files untouched to preserve color interpretation.
        if image.info:
            continue
        output = BytesIO()
        image.save(output, format='PNG', optimize=True, compress_level=9)
        candidate = output.getvalue()
        with Image.open(BytesIO(candidate)) as check:
            if check.mode != image.mode or check.size != image.size or check.tobytes() != image.tobytes():
                raise RuntimeError(f'Pixel mismatch: {path}')
        if len(candidate) < len(original):
            path.write_bytes(candidate)
            saved += len(original) - len(candidate)
            changed += 1
print(f'PNG_OPTIMIZATION_PASS changed={changed} saved_bytes={saved}')
