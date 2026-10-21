import sys
import synth
from synth import *

synth.setup(7, sys.argv[1] if len(sys.argv) > 1 else ".")
rng = synth.rng


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
