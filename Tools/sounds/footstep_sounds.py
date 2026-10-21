import sys, math
import numpy as np
import synth
from synth import samples, resonator, lowpass, noise, envelope, crunch, place, soft_clip, save, RATE

synth.setup(41, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng

VARIANTS = 4


def damped(freq, decay, length):
    n = samples(length)
    t = np.arange(n) / RATE
    return np.sin(2 * math.pi * freq * t) * np.exp(-t / decay) * (1 - np.exp(-t / 0.0015))


def loudness_peak(x, target_rms):
    x = x / max(1e-9, np.max(np.abs(x)))
    rms = math.sqrt(float(np.mean(x ** 2)))
    return min(0.7, target_rms / max(1e-9, rms))


def heel(length, cutoff, decay):
    n = samples(length)
    return lowpass(lowpass(noise(n), cutoff), cutoff) * np.exp(-np.arange(n) / (decay * RATE)) * 4


def stone_step():
    length = 0.3
    out = np.zeros(samples(length))
    place(out, heel(0.12, rng.uniform(180, 260), 0.025), 0)
    place(out, damped(rng.uniform(80, 110), 0.03, 0.15) * 0.6, 0)
    grit = crunch(0.16, int(rng.integers(40, 70)), 1400, 3200) * envelope(samples(0.16), [(0, 1), (0.4, 0.5), (1, 0)])
    place(out, grit * 0.45, rng.uniform(0.005, 0.02))
    roll = crunch(0.1, int(rng.integers(10, 20)), 900, 2000) * 0.25
    place(out, roll, rng.uniform(0.06, 0.1))
    return soft_clip(lowpass(out, 6000), 1.1)


def metal_step():
    length = 0.32
    out = np.zeros(samples(length))
    place(out, heel(0.1, rng.uniform(220, 300), 0.02), 0)
    place(out, damped(rng.uniform(105, 125), 0.04, 0.2) * 0.7, 0)
    for freq, decay, gain in ((rng.uniform(290, 330), 0.06, 0.32), (rng.uniform(510, 560), 0.045, 0.22), (rng.uniform(840, 920), 0.03, 0.12)):
        place(out, damped(freq, decay, 0.25) * gain, 0.002)
    n = samples(0.04)
    tick = resonator(noise(n), rng.uniform(2200, 2800), 2.0) * np.exp(-np.arange(n) / (0.006 * RATE)) * 0.25
    place(out, tick, 0)
    return soft_clip(lowpass(out, 5000), 1.1)


for i in range(1, VARIANTS + 1):
    sound = stone_step()
    save(f"footstepStone{i}", sound, loudness_peak(sound, 0.07))

for i in range(1, VARIANTS + 1):
    sound = metal_step()
    save(f"footstepMetal{i}", sound, loudness_peak(sound, 0.07))
