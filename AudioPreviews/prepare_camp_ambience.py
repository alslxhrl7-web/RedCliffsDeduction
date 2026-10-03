"""Extract VARCO ambience and soften its loop seam for Unity."""
import subprocess
import sys
import tempfile
import wave
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path(tempfile.gettempdir()) / 'redcliffs-audio-tools'))
sys.path.insert(0, str(Path.home() / 'AppData' / 'Local' / 'Temp' / 'redcliffs-audio-tools'))
import imageio_ffmpeg

root = Path(__file__).resolve().parent.parent
source = root / 'AudioPreviews' / 'varco-wind-flags-original.mp4'
raw = root / 'AudioPreviews' / 'varco-wind-flags-original.wav'
target = root / 'Assets' / 'Resources' / 'Audio' / 'CampWindAmbience.wav'
target.parent.mkdir(parents=True, exist_ok=True)
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-y', '-i', str(source),
                '-vn', '-ac', '2', '-ar', '44100', '-c:a', 'pcm_s16le', str(raw)],
               check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
with wave.open(str(raw), 'rb') as wav:
    rate = wav.getframerate()
    samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').reshape(-1, 2).astype(np.float64)
overlap = min(int(.6 * rate), len(samples) // 4)
blend = np.linspace(0, 1, overlap)[:, None]
seam = samples[-overlap:] * (1 - blend) + samples[:overlap] * blend
loop = np.concatenate((samples[overlap:-overlap], seam))
peak = np.max(np.abs(loop))
if peak > 0:
    loop *= (.45 * 32767) / peak
with wave.open(str(target), 'wb') as wav:
    wav.setparams((2, 2, rate, 0, 'NONE', 'not compressed'))
    wav.writeframes(np.clip(np.rint(loop), -32768, 32767).astype('<i2').tobytes())
print(f'Original {len(samples)/rate:.2f}s; loop {len(loop)/rate:.2f}s; stereo {rate}Hz; peak {np.max(np.abs(loop))/32768:.3f}')
