import sys, math
import numpy as np
import synth
from synth import samples, curve, wander, resonator, lowpass, noise, envelope, bubble, squelch, place, soft_clip, save, RATE

synth.setup(23, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng

LOOP = 4.0
OVERLAP = 0.5


def sine(freq, n, phase=0.0):
    return np.sin(2 * math.pi * freq * np.arange(n) / RATE + phase)


def damped(freq, decay, length):
    n = samples(length)
    t = np.arange(n) / RATE
    return np.sin(2 * math.pi * freq * t) * np.exp(-t / decay) * (1 - np.exp(-t / 0.002))


def make_loopable(x, loop_n, overlap_n):
    out = x[:loop_n].copy()
    k = np.linspace(0, 1, overlap_n)
    out[:overlap_n] = out[:overlap_n] * np.sin(k * math.pi / 2) + x[loop_n:loop_n + overlap_n] * np.cos(k * math.pi / 2)
    return out


def loudness_peak(x, target_rms):
    x = x / max(1e-9, np.max(np.abs(x)))
    rms = math.sqrt(float(np.mean(x ** 2)))
    return min(0.6, target_rms / max(1e-9, rms))


def reconstruction_loop():
    n = samples(LOOP)
    total = samples(LOOP + OVERLAP)
    breath = 0.5 + 0.5 * np.sin(2 * math.pi * 0.5 * np.arange(total) / RATE)
    pad = (sine(55, total) * 0.9 + sine(82.5, total, 1.3) * 0.35 + sine(110, total, 0.4) * 0.45 + sine(110.5, total, 2.1) * 0.4 + sine(165, total, 0.9) * 0.15)
    pad = lowpass(pad, 500) * (0.75 + 0.25 * breath)

    fluid = resonator(noise(total), 520 + wander(total, 1.5, 180), 2.5) * 0.6
    for _ in range(26):
        size = rng.uniform(0.04, 0.12)
        place(fluid, bubble(size, rng.uniform(260, 620), rng.uniform(0.4, 1.2), size * 0.35) * rng.uniform(0.15, 0.4), rng.uniform(0, LOOP + OVERLAP - size))
    fluid = lowpass(fluid, 1800)

    shimmer = resonator(noise(total), 3200, 6.0) * 0.25 * (0.5 + 0.5 * np.sin(2 * math.pi * 2.0 * np.arange(total) / RATE))

    mix = soft_clip(pad * 0.55 + fluid * 0.5 + shimmer * 0.12, 1.05)
    return make_loopable(mix, n, samples(OVERLAP))


def stage_cue():
    length = 1.0
    out = np.zeros(samples(length))
    place(out, squelch(0.45, 260, 900, 3.0) * 0.7, 0)
    for i in range(5):
        size = rng.uniform(0.05, 0.1)
        place(out, bubble(size, rng.uniform(350, 700), rng.uniform(0.6, 1.4), size * 0.4) * 0.35, 0.05 + i * 0.06 + rng.uniform(0, 0.03))
    tone = damped(330, 0.22, 0.8) * 0.5 + damped(495, 0.18, 0.8) * 0.3
    place(out, lowpass(tone, 1400), 0.12)
    n = len(out)
    swell = lowpass(lowpass(noise(n), 900), 900) * 2.0 * envelope(n, [(0, 0), (0.15, 1), (0.6, 0.3), (1, 0)])
    return soft_clip(lowpass(out + swell * 0.5, 5000), 1.1)


def spawn():
    length = 1.9
    n = samples(length)
    out = np.zeros(n)
    hit = 0.16

    rise_n = samples(hit + 0.05)
    rise = resonator(noise(rise_n), curve([300, 1800], rise_n), 1.5) * envelope(rise_n, [(0, 0), (0.85, 1), (1, 0)])
    place(out, rise * 0.6, 0)

    place(out, damped(58, 0.12, 0.6) * 1.1, hit)
    burst_n = samples(0.5)
    burst = lowpass(lowpass(noise(burst_n), 700), 700) * 3.0 * np.exp(-np.arange(burst_n) / (0.09 * RATE))
    place(out, burst, hit)

    place(out, squelch(0.55, 180, 700, 3.5) * 0.8, hit + 0.02)
    for _ in range(7):
        size = rng.uniform(0.05, 0.12)
        place(out, bubble(size, rng.uniform(220, 520), rng.uniform(0.5, 1.2), size * 0.4) * 0.3, hit + rng.uniform(0.05, 0.6))

    tail_n = n - samples(hit)
    tail = resonator(noise(tail_n), curve([2600, 3400, 3000], tail_n), 5.0) * np.exp(-np.arange(tail_n) / (0.45 * RATE)) * 0.35
    place(out, tail, hit)

    pad_n = samples(1.6)
    pad = lowpass(sine(110, pad_n) * 0.6 + sine(165, pad_n) * 0.3, 600) * envelope(pad_n, [(0, 0), (0.08, 1), (0.5, 0.5), (1, 0)])
    place(out, pad * 0.5, hit)

    return soft_clip(lowpass(out, 6500), 1.15)


loop = reconstruction_loop()
save("dnaReconstructionLoop", loop, loudness_peak(loop, 0.06), fade_edges=False)
cue = stage_cue()
save("dnaReconstructionStage", cue, loudness_peak(cue, 0.07))
sound = spawn()
save("dnaReconstructionSpawn", sound, loudness_peak(sound, 0.09))
