import sys, os, wave, math
import numpy as np

RATE = 44100
rng = np.random.default_rng(7)
out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(out_dir, exist_ok=True)


def samples(seconds):
    return int(seconds * RATE)


def curve(values, n):
    points = np.linspace(0, n - 1, len(values))
    return np.interp(np.arange(n), points, values)


def wander(n, rate, amount):
    steps = max(2, int(n / RATE * rate) + 2)
    return curve(rng.uniform(-1, 1, steps), n) * amount


def resonator(x, freq, q):
    freq = np.broadcast_to(freq, x.shape).astype(float)
    y = np.zeros_like(x)
    y1 = y2 = x1 = x2 = 0.0
    for i in range(len(x)):
        w = 2 * math.pi * min(freq[i], RATE * 0.45) / RATE
        alpha = math.sin(w) / (2 * q)
        cos_w = math.cos(w)
        a0 = 1 + alpha
        b0, b2 = alpha / a0, -alpha / a0
        a1, a2 = -2 * cos_w / a0, (1 - alpha) / a0
        v = b0 * x[i] + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, x[i]
        y2, y1 = y1, v
        y[i] = v
    return y


def lowpass(x, cutoff):
    a = math.exp(-2 * math.pi * cutoff / RATE)
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def noise(n):
    return rng.uniform(-1, 1, n)


def envelope(n, points):
    times = [p[0] for p in points]
    values = [p[1] for p in points]
    return np.interp(np.linspace(0, 1, n), times, values)


def glottal(f0, jitter, shimmer):
    n = len(f0)
    out = np.zeros(n)
    pos = 0.0
    while pos < n:
        i = int(pos)
        period = RATE / max(20.0, f0[i] * (1 + rng.normal(0, jitter)))
        width = max(2, int(period * 0.35))
        pulse = np.hanning(width * 2)[:width] * (1 + rng.normal(0, shimmer))
        end = min(n, i + width)
        out[i:end] += pulse[:end - i]
        pos += period
    return out - lowpass(out, 40)


def voice(f0, formants, breath, jitter=0.04, shimmer=0.25):
    n = len(f0)
    source = glottal(f0, jitter, shimmer) + noise(n) * breath
    out = np.zeros(n)
    for freq, q, gain in formants:
        out += resonator(source, freq, q) * gain
    return out


def bubble(seconds, start_freq, rise, decay):
    n = samples(seconds)
    t = np.arange(n) / RATE
    freq = start_freq * (1 + rise * t / seconds)
    phase = np.cumsum(freq) / RATE * 2 * math.pi
    return np.sin(phase) * np.exp(-t / decay) * (1 - np.exp(-t / 0.002))


def squelch(seconds, low, high, q=4.0):
    n = samples(seconds)
    sweep = curve([low, high * rng.uniform(0.8, 1.1), low * 1.3], n)
    env = envelope(n, [(0, 0), (0.08, 1), (0.5, 0.6), (1, 0)])
    return resonator(noise(n), sweep, q) * env


def crunch(seconds, grains, low, high):
    n = samples(seconds)
    out = np.zeros(n)
    for _ in range(grains):
        length = rng.integers(samples(0.001), samples(0.005))
        start = rng.integers(0, max(1, n - length))
        grain = noise(length) * np.hanning(length) * rng.uniform(0.3, 1.0)
        out[start:start + length] += grain
    center = rng.uniform(low, high)
    return resonator(out, center, 1.2) * 1.6 + lowpass(lowpass(out, high), high) * 0.25


def place(target, sound, start):
    i = int(start * RATE)
    end = min(len(target), i + len(sound))
    if end > i: target[i:end] += sound[:end - i]


def soft_clip(x, drive):
    return np.tanh(x * drive) / np.tanh(drive)


def save(name, x, peak):
    x = x - np.mean(x)
    m = np.max(np.abs(x))
    if m > 0: x = x / m * peak
    fade = samples(0.006)
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
    d = 1.1
    n = samples(d)
    f0 = 70 + wander(n, 6, 12) + curve([0, 10, -6], n)
    growl = voice(f0, [(curve([380, 520, 420], n), 5, 1.0), (curve([1100, 1300, 1000], n), 7, 0.45)], 0.5, 0.08, 0.4)
    growl *= envelope(n, [(0, 0), (0.15, 0.6), (0.5, 0.35), (0.8, 0.5), (1, 0)])
    x = growl * 0.7
    t = 0.1
    while t < d - 0.15:
        place(x, bubble(rng.uniform(0.04, 0.09), rng.uniform(160, 320), rng.uniform(0.6, 1.4), 0.02) * rng.uniform(0.2, 0.45), t)
        t += rng.uniform(0.08, 0.2)
    for _ in range(3):
        place(x, crunch(0.03, 6, 1400, 2600) * 0.25, rng.uniform(0.2, d - 0.1))
    return soft_clip(lowpass(x, 5000), 1.2)


