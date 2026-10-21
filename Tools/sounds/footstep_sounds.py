import sys, math
import numpy as np
import synth
from synth import samples, resonator, lowpass, noise, place, soft_clip, save, RATE

synth.setup(41, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng

VARIANTS = 4
PEAK = 0.4


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def burst(length, decay, attack=0.001):
    n = samples(length)
    t = np.arange(n) / RATE
    return noise(n) * np.exp(-t / decay) * (1 - np.exp(-t / attack))


def band(x, center, q):
    return resonator(x, center, q)


def scuff(length, cutoff, amount):
    n = samples(length)
    t = np.arange(n) / RATE
    env = (1 - np.exp(-t / 0.008)) * np.exp(-t / (length * 0.35))
    return highpass(noise(n), cutoff) * env * amount


def stone_contact(strength):
    hit = burst(0.08, rng.uniform(0.012, 0.02))
    body = band(hit, rng.uniform(420, 650), 0.8) * 1.2
    thump = lowpass(lowpass(burst(0.08, 0.02), 260), 260) * 1.5
    return highpass(body + thump, 110) * strength


def stone_step():
    out = np.zeros(samples(0.28))
    place(out, stone_contact(1.0), 0)
    place(out, stone_contact(rng.uniform(0.35, 0.5)), rng.uniform(0.055, 0.08))
    place(out, lowpass(scuff(0.12, rng.uniform(1800, 2600), 0.05), 4000), rng.uniform(0.0, 0.01))
    return soft_clip(lowpass(lowpass(out, 3800), 6000), 1.05)


def metal_contact(strength):
    hit = burst(0.12, rng.uniform(0.008, 0.012))
    clack = band(hit, rng.uniform(1300, 1700), 0.7) * 0.9
    plate = np.zeros_like(hit)
    for center, decay, gain in ((rng.uniform(420, 520), 0.035, 0.55), (rng.uniform(1000, 1250), 0.025, 0.35), (rng.uniform(2100, 2500), 0.018, 0.2)):
        excite = burst(0.12, decay)
        plate += band(excite, center, 2.5) * gain
    thump = lowpass(lowpass(burst(0.12, 0.02), 240), 240) * 1.1
    return highpass(clack + plate + thump, 90) * strength


def metal_step():
    out = np.zeros(samples(0.3))
    place(out, metal_contact(1.0), 0)
    place(out, metal_contact(rng.uniform(0.35, 0.5)), rng.uniform(0.06, 0.08))
    place(out, lowpass(scuff(0.08, 2500, 0.03), 5000), 0.005)
    return soft_clip(lowpass(lowpass(out, 5000), 7000), 1.05)


def glass_contact(strength):
    hit = burst(0.1, rng.uniform(0.003, 0.005))
    tap = band(hit, rng.uniform(3200, 3900), 1.5) * 0.9
    pane = np.zeros_like(hit)
    for center, decay, gain in ((rng.uniform(1700, 1950), 0.022, 0.3), (rng.uniform(2950, 3250), 0.017, 0.22), (rng.uniform(4500, 4900), 0.012, 0.15)):
        pane += band(burst(0.1, decay), center, 6.0) * gain
    thump = lowpass(lowpass(burst(0.1, 0.012), 320), 320) * 0.7
    return highpass(tap + pane + thump, 150) * strength


def glass_step():
    out = np.zeros(samples(0.26))
    place(out, glass_contact(1.0), 0)
    place(out, glass_contact(rng.uniform(0.35, 0.5)), rng.uniform(0.055, 0.075))
    return soft_clip(lowpass(lowpass(out, 7000), 9000), 1.05)


for name, build in (("footstepStone", stone_step), ("footstepMetal", metal_step), ("footstepGlass", glass_step)):
    for i in range(1, VARIANTS + 1):
        save(f"{name}{i}", build(), PEAK)
