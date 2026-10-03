"""Prebattle camp drum previews, synthesized from original waveforms."""
import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
OUT = Path(__file__).resolve().parent


def render(hits, tension):
    duration = 4.2
    count = int(RATE * duration)
    dry = [0.0] * count
    rng = random.Random(208)
    rumble = 0.0
    for i in range(count):
        t = i / RATE
        fade = min(1, t / .22, (duration - t) / .65)
        rumble = .985 * rumble + .015 * rng.uniform(-1, 1)
        # A restrained low bed suggests an uneasy, waking camp.
        dry[i] = fade * (.033 * math.sin(2 * math.pi * 47 * t)
                         + .022 * math.sin(2 * math.pi * 49.1 * t)
                         + (.10 + .04 * tension) * rumble)
    for start, strength, pitch in hits:
        phase = 0.0
        skin = 0.0
        for i in range(int(RATE * 2.6)):
            pos = round(start * RATE) + i
            if pos >= count:
                break
            t = i / RATE
            phase += 2 * math.pi * (pitch + 38 * math.exp(-t * 42)) / RATE
            skin = .48 * skin + .52 * rng.uniform(-1, 1)
            attack = min(1, t / .002)
            body = (.52 * math.sin(phase) * math.exp(-t * 3.5)
                    + .27 * math.sin(phase * 1.593) * math.exp(-t * 5.2)
                    + .15 * math.sin(phase * 2.136) * math.exp(-t * 7.8)
                    + .09 * math.sin(phase * 2.917) * math.exp(-t * 11))
            dry[pos] += strength * attack * (body + .65 * skin * math.exp(-t * 48))
    left, right = dry[:], dry[:]
    for delay, gain, pan in ((.071, .15, .7), (.137, .11, 1.3),
                             (.223, .08, .65), (.361, .05, 1.2), (.517, .03, .8)):
        shift = round(delay * RATE)
        for i in range(shift, count):
            left[i] += dry[i - shift] * gain * pan
            right[i] += dry[i - shift] * gain * (2 - pan)
    return list(zip(left, right))


def save(name, frames):
    with wave.open(str(OUT / name), 'wb') as wav:
        wav.setparams((2, 2, RATE, 0, 'NONE', 'not compressed'))
        data = bytearray()
        for frame in frames:
            for sample in frame:
                data.extend(struct.pack('<h', round(math.tanh(sample * .85) * 28000)))
        wav.writeframes(data)


stages = [
    ('01-prebattle-first-day.wav', [( .12, .86, 68), (.96, .32, 76), (1.64, .72, 65)], .25),
    ('02-prebattle-second-day.wav', [(.12, .90, 65), (.83, .43, 75), (1.18, .52, 70), (1.85, .83, 62)], .6),
    ('03-prebattle-final-day.wav', [(.12, .96, 62), (.69, .48, 73), (1.04, .68, 65),
                                  (1.31, .53, 72), (1.57, .72, 64), (1.92, 1.0, 59)], 1),
]
combined = []
for name, hits, tension in stages:
    frames = render(hits, tension)
    save(name, frames)
    combined.extend(frames)
    combined.extend([(0.0, 0.0)] * round(.65 * RATE))
save('prebattle-drums-progression.wav', combined)
print('Created prebattle drum progression: 14.55 seconds, stereo 44.1 kHz.')
