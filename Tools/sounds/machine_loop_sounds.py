import os, sys, math
import numpy as np
import synth
from synth import loop_filter, samples, curve, resonator, lowpass, highpass, noise, envelope, soft_clip, save, place, sine, blip, bell, periodic, crossfade_loop, loudness_peak, RATE

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
synth.setup(73, sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Spacebox", "Mods", "GameSets", "Default", "Sounds", "Blocks", "Machines"))
rng = synth.rng

FADE = 0.4


def noise_layer(loop_seconds, make):
    loop_n = samples(loop_seconds)
    total = loop_n + samples(FADE)
    return crossfade_loop(make(total), loop_n)


def tone_layer(loop_seconds, partials, wobble_rate=0.0, wobble_depth=0.0):
    n = samples(loop_seconds)
    t = np.arange(n) / RATE
    out = np.zeros(n)
    for freq, gain in partials:
        f = periodic(freq, loop_seconds)
        out += np.sin(2 * math.pi * f * t) * gain
    if wobble_rate > 0:
        rate = periodic(wobble_rate, loop_seconds)
        out *= 1 + wobble_depth * np.sin(2 * math.pi * rate * t)
    return out


def pulses(loop_seconds, rate, make_pulse, jitter=0.0):
    n = samples(loop_seconds)
    out = np.zeros(n + samples(1.0))
    count = max(1, round(loop_seconds * rate))
    step = loop_seconds / count
    for i in range(count):
        place(out, make_pulse(i), i * step + rng.uniform(-jitter, jitter) * step + jitter * step)
    tail = out[n:]
    out = out[:n]
    out[:len(tail)] += tail[:n]
    return out


def furnace_loop():
    length = 3.0
    roar = noise_layer(length, lambda n: lowpass(lowpass(noise(n), 260), 260) * 3.0 * (1 + 0.25 * curve(rng.uniform(-1, 1, 12), n)))
    breath = noise_layer(length, lambda n: resonator(noise(n), 520, 0.8) * 0.12)
    crackle = noise_layer(length, lambda n: highpass(synth.crunch(n / RATE, int(n / RATE * 9), 1400, 2200), 600) * 0.12)
    hum = tone_layer(length, ((50, 0.25), (100, 0.12)), 0.33, 0.15)
    return loop_filter(roar + breath + crackle + hum, lambda x: lowpass(x, 3000))


def crusher_loop():
    length = 2.4
    motor = tone_layer(length, ((78, 0.5), (156, 0.22), (234, 0.08)), 2.5, 0.12)

    def thump(_):
        n = samples(0.18)
        t = np.arange(n) / RATE
        body = np.sin(2 * math.pi * 70 * t) * np.exp(-t / 0.05) * (1 - np.exp(-t / 0.003))
        grit = lowpass(noise(n), 900) * np.exp(-t / 0.03) * 0.5
        return body + grit

    thumps = pulses(length, 2.5, thump, 0.04) * 0.55
    grind = noise_layer(length, lambda n: resonator(noise(n), 700, 1.0) * 0.18 * (1 + 0.4 * np.sin(2 * math.pi * 5 * np.arange(n) / RATE)))
    return loop_filter(motor + thumps + grind, lambda x: lowpass(x, 2500))


def disassembler_loop():
    length = 2.0
    whir = tone_layer(length, ((140, 0.4), (280, 0.18), (420, 0.06)), 1.5, 0.2)

    def tick(_):
        n = samples(0.04)
        t = np.arange(n) / RATE
        return resonator(noise(n), 1800, 3.0) * np.exp(-t / 0.006)

    ticks = pulses(length, 6.0, tick, 0.03) * 0.16
    air = noise_layer(length, lambda n: lowpass(resonator(noise(n), 900, 0.9), 2000) * 0.1)
    return loop_filter(whir + ticks + air, lambda x: lowpass(x, 3200))


def generator_hum():
    length = 4.0
    hum = tone_layer(length, ((55, 0.6), (110.25, 0.3), (165, 0.1), (220.5, 0.04)), 0.5, 0.08)
    buzz = noise_layer(length, lambda n: resonator(noise(n), 330, 4.0) * 0.06)
    return loop_filter(hum + buzz, lambda x: lowpass(x, 1200))


def disassembler_done():
    out = np.zeros(samples(0.6))
    n = samples(0.2)
    t = np.arange(n) / RATE
    thunk = np.sin(2 * math.pi * 115 * t) * np.exp(-t / 0.05) * (1 - np.exp(-t / 0.003)) + lowpass(noise(n), 700) * np.exp(-t / 0.025) * 0.5
    place(out, thunk * 0.8, 0.0)
    place(out, blip(660, 680, 0.2, 0.004, 0.06) * 0.45, 0.1)
    place(out, blip(990, 1000, 0.25, 0.004, 0.08) * 0.4, 0.18)
    return soft_clip(lowpass(out, 4500), 1.05)


for name, make, rms in (
        ("furnaceLoop", furnace_loop, 0.07),
        ("crusherLoop", crusher_loop, 0.07),
        ("disassemblerLoop", disassembler_loop, 0.06),
        ("generatorHum", generator_hum, 0.07)):
    sound = make()
    save(name, sound, loudness_peak(sound, rms), fade_edges=False)

for name, make, rms in (
        ("disassemblerDone", disassembler_done, 0.07)):
    sound = make()
    save(name, sound, loudness_peak(sound, rms))
