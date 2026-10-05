# Audio production tools

The Python scripts here remain versioned so the game audio can be reproduced.
Generated WAV previews and VARCO source MP4/JPG files are excluded from Git.
The imported game audio remains in `Assets/Resources/Audio`.

Original media from the 2026-10-05 cleanup is backed up locally in
`LocalArchive/GitHubCleanup-2026-10-05/AudioPreviews`.
To rerun `prepare_camp_ambience.py`, copy `varco-wind-flags-original.mp4`
from that backup into this directory. On another machine, obtain the original
VARCO export first. Required Python libraries are NumPy and imageio-ffmpeg.
