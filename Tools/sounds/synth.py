import os, wave, math
import numpy as np

RATE = 44100
rng = np.random.default_rng(0)
_out_dir = "."


def setup(seed, out_dir):
    global rng, _out_dir
    rng = np.random.default_rng(seed)
    _out_dir = out_dir
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


def save(name, x, peak, fade_edges=True):
    x = x - np.mean(x)
    m = np.max(np.abs(x))
    if m > 0: x = x / m * peak
    if fade_edges:
        fade = samples(0.006)
        x[:fade] *= np.linspace(0, 1, fade)
        x[-fade:] *= np.linspace(1, 0, fade)
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(_out_dir, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print(name, f"{len(data) / RATE:.2f}s")


def output_to(out_dir):
    global _out_dir
    _out_dir = out_dir
    os.makedirs(out_dir, exist_ok=True)


def loudness_peak(x, target_rms, max_peak=0.6):
    x = x / max(1e-9, np.max(np.abs(x)))
    rms = math.sqrt(float(np.mean(x ** 2)))
    return min(max_peak, target_rms / max(1e-9, rms))


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def sine(freq, n, phase=0.0):
    freq = np.broadcast_to(freq, (n,)).astype(float)
    return np.sin(2 * math.pi * np.cumsum(freq) / RATE + phase)


def periodic(freq, loop_seconds):
    return max(1, round(freq * loop_seconds)) / loop_seconds


def crossfade_loop(x, loop_n):
    fade = len(x) - loop_n
    out = x[:loop_n].copy()
    if fade <= 0: return out
    w = np.linspace(0, 1, fade)
    out[:fade] = x[:fade] * np.sqrt(w) + x[loop_n:loop_n + fade] * np.sqrt(1 - w)
    return out


def loop_filter(x, filter_fn):
    return filter_fn(np.concatenate([x, x]))[len(x):]


def blip(freq_start, freq_end, seconds, attack, decay):
    n = samples(seconds)
    t = np.arange(n) / RATE
    env = (1 - np.exp(-t / attack)) * np.exp(-t / decay)
    return sine(curve([freq_start, freq_end], n), n) * env


def bell(freq, seconds, decay, partials=((1.0, 1.0), (2.76, 0.22), (5.4, 0.06))):
    n = samples(seconds)
    t = np.arange(n) / RATE
    out = np.zeros(n)
    for ratio, gain in partials:
        out += np.sin(2 * math.pi * freq * ratio * t) * gain * np.exp(-t / (decay / ratio ** 0.5))
    return out * (1 - np.exp(-t / 0.002))
