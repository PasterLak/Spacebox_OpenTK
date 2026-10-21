import sys, os, wave
import numpy as np

RATE = 44100
rng = np.random.default_rng(42)
out_dir = sys.argv[1]
os.makedirs(out_dir, exist_ok=True)


def t(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / RATE)
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def bandpass(x, low, high):
    return lowpass(x, high) - lowpass(x, low)


def env(n, attack, release):
    e = np.ones(n)
    a = max(1, int(attack * RATE))
    r = max(1, int(release * RATE))
    e[:a] = np.linspace(0, 1, a)
    e[-r:] *= np.linspace(1, 0, r)
    return e


def sweep(freq_start, freq_end, seconds, curve=1.0):
    tt = t(seconds)
    p = (tt / seconds) ** curve
    freq = freq_start + (freq_end - freq_start) * p
    return np.cumsum(freq) / RATE * 2 * np.pi, tt


def saturate(x, drive):
    return np.tanh(x * drive) / np.tanh(drive)


def burst(seconds, low, high, decay):
    n = int(seconds * RATE)
    noise = rng.uniform(-1, 1, n)
    b = bandpass(noise, low, high)
    return b * np.exp(-np.arange(n) / (decay * RATE))


def place(target, sound, start):
    i = int(start * RATE)
    end = min(len(target), i + len(sound))
    target[i:end] += sound[:end - i]


def save(name, x, peak=0.7):
    x = x - np.mean(x)
    m = np.max(np.abs(x))
    if m > 0: x = x / m * peak
    fade = int(0.004 * RATE)
    x[:fade] *= np.linspace(0, 1, fade)
    x[-fade:] *= np.linspace(1, 0, fade)
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(out_dir, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print(name, f"{len(data) / RATE:.2f}s")


def idle():
    d = 0.8
    phase, tt = sweep(95, 130, d)
    wobble = 0.5 + 0.5 * np.sin(2 * np.pi * 7 * tt + 1.3)
    body = np.sin(phase + 0.8 * np.sin(2 * np.pi * 23 * tt)) * wobble * env(len(tt), 0.08, 0.3) * 0.5
    x = body.copy()
    start = 0.12
    while start < d - 0.1:
        place(x, burst(0.03, 1800, 5200, 0.006) * rng.uniform(0.5, 0.9), start)
        start += rng.uniform(0.045, 0.11)
    return saturate(x, 1.6)


def alert():
    d = 0.55
    phase, tt = sweep(380, 1450, d, 0.7)
    vibrato = np.sin(2 * np.pi * 31 * tt) * 2.2
    saw = 2 * ((phase / (2 * np.pi) + vibrato / (2 * np.pi)) % 1) - 1
    screech = bandpass(saw, 300, 4200) * env(len(tt), 0.02, 0.18)
    noise = bandpass(rng.uniform(-1, 1, len(tt)), 2500, 7000) * env(len(tt), 0.01, 0.25) * 0.35
    return saturate(screech + noise, 2.5)


def bite():
    d = 0.24
    x = np.zeros(int(d * RATE))
    for start in (0.0, 0.085):
        place(x, burst(0.07, 900, 6500, 0.014), start)
        tt = t(0.09)
        place(x, np.sin(2 * np.pi * (85 - 300 * tt) * tt) * np.exp(-tt / 0.025) * 0.9, start)
    return saturate(x, 2.0)


def hurt():
    d = 0.42
    phase, tt = sweep(980, 430, d, 0.6)
    tone = np.sin(phase + 1.4 * np.sin(2 * np.pi * 42 * tt)) * env(len(tt), 0.01, 0.2)
    noise = bandpass(rng.uniform(-1, 1, len(tt)), 1200, 5000) * env(len(tt), 0.005, 0.3) * 0.3
    return saturate(tone + noise, 1.8)


def death():
    d = 1.2
    phase, tt = sweep(320, 55, d, 0.8)
    gurgle = 0.55 + 0.45 * np.sin(2 * np.pi * (14 - 8 * tt / d) * tt)
    tone = np.sin(phase + 1.1 * np.sin(2 * np.pi * 19 * tt)) * gurgle * env(len(tt), 0.02, 0.5)
    x = tone * 0.8
    start = 0.05
    while start < d - 0.2:
        place(x, burst(0.05, 300, 1800, 0.012) * rng.uniform(0.3, 0.7) * (1 - start / d), start)
        start += rng.uniform(0.06, 0.14)
    place(x, burst(0.12, 700, 6000, 0.03) * 0.8, 0.0)
    return saturate(x, 1.7)


def eat():
    d = 0.9
    x = np.zeros(int(d * RATE))
    start = 0.0
    while start < d - 0.06:
        place(x, burst(0.06, rng.uniform(500, 900), rng.uniform(3000, 5500), rng.uniform(0.008, 0.016)) * rng.uniform(0.5, 1.0), start)
        start += rng.uniform(0.07, 0.12)
    tt = t(d)
    x += np.sin(2 * np.pi * 70 * tt) * (0.5 + 0.5 * np.sin(2 * np.pi * 9 * tt)) * 0.12
    return saturate(lowpass(x, 6000), 1.5)


save("parasiteIdle", idle(), 0.55)
save("parasiteAlert", alert(), 0.75)
save("parasiteBite", bite(), 0.75)
save("parasiteHurt", hurt(), 0.7)
save("parasiteDeath", death(), 0.75)
save("parasiteEat", eat(), 0.5)
