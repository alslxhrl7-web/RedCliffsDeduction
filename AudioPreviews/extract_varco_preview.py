"""Extract a VARCO source into a level-adjusted WAV listening preview."""
import subprocess
import sys
import tempfile
import wave
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path.home() / 'AppData/Local/Temp/redcliffs-audio-tools'))
sys.path.insert(0, str(Path(tempfile.gettempdir()) / 'redcliffs-audio-tools'))
import imageio_ffmpeg

source, output = map(Path, sys.argv[1:3])
raw = source.with_suffix('.wav')
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-y', '-i', str(source), '-vn',
                '-ac', '2', '-ar', '44100', '-c:a', 'pcm_s16le', str(raw)],
               check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
with wave.open(str(raw), 'rb') as wav:
    rate = wav.getframerate()
    samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').reshape(-1, 2).astype(float)
peak = np.max(np.abs(samples))
if peak:
    samples *= .45 * 32767 / peak
with wave.open(str(output), 'wb') as wav:
    wav.setparams((2, 2, rate, 0, 'NONE', 'not compressed'))
    wav.writeframes(np.clip(np.rint(samples), -32768, 32767).astype('<i2').tobytes())
print(f'{output.name}: {len(samples)/rate:.2f}s, stereo {rate} Hz, peak {np.max(np.abs(samples))/32768:.3f}')
