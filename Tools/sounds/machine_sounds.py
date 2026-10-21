import sys, math
import numpy as np
import synth
from synth import samples, curve, resonator, lowpass, noise, envelope, soft_clip, save, place, RATE

synth.setup(31, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng


def sweep(freqs, n):
    return np.sin(2 * math.pi * np.cumsum(curve(freqs, n)) / RATE)


def loudness_peak(x, target_rms):
    x = x / max(1e-9, np.max(np.abs(x)))
    rms = math.sqrt(float(np.mean(x ** 2)))
    return min(0.6, target_rms / max(1e-9, rms))


def gravity(on):
    length = 1.6
    n = samples(length)
    low = [34, 52, 60] if on else [60, 50, 30]
    base = sweep(low, n) * 0.9 + sweep([f * 2 for f in low], n) * 0.35 + sweep([f * 3.01 for f in low], n) * 0.12
    wobble = 1 + 0.15 * np.sin(2 * math.pi * np.cumsum(curve([3, 7, 5] if on else [6, 3, 1.5], n)) / RATE)
    env = envelope(n, [(0, 0), (0.25, 0.9), (0.6, 1), (1, 0)] if on else [(0, 0), (0.05, 1), (0.45, 0.7), (1, 0)])
    hum = lowpass(base * wobble, 700) * env

    centers = curve([250, 900, 600] if on else [800, 300, 150], n)
    air = lowpass(resonator(noise(n), centers, 1.2), 2500) * envelope(n, [(0, 0), (0.3, 1), (0.7, 0.5), (1, 0)]) * 0.35

    out = hum + air
    thump_n = samples(0.5)
    t = np.arange(thump_n) / RATE
    thump = np.sin(2 * math.pi * 45 * t) * np.exp(-t / 0.12) * (1 - np.exp(-t / 0.004))
    place(out, thump * 0.8, length * 0.55 if on else 0.0)
    return soft_clip(out, 1.1)


def air_zone(entering):
    length = 0.75
    n = samples(length)
    cutoff = curve([500, 2400, 1600] if entering else [2200, 900, 300], n)
    hiss = resonator(noise(n), cutoff, 0.7)
    body = lowpass(lowpass(noise(n), 350), 350) * 2.0
    env = envelope(n, [(0, 0), (0.25, 1), (0.65, 0.6), (1, 0)] if entering else [(0, 0), (0.08, 1), (0.4, 0.55), (1, 0)])
    return soft_clip(lowpass(hiss * 0.6 + body, 4500) * env, 1.05)


for on, name in ((True, "gravityGeneratorOn"), (False, "gravityGeneratorOff")):
    sound = gravity(on)
    save(name, sound, loudness_peak(sound, 0.08))

for entering, name in ((True, "airZoneEnter"), (False, "airZoneExit")):
    sound = air_zone(entering)
    save(name, sound, loudness_peak(sound, 0.035))
