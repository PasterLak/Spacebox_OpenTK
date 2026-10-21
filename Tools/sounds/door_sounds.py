import sys, math
import numpy as np
import synth
from synth import samples, curve, resonator, lowpass, noise, envelope, place, soft_clip, save, RATE

synth.setup(11, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng

DOORS = {
    "aluminiumDoor": {"slide": 1.0, "thud": 150, "body": 900, "weight": 0.6, "air": 1.0},
    "ironDoor": {"slide": 1.25, "thud": 95, "body": 600, "weight": 1.0, "air": 0.8},
    "titaniumDoor": {"slide": 0.83, "thud": 120, "body": 750, "weight": 0.8, "air": 0.7},
}

TAIL = 0.22


def damped(freq, decay, length):
    n = samples(length)
    t = np.arange(n) / RATE
    return np.sin(2 * math.pi * freq * t) * np.exp(-t / decay) * (1 - np.exp(-t / 0.001))


def thud(door, strength):
    length = 0.2
    n = samples(length)
    body = lowpass(lowpass(noise(n), door["thud"] * 2.5), door["thud"] * 2.5) * envelope(n, [(0, 1), (0.15, 0.35), (1, 0)]) * 4
    low = damped(door["thud"], 0.035 * door["weight"] + 0.015, length)
    knock = resonator(noise(n), door["body"], 3.0) * np.exp(-np.arange(n) / (0.012 * RATE)) * 0.6
    return (body + low * 0.9 + knock) * strength * door["weight"]


def click(door, strength):
    n = samples(0.05)
    tick = resonator(noise(n), door["body"] * 2.2, 2.0) * np.exp(-np.arange(n) / (0.004 * RATE))
    return (tick + damped(door["body"] * 1.7, 0.006, 0.05) * 0.4) * strength


def slide_noise(door, length, smooth):
    n = samples(length)
    center = curve([door["body"] * 0.55, door["body"] * 0.7, door["body"] * 0.6], n)
    swish = resonator(noise(n), center, 0.8)
    rumble = lowpass(lowpass(noise(n), 260), 260) * 3 * door["weight"]
    roll = 1 + 0.12 * np.sin(2 * math.pi * np.cumsum(curve([6, 9, 7], n)) / RATE) * (1 - smooth)
    env = envelope(n, [(0, 0), (0.18, 1), (0.75, 0.85), (1, 0)])
    return lowpass((swish * 0.5 + rumble) * roll, 3200) * env


def air(length, cutoff, amount):
    n = samples(length)
    return lowpass(lowpass(noise(n), cutoff), cutoff) * 2.2 * amount * envelope(n, [(0, 0), (0.06, 1), (0.5, 0.45), (1, 0)])


def hum(door, length, opening):
    n = samples(length)
    base = door["thud"] * 1.6
    center = curve([base * 0.85, base * 1.08, base] if opening else [base, base * 1.05, base * 0.85], n)
    tone = resonator(noise(n), center, 7.0) * 2.5
    return lowpass(tone, 900) * envelope(n, [(0, 0), (0.15, 1), (0.85, 0.9), (1, 0)])


def whoosh(door, length, opening):
    n = samples(length)
    lo, hi = door["body"] * 0.45, door["body"] * 1.4
    center = curve([lo, hi, hi * 0.9] if opening else [hi * 0.9, hi, lo], n)
    return lowpass(resonator(noise(n), center, 1.6), 3500) * envelope(n, [(0, 0), (0.2, 1), (0.8, 0.7), (1, 0)])


def build(door, style, opening):
    slide = door["slide"]
    out = np.zeros(samples(slide + TAIL))
    lead = 0.03 if opening else 0.0
    move = slide - lead

    if opening:
        place(out, click(door, 0.5), 0)

    if style == 1:
        place(out, air(move * 0.55, 1800, 0.55 * door["air"]), lead)
        place(out, slide_noise(door, move, 0.7) * 0.8, lead)
    elif style == 2:
        place(out, hum(door, move, opening) * 0.7, lead)
        place(out, slide_noise(door, move, 0.3), lead)
    else:
        place(out, whoosh(door, move, opening) * 0.8, lead)
        place(out, slide_noise(door, move, 0.9) * 0.5, lead)

    if opening:
        place(out, thud(door, 0.35), slide - 0.01)
    else:
        place(out, thud(door, 1.0), slide - 0.01)
        if style == 1: place(out, air(0.18, 1400, 0.25 * door["air"]), slide)

    return soft_clip(lowpass(out, 6000), 1.1)


TARGET_RMS = {True: 0.07, False: 0.085}


def loudness_peak(x, target_rms):
    x = x / max(1e-9, np.max(np.abs(x)))
    rms = math.sqrt(float(np.mean(x ** 2)))
    return min(0.6, target_rms / max(1e-9, rms))


for name, door in DOORS.items():
    for style in (1, 2, 3):
        for opening, label in ((True, "Open"), (False, "Close")):
            sound = build(door, style, opening)
            save(f"{name}{label}{style}", sound, loudness_peak(sound, TARGET_RMS[opening]))
