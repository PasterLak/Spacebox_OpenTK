import os
import sys

from PIL import Image

GAMESET = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Spacebox", "GameSets", "Default")
BLOCKS = os.path.join(GAMESET, "Blocks", "Lights")
EMISSIONS = os.path.join(GAMESET, "Blocks", "Lights", "Emissions")

COLORS = ["white", "blue", "red", "yellow", "cyan", "orange", "green", "pink"]

SIZE = 32
FACE_X0 = 8
FACE_W = 16

BASE = (89, 89, 89, 255)
LIGHT = (112, 112, 112, 255)
HIGHLIGHT = (131, 131, 131, 255)
SHADOW = (68, 68, 68, 255)
DARK = (49, 49, 49, 255)
DARKEST = (27, 27, 27, 255)

LIGHT_INTERIOR = (3, 3, 29, 29)
WINDOW_TOP = 6
WINDOW_BOTTOM = 26

TOP_BAND = [DARK, LIGHT, BASE, BASE, SHADOW, DARKEST]
BOTTOM_BAND = [DARKEST, LIGHT, BASE, BASE, SHADOW, DARK]


def fill(px, x0, y0, x1, y1, color):
    for y in range(y0, y1):
        for x in range(x0, x1):
            px[x, y] = color


def band(px, y0, rows):
    for i, color in enumerate(rows):
        fill(px, 0, y0 + i, SIZE, y0 + i + 1, color)
    x0, x1 = FACE_X0, FACE_X0 + FACE_W
    for i in range(1, len(rows) - 1):
        px[x0, y0 + i] = SHADOW
        px[x1 - 1, y0 + i] = SHADOW
    for x in (x0 + 2, x1 - 3):
        px[x, y0 + 2] = DARKEST
        px[x, y0 + 3] = DARKEST


def window(source):
    return source.crop(LIGHT_INTERIOR).resize((SIZE, WINDOW_BOTTOM - WINDOW_TOP), Image.BOX)


def side_texture(light):
    img = Image.new("RGBA", (SIZE, SIZE), BASE)
    px = img.load()
    band(px, 0, TOP_BAND)
    band(px, WINDOW_BOTTOM, BOTTOM_BAND)
    img.paste(window(light), (0, WINDOW_TOP))
    return img


def side_emission(emission):
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    img.paste(window(emission), (0, WINDOW_TOP))
    return img


def cap_texture():
    img = Image.new("RGBA", (SIZE, SIZE), BASE)
    px = img.load()
    x0, x1 = FACE_X0, FACE_X0 + FACE_W

    for i in range(x0, x1):
        px[i, x0] = DARK
        px[i, x1 - 1] = DARK
        px[x0, i] = DARK
        px[x1 - 1, i] = DARK
    for i in range(x0 + 1, x1 - 1):
        px[i, x0 + 1] = LIGHT
        px[x0 + 1, i] = LIGHT
        px[i, x1 - 2] = SHADOW
        px[x1 - 2, i] = SHADOW

    a, b = x0 + 4, x1 - 4
    for i in range(a, b):
        px[i, a] = SHADOW
        px[a, i] = SHADOW
        px[i, b - 1] = LIGHT
        px[b - 1, i] = LIGHT
    px[a, b - 1] = BASE
    px[b - 1, a] = BASE

    for x, y in ((x0 + 3, x0 + 3), (x1 - 4, x0 + 3), (x0 + 3, x1 - 4), (x1 - 4, x1 - 4)):
        px[x, y] = DARKEST
        px[x + 1, y + 1] = HIGHLIGHT
    return img


def main():
    cap_texture().save(os.path.join(BLOCKS, "torchMetal.png"))

    for color in COLORS:
        light = Image.open(os.path.join(BLOCKS, color + ".png")).convert("RGBA")
        emission = Image.open(os.path.join(EMISSIONS, color + ".png")).convert("RGBA")
        name = "torch" + color.capitalize()
        side_texture(light).save(os.path.join(BLOCKS, name + ".png"))
        side_emission(emission).save(os.path.join(EMISSIONS, name + ".png"))


if __name__ == "__main__":
    main()
