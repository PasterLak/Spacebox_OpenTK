import os, sys, math
import numpy as np
import synth
from synth import samples, curve, resonator, lowpass, highpass, noise, envelope, soft_clip, save, place, sine, blip, bell, loudness_peak, RATE

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
synth.setup(57, sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Spacebox", "Resources", "Audio", "UI"))
rng = synth.rng


def swish(seconds, low, high, q=0.9):
    n = samples(seconds)
    centers = curve([low, high], n)
    env = envelope(n, [(0, 0), (0.35, 1), (1, 0)])
    return lowpass(resonator(noise(n), centers, q), 5000) * env


def window(opening):
    length = 0.22
    out = np.zeros(samples(length))
    place(out, swish(0.18, 500, 1900, 0.8) if opening else swish(0.17, 1700, 450, 0.8), 0.0)
    tone_a, tone_b = (660, 990) if opening else (880, 587)
    place(out, blip(tone_a, tone_a * 1.02, 0.12, 0.004, 0.035) * 0.55, 0.03)
    place(out, blip(tone_b, tone_b * 1.01, 0.14, 0.004, 0.045) * 0.45, 0.075)
    return soft_clip(lowpass(out, 4200), 1.05)


def hover():
    n = samples(0.05)
    t = np.arange(n) / RATE
    tick = np.sin(2 * math.pi * 1900 * t) * np.exp(-t / 0.009) * (1 - np.exp(-t / 0.0015))
    return lowpass(tick, 3500)


def tab():
    out = np.zeros(samples(0.11))
    place(out, blip(1050, 1050, 0.06, 0.002, 0.015) * 0.8, 0.0)
    place(out, blip(1400, 1400, 0.07, 0.002, 0.02) * 0.6, 0.035)
    return lowpass(out, 4000)


def slot_pick():
    n = samples(0.09)
    out = np.zeros(n)
    tap = lowpass(resonator(noise(samples(0.03)), 1100, 2.0), 3000) * np.exp(-np.arange(samples(0.03)) / (0.006 * RATE))
    place(out, tap * 0.7, 0.0)
    place(out, blip(520, 820, 0.07, 0.003, 0.02) * 0.5, 0.005)
    return lowpass(out, 3800)


def slot_place():
    n = samples(0.1)
    out = np.zeros(n)
    tap = lowpass(resonator(noise(samples(0.035)), 650, 1.6), 2200) * np.exp(-np.arange(samples(0.035)) / (0.008 * RATE))
    place(out, tap * 0.8, 0.0)
    place(out, blip(360, 250, 0.08, 0.002, 0.022) * 0.6, 0.0)
    return lowpass(out, 3000)


def slot_split():
    out = np.zeros(samples(0.12))
    place(out, blip(760, 760, 0.04, 0.0015, 0.01) * 0.6, 0.0)
    place(out, blip(980, 980, 0.05, 0.0015, 0.012) * 0.5, 0.045)
    tap = lowpass(resonator(noise(samples(0.02)), 1500, 2.0), 3500) * np.exp(-np.arange(samples(0.02)) / (0.004 * RATE))
    place(out, tap * 0.4, 0.0)
    return lowpass(out, 3800)


def craft_complete():
    out = np.zeros(samples(0.7))
    place(out, bell(784, 0.6, 0.2) * 0.6, 0.0)
    place(out, bell(1175, 0.6, 0.24) * 0.45, 0.07)
    place(out, swish(0.12, 900, 2400, 1.2) * 0.12, 0.0)
    return soft_clip(lowpass(out, 5000), 1.05)


def denied():
    out = np.zeros(samples(0.26))
    for i in range(2):
        n = samples(0.08)
        t = np.arange(n) / RATE
        f = 233 - i * 22
        tone = (np.sin(2 * math.pi * f * t) + 0.3 * np.sin(2 * math.pi * f * 2 * t) + 0.12 * np.sin(2 * math.pi * f * 3 * t))
        env = envelope(n, [(0, 0), (0.12, 1), (0.6, 0.7), (1, 0)])
        place(out, lowpass(tone * env, 1400), 0.01 + i * 0.11)
    return soft_clip(out, 1.1)


def no_power():
    out = np.zeros(samples(0.5))
    for i, (a, b) in enumerate(((523, 500), (392, 350))):
        n = samples(0.16)
        tone = sine(curve([a, b], n), n) + 0.2 * sine(curve([a * 2, b * 2], n), n)
        env = envelope(n, [(0, 0), (0.1, 1), (0.55, 0.6), (1, 0)])
        place(out, lowpass(tone * env, 2200) * (0.8 - i * 0.1), 0.02 + i * 0.17)
    hum_n = samples(0.4)
    hum = lowpass(sine(curve([120, 70], hum_n), hum_n), 400) * envelope(hum_n, [(0, 0), (0.15, 0.4), (1, 0)])
    place(out, hum * 0.35, 0.05)
    return soft_clip(out, 1.05)


def waypoint():
    out = np.zeros(samples(0.75))
    place(out, bell(1047, 0.7, 0.18, ((1.0, 1.0), (2.0, 0.18), (3.0, 0.05))) * 0.6, 0.0)
    place(out, bell(1568, 0.6, 0.22, ((1.0, 1.0), (2.0, 0.12))) * 0.35, 0.09)
    shimmer_n = samples(0.5)
    shimmer = resonator(noise(shimmer_n), 3000, 6.0) * np.exp(-np.arange(shimmer_n) / (0.12 * RATE))
    place(out, shimmer * 0.08, 0.02)
    return soft_clip(lowpass(out, 5500), 1.05)


def low_oxygen():
    out = np.zeros(samples(0.36))
    for i in range(2):
        n = samples(0.11)
        t = np.arange(n) / RATE
        tone = np.sin(2 * math.pi * 880 * t) + 0.15 * np.sin(2 * math.pi * 1760 * t)
        env = envelope(n, [(0, 0), (0.15, 1), (0.6, 0.8), (1, 0)])
        place(out, tone * env, 0.01 + i * 0.16)
    return lowpass(out, 3000)


for opening, name in ((True, "windowOpen"), (False, "windowClose")):
    sound = window(opening)
    save(name, sound, loudness_peak(sound, 0.05))

for name, make, rms in (
        ("buttonHover", hover, 0.02),
        ("tabSwitch", tab, 0.035),
        ("slotPick", slot_pick, 0.045),
        ("slotPlace", slot_place, 0.05),
        ("slotSplit", slot_split, 0.04),
        ("craftComplete", craft_complete, 0.06),
        ("denied", denied, 0.06),
        ("noPower", no_power, 0.06),
        ("waypointAdded", waypoint, 0.05),
        ("lowOxygen", low_oxygen, 0.06)):
    sound = make()
    save(name, sound, loudness_peak(sound, rms))
