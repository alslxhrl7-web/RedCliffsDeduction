"""Original synthesized drum sketches for listening; no Unity changes."""
import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
OUT = Path(__file__).resolve().parent


def sketch(hits, duration=3.5):
    samples = [0.0] * int(RATE * duration)
    rng = random.Random(208)
    for start, strength, pitch in hits:
        previous_noise = 0.0
        phase = 0.0
        for i in range(int(RATE * 1.7)):
            pos = int(start * RATE) + i
            if pos >= len(samples):
                break
            t = i / RATE
            phase += 2 * math.pi * (pitch + 55 * math.exp(-t * 34)) / RATE
            noise = rng.uniform(-1, 1)
            previous_noise = .72 * previous_noise + .28 * noise
            attack = min(1, t / .003)
            body = (.65 * math.sin(phase) * math.exp(-t * 5)
                    + .24 * math.sin(phase * 1.59) * math.exp(-t * 8)
                    + .12 * math.sin(phase * 2.17) * math.exp(-t * 12))
            strike = .7 * previous_noise * math.exp(-t * 65)
            value = strength * attack * (body + strike)
            samples[pos] += value
            # Subtle reflections keep the drum spacious without a long wash.
            for delay, gain in ((.083, .13), (.151, .08), (.237, .04)):
                echo = pos + int(delay * RATE)
                if echo < len(samples):
                    samples[echo] += value * gain
    return samples


def save(name, samples):
    with wave.open(str(OUT / name), 'wb') as wav:
        wav.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        wav.writeframes(b''.join(struct.pack('<h', round(max(-1, min(1, x * .68)) * 32767)) for x in samples))


stages = [
    ('01-calm-dawn.wav', [(0.15, .64, 83), (1.35, .47, 79)]),
    ('02-shortening-time.wav', [(0.15, .70, 78), (.95, .57, 76), (1.65, .72, 74)]),
    ('03-final-day.wav', [(0.15, .78, 73), (.78, .64, 71), (1.27, .72, 70),
                          (1.63, .68, 70), (1.93, .86, 67)]),
]
combined = []
for name, hits in stages:
    samples = sketch(hits)
    save(name, samples)
    combined.extend(samples)
    combined.extend([0.0] * int(RATE * .8))
save('dawn-drums-progression.wav', combined)
print('Created three drum previews and combined progression.')
