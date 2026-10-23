import os, sys, math
import numpy as np
import synth
from synth import loop_filter, samples, curve, resonator, lowpass, highpass, noise, envelope, soft_clip, save, place, sine, blip, periodic, crossfade_loop, loudness_peak, RATE

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
BASE = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Spacebox", "Mods", "GameSets", "Empty", "Sounds")
synth.setup(89, BASE)
rng = synth.rng


def click(seconds, center, q, decay):
    n = samples(seconds)
    t = np.arange(n) / RATE
    return resonator(noise(n), center, q) * np.exp(-t / decay) * (1 - np.exp(-t / 0.0008))


def equip_armor():
    out = np.zeros(samples(0.42))
    swish_n = samples(0.2)
    swish = lowpass(resonator(noise(swish_n), curve([400, 1300, 700], swish_n), 0.7), 3000) * envelope(swish_n, [(0, 0), (0.4, 1), (1, 0)])
    place(out, swish * 0.35, 0.0)
    place(out, click(0.06, 1700, 3.0, 0.008) * 0.6, 0.12)
    place(out, click(0.08, 1250, 3.5, 0.012) * 0.7, 0.19)
    ring_n = samples(0.22)
    t = np.arange(ring_n) / RATE
    ring = (np.sin(2 * math.pi * 2350 * t) * 0.5 + np.sin(2 * math.pi * 3620 * t) * 0.25) * np.exp(-t / 0.035)
    place(out, ring * 0.12, 0.19)
    thud_n = samples(0.12)
    t = np.arange(thud_n) / RATE
    place(out, np.sin(2 * math.pi * 160 * t) * np.exp(-t / 0.03) * 0.4, 0.19)
    return soft_clip(lowpass(out, 5000), 1.05)


def equip_device():
    out = np.zeros(samples(0.32))
    place(out, click(0.04, 1500, 2.5, 0.006) * 0.6, 0.0)
    place(out, blip(600, 900, 0.1, 0.004, 0.035) * 0.45, 0.04)
    place(out, blip(900, 1200, 0.14, 0.004, 0.05) * 0.4, 0.11)
    return soft_clip(lowpass(out, 4200), 1.05)


def drill_loop():
    length = 2.0
    loop_n = samples(length)
    t = np.arange(loop_n) / RATE
    fundamental = periodic(170, length)
    am = 1 + 0.06 * np.sin(2 * math.pi * periodic(9, length) * t)
    vibrato = 0.004 * np.sin(2 * math.pi * periodic(3, length) * t)
    phase = 2 * math.pi * np.cumsum(fundamental * (1 + vibrato)) / RATE
    motor = (np.sin(phase) * 0.5 + np.sin(2 * phase) * 0.22 + np.sin(3 * phase) * 0.08 + np.sin(4 * phase) * 0.03) * am

    total = loop_n + samples(0.4)
    grind = crossfade_loop(lowpass(resonator(noise(total), 650, 0.9), 1600) * 0.22, loop_n)
    body = crossfade_loop(lowpass(lowpass(noise(total), 180), 180) * 1.4, loop_n)
    return loop_filter(motor + grind + body, lambda x: lowpass(x, 1800))


for folder, name, make, rms in (
        ("Player", "equipArmor", equip_armor, 0.05),
        ("Player", "equipDevice", equip_device, 0.05)):
    synth.output_to(os.path.join(BASE, folder))
    sound = make()
    save(name, sound, loudness_peak(sound, rms))

synth.output_to(os.path.join(BASE, "Drills"))
sound = drill_loop()
save("drillLoop", sound, loudness_peak(sound, 0.08), fade_edges=False)
