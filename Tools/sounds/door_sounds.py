import sys, math
import numpy as np
import synth
from synth import samples, curve, resonator, lowpass, noise, envelope, place, soft_clip, save, RATE

synth.setup(11, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng

PLATE_MODES = [(1.0, 1.0, 1.0), (1.59, 0.55, 0.8), (2.14, 0.4, 0.65), (2.65, 0.3, 0.5), (3.42, 0.18, 0.35), (4.17, 0.1, 0.25)]

DOORS = {
    "aluminiumDoor": {"slide": 1.0, "ring": 980, "decay": 0.22, "weight": 0.55, "rumble": 160, "motor": 260, "noise": 1.0},
    "ironDoor": {"slide": 1.25, "ring": 430, "decay": 0.5, "weight": 1.0, "rumble": 95, "motor": 140, "noise": 0.8},
    "titaniumDoor": {"slide": 0.83, "ring": 700, "decay": 0.34, "weight": 0.8, "rumble": 120, "motor": 340, "noise": 0.6},
}


def metal_ring(freq, decay, length=None, brightness=1.0):
    length = length or decay * 5
    n = samples(length)
    t = np.arange(n) / RATE
    out = np.zeros(n)
    for ratio, gain, decay_scale in PLATE_MODES:
        f = freq * ratio * rng.uniform(0.985, 1.015)
        if f > RATE * 0.45: continue
        g = gain * (brightness if ratio > 2 else 1.0)
        out += np.sin(2 * math.pi * f * t + rng.uniform(0, 6.28)) * np.exp(-t / (decay * decay_scale)) * g
    strike = resonator(noise(samples(0.01)) * np.hanning(samples(0.01)), freq * 3, 1.5)
    place(out, strike * 0.6, 0)
    return out * (1 - np.exp(-t / 0.0008))


def thump(freq, length, weight):
    n = samples(length)
    body = lowpass(lowpass(noise(n), freq), freq) * envelope(n, [(0, 1), (0.3, 0.4), (1, 0)])
    return body * 6 * weight


def latch(door, strength):
    out = np.zeros(samples(0.5))
    place(out, metal_ring(door["ring"] * 1.6, door["decay"] * 0.25, 0.35, 1.2) * 0.5 * strength, 0)
    place(out, thump(door["rumble"] * 1.5, 0.12, door["weight"]) * strength, 0.002)
    place(out, metal_ring(door["ring"] * 2.3, door["decay"] * 0.12, 0.2) * 0.25 * strength, 0.035)
    return out


def slam(door, strength):
    out = np.zeros(samples(door["decay"] * 4 + 0.2))
    place(out, thump(door["rumble"], 0.25, door["weight"]) * 1.3 * strength, 0)
    place(out, metal_ring(door["ring"], door["decay"], door["decay"] * 4) * 0.7 * strength, 0.001)
    place(out, latch(door, 0.6) * strength, 0.06)
    return out


def slide_texture(door, length, smooth):
    n = samples(length)
    center = door["ring"] * 0.7 * (1 + curve(rng.uniform(-0.08, 0.08, 6), n))
    scrape = resonator(noise(n), center, 2.0 + smooth * 4) * (1 - smooth * 0.6)
    roller_rate = curve([9, 14, 12, 8], n) * (1.4 if door["weight"] < 0.7 else 1.0)
    roller = 0.6 + 0.4 * np.sin(2 * math.pi * np.cumsum(roller_rate) / RATE)
    rumble = lowpass(lowpass(noise(n), door["rumble"] * 2), door["rumble"] * 2) * 5 * door["weight"]
    env = envelope(n, [(0, 0), (0.15, 1), (0.8, 0.9), (1, 0.2)])
    return (scrape * 0.35 * door["noise"] + rumble * roller) * env


def hiss(length, low, high, amount):
    n = samples(length)
    sweep = curve([high, (low + high) * 0.5, low], n)
    burst = resonator(noise(n), sweep, 0.9) * envelope(n, [(0, 0), (0.04, 1), (0.35, 0.5), (1, 0)])
    return burst * amount


def motor(door, length, rising):
    n = samples(length)
    base = door["motor"]
    f0 = curve([base * 0.6, base * 1.05, base, base * 0.7] if rising else [base * 0.7, base, base * 1.05, base * 0.6], n)
    f0 = f0 * (1 + curve(rng.uniform(-0.01, 0.01, 30), n))
    phase = np.cumsum(f0) / RATE * 2 * math.pi
    tone = sum(np.sin(phase * k) / k ** 1.3 for k in range(1, 8))
    gears = 1 + 0.25 * np.sign(np.sin(phase / 6))
    out = lowpass(tone * gears, 2200) * envelope(n, [(0, 0), (0.12, 1), (0.85, 0.9), (1, 0)])
    return out * 0.5


def servo(door, length, rising):
    n = samples(length)
    base = door["motor"] * 2.2
    f0 = curve([base * 0.75, base * 1.2, base * 1.25] if rising else [base * 1.25, base * 1.15, base * 0.75], n)
    phase = np.cumsum(f0) / RATE * 2 * math.pi
    tone = np.sin(phase) + 0.35 * np.sin(phase * 2.01) + 0.15 * np.sin(phase * 3.02)
    shimmer = 0.85 + 0.15 * np.sin(2 * math.pi * 6 * np.arange(n) / RATE)
    out = resonator(tone * shimmer, base * 1.6, 1.2) + tone * 0.2
    return out * envelope(n, [(0, 0), (0.1, 1), (0.8, 0.8), (1, 0)]) * 0.35


def build(door, style, opening):
    slide = door["slide"]
    total = slide + 0.9
    out = np.zeros(samples(total))
    start = 0.06 if opening else 0.0

    if opening:
        place(out, latch(door, 0.7), 0)
    if style == 1:
        place(out, lowpass(hiss(0.45 if opening else 0.35, 700, 4200, 0.4 * door["noise"]), 5000), 0.02 if opening else slide - 0.05)
        place(out, slide_texture(door, slide, 0.6) * 0.8, start)
    elif style == 2:
        place(out, motor(door, slide + 0.1, opening), start - 0.03 if opening else 0)
        place(out, slide_texture(door, slide, 0.2), start)
    else:
        place(out, servo(door, slide, opening), start)
        place(out, slide_texture(door, slide, 0.9) * 0.45, start)
        place(out, hiss(0.25, 2000, 7000, 0.18), start + slide - 0.15)

    end = start + slide
    if opening:
        place(out, slam(door, 0.35), end)
    else:
        place(out, slam(door, 1.0), end)
        if style == 1: place(out, lowpass(hiss(0.3, 1000, 3800, 0.18), 4500), end + 0.05)

    out = soft_clip(lowpass(out, 9000), 1.2)
    tail = samples(end + door["decay"] * 2.5 + 0.15)
    return out[:min(len(out), tail)]


TARGET_RMS = {True: 0.12, False: 0.15}


def loudness_peak(x, target_rms):
    x = x / max(1e-9, np.max(np.abs(x)))
    rms = math.sqrt(float(np.mean(x ** 2)))
    return min(0.9, target_rms / max(1e-9, rms))


for name, door in DOORS.items():
    for style in (1, 2, 3):
        for opening, label in ((True, "Open"), (False, "Close")):
            sound = build(door, style, opening)
            save(f"{name}{label}{style}", sound, loudness_peak(sound, TARGET_RMS[opening]))