def alert():
    d = 0.75
    n = samples(d)
    f0 = curve([95, 160, 140, 120], n) + wander(n, 25, 18)
    roar = voice(f0, [(curve([450, 900, 700], n), 4, 1.0), (curve([1300, 2300, 1800], n), 6, 0.7), (2900, 8, 0.25)], 0.9, 0.12, 0.6)
    roar *= envelope(n, [(0, 0), (0.06, 1), (0.6, 0.8), (1, 0)])
    hiss = resonator(noise(n), curve([2500, 4200, 3200], n), 1.5) * envelope(n, [(0, 0), (0.05, 0.7), (0.4, 0.5), (1, 0)])
    rattle = 1 + 0.35 * np.sign(np.sin(2 * math.pi * np.cumsum(curve([28, 36, 24], n)) / RATE)) * envelope(n, [(0, 0.3), (0.5, 1), (1, 0.5)])
    return soft_clip(lowpass((roar * 0.9 + hiss * 0.22) * rattle, 4200), 1.8)


def bite():
    d = 0.3
    x = np.zeros(samples(d))
    for start in (0.0, 0.11):
        thump = lowpass(noise(samples(0.06)), 180) * envelope(samples(0.06), [(0, 1), (1, 0)])
        place(x, thump * 3.0, start)
        place(x, crunch(0.05, 30, 900, 2800), start + 0.004)
        place(x, squelch(0.07, 350, 900, 3.0) * 0.5, start + 0.02)
    return soft_clip(lowpass(lowpass(x, 4200), 5000), 1.5)


def hurt():
    d = 0.45
    n = samples(d)
    f0 = curve([330, 420, 260], n) + wander(n, 30, 30)
    cry = voice(f0, [(curve([700, 950, 600], n), 5, 1.0), (curve([1900, 2500, 1700], n), 7, 0.6)], 0.6, 0.1, 0.5)
    cry *= envelope(n, [(0, 0), (0.05, 1), (0.35, 0.7), (1, 0)])
    x = cry
    place(x, squelch(0.12, 300, 1100, 3.5) * 0.6, 0.0)
    return soft_clip(x, 1.4)


def death():
    d = 1.5
    n = samples(d)
    f0 = curve([150, 120, 80, 50], n) + wander(n, 18, 14)
    moan = voice(f0, [(curve([650, 500, 350, 260], n), 4, 1.0), (curve([1600, 1300, 900, 700], n), 6, 0.5)], 0.7, curve([0.05, 0.15, 0.3], n).mean(), 0.6)
    moan *= envelope(n, [(0, 0), (0.05, 1), (0.4, 0.7), (0.75, 0.4), (1, 0)])
    x = moan * 0.8
    place(x, squelch(0.25, 250, 1200, 3.0) * 0.8, 0.0)
    t = 0.3
    while t < d - 0.1:
        place(x, bubble(rng.uniform(0.05, 0.12), rng.uniform(120, 260), rng.uniform(0.5, 1.2), 0.03) * rng.uniform(0.3, 0.6) * (1 - t / d), t)
        t += rng.uniform(0.05, 0.15)
    place(x, squelch(0.3, 180, 600, 2.5) * 0.5, d - 0.45)
    return soft_clip(lowpass(x, 5500), 1.3)


def eat():
    d = 0.95
    x = np.zeros(samples(d))
    for start in (0.0, 0.32, 0.62):
        place(x, squelch(0.18, rng.uniform(250, 350), rng.uniform(800, 1200), 3.5) * 0.8, start)
        place(x, crunch(0.1, 40, 1000, 2600) * 0.7, start + 0.05)
        place(x, bubble(0.05, rng.uniform(200, 300), 1.0, 0.015) * 0.3, start + 0.12)
    return soft_clip(lowpass(lowpass(x, 3800), 4500), 1.3)


save("parasiteIdle", idle(), 0.5)
save("parasiteAlert", alert(), 0.8)
save("parasiteBite", bite(), 0.75)
save("parasiteHurt", hurt(), 0.7)
save("parasiteDeath", death(), 0.75)
save("parasiteEat", eat(), 0.55)
