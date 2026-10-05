# Repository cleanup — 2026-10-05

Removed 12 unused character reference PNGs and their Unity metadata, plus
15 audio preview/source media files from the current repository tree.
All removed media were copied and hash-verified in the ignored local backup
`LocalArchive/GitHubCleanup-2026-10-05`, before removal. The backup is local
and is not included when another machine clones the repository.

Retained the audio production scripts, imported game audio, character models,
role pose meshes, texture sources, five runtime evidence document images,
and the embedded Unity Pipeline package.

Losslessly recompressed 41 PNGs, saving 21,225,220 bytes. Verification against
the original commit confirmed identical decoded pixels, resolution, mode,
and image metadata for every changed PNG. Removed media total 39,868,483
bytes, excluding their small metadata files. Combined reduction is
61,093,703 bytes (58.26 MiB), excluding small added documentation/tools.
This is source checkout storage reduction, not a claim of reduced runtime
texture memory or compressed build size.

Validation: 12 removed reference GUIDs are unused in scenes, prefabs,
materials, serialized assets and scripts; Unity play regression passed
27 checks with zero failures on 2026-10-05.
The Windows x64 demo build also completed successfully after the cleanup.

Local PlayerSettings edits were kept out of the cleanup commit.
Builds, Unity caches and logs remain ignored. Git history and existing LFS
objects were not rewritten or purged, so historical storage is unchanged.

Tools require Python and Pillow. Run `Tools/optimize_repository_pngs.py`
from any directory to recompress metadata-free PNGs, and
`Tools/verify_repository_assets.py` before committing to compare changed
PNGs and removed reference GUIDs against HEAD.
