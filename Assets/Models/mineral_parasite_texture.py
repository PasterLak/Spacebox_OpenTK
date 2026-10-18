import math
import random
import sys

from PIL import Image

SIZE = 64

BODY = (0, 0, 32, 32)
MOUTH = (32, 0, 64, 32)
SPIKE = (0, 32, 16, 64)
BACK = (16, 32, 32, 48)
LIP = (32, 32, 48, 48)

PURPLE = (96, 38, 150)
PURPLE_DARK = (66, 22, 108)
PURPLE_LIGHT = (128, 62, 186)
FLESH = (176, 22, 34)
FLESH_DARK = (96, 6, 16)
THROAT = (40, 0, 8)
TOOTH = (236, 232, 236)
TOOTH_SHADE = (196, 188, 204)


def shade(color, amount):
    return tuple(max(0, min(255, int(c + amount))) for c in color)


def body(img, rect, rng, veins):
    x0, y0, x1, y1 = rect
    for y in range(y0, y1):
        for x in range(x0, x1):
            roll = rng.random()
            color = PURPLE_LIGHT if roll < 0.12 else PURPLE_DARK if roll < 0.3 else PURPLE
            color = shade(color, rng.randint(-6, 6))
            if x in (x0, x1 - 1) or y in (y0, y1 - 1):
                color = shade(PURPLE_DARK, -12)
            img.putpixel((x, y), color)

    if not veins:
        return

    for _ in range(4):
        x = rng.randint(x0 + 2, x1 - 3)
        y = rng.randint(y0 + 2, y1 - 3)
        for _ in range(10):
            img.putpixel((x, y), FLESH_DARK if rng.random() < 0.5 else FLESH)
            x = max(x0 + 1, min(x1 - 2, x + rng.choice((-1, 0, 1))))
            y = max(y0 + 1, min(y1 - 2, y + rng.choice((-1, 0, 1))))


def mouth(img, rect, rng):
    x0, y0, x1, y1 = rect
    cx = (x0 + x1 - 1) / 2
    cy = (y0 + y1 - 1) / 2
    teeth = 10
    for y in range(y0, y1):
        for x in range(x0, x1):
            dx = x - cx
            dy = y - cy
            r = math.hypot(dx, dy)
            angle = (math.atan2(dy, dx) / (2 * math.pi)) % 1.0
            sector = (angle * teeth) % 1.0
            tooth_width = max(0.0, (r - 4.5) / 9.0)

            if r > 14.5:
                color = shade(PURPLE_DARK, rng.randint(-8, 4))
            elif r > 12.5:
                color = shade(FLESH_DARK, rng.randint(-6, 6))
            elif r < 3.5:
                color = THROAT
            elif abs(sector - 0.5) < tooth_width * 0.5:
                color = TOOTH if sector < 0.5 else TOOTH_SHADE
            else:
                color = FLESH if rng.random() > 0.15 else FLESH_DARK
                color = shade(color, -int((12.5 - r) * 4))

            img.putpixel((x, y), color)


def spike(img, rect, rng):
    x0, y0, x1, y1 = rect
    height = y1 - y0
    for y in range(y0, y1):
        t = (y - y0) / (height - 1)
        base = tuple(int(TOOTH[i] * (1 - t) + (150, 132, 176)[i] * t) for i in range(3))
        for x in range(x0, x1):
            color = shade(base, rng.randint(-8, 8))
            if x == x0 or (x - x0) % 5 == 0:
                color = shade(color, -24)
            img.putpixel((x, y), color)


def lip(img, rect, rng):
    x0, y0, x1, y1 = rect
    for y in range(y0, y1):
        for x in range(x0, x1):
            color = PURPLE_DARK if (y - y0) % 4 else shade(PURPLE_DARK, -16)
            img.putpixel((x, y), shade(color, rng.randint(-6, 6)))


def build(path):
    rng = random.Random(59640)
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 255))
    body(img, BODY, rng, False)
    mouth(img, MOUTH, rng)
    spike(img, SPIKE, rng)
    body(img, BACK, rng, True)
    lip(img, LIP, rng)
    img.save(path)


if __name__ == "__main__":
    build(sys.argv[1])
