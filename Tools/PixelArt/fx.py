"""Effects added in the revamp: hit sparks, death puffs, dust motes, and the shadow under creatures."""

import math
import random

from pixelart import blank, put, rgba


def _noise(x, y, seed):
    return random.Random(x * 7919 + y * 104729 + seed * 1299709).random()


SMOKE_FRAMES = 5


def smoke(frame):
    """A puff that swells and thins out. Used when something dies."""
    img = blank(16, 16)
    puffs = [(5.5, 9.5, 3.2), (10.0, 9.0, 3.0), (7.5, 6.0, 3.4)]
    grow = [0.6, 1.0, 1.25, 1.4, 1.5][frame]
    thin = [0.0, 0.0, 0.2, 0.45, 0.7][frame]
    rise = [0, 0, -1, -2, -3][frame]
    for y in range(16):
        for x in range(16):
            n = _noise(x, y, 200 + frame)
            if n < thin:
                continue
            for px, py, r in puffs:
                d = math.hypot(x - px, y - (py + rise))
                if d < r * grow:
                    shade = "bn3" if y < py + rise - r * 0.3 and x < px else "bn2" if d < r * grow * 0.7 else "bn1"
                    put(img, x, y, shade)
                    break
    return img


HIT_FRAMES = 3


def hit(frame):
    """A white impact star for melee hits."""
    img = blank(16, 16)
    cx, cy = 7, 8
    lengths = [3, 6, 7][frame]
    start = [0, 2, 4][frame]
    for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)]:
        diag = dx != 0 and dy != 0
        for i in range(start, lengths - (2 if diag else 0)):
            put(img, cx + dx * i, cy + dy * i, "white" if i < lengths - 2 else "au3")
    if frame == 0:
        for x in range(cx - 1, cx + 2):
            for y in range(cy - 1, cy + 2):
                put(img, x, y, "white")
    return img


def pixel():
    img = blank(2, 2)
    for x in range(2):
        for y in range(2):
            put(img, x, y, "white")
    return img


def shadow():
    """Soft ground shadow under creatures: a squashed ellipse of translucent ink."""
    img = blank(16, 16)
    c = rgba("ink", 110)
    for x in range(3, 13):
        img.putpixel((x, 15), c)
    for x in range(4, 12):
        img.putpixel((x, 14), c)
    for x in range(5, 11):
        img.putpixel((x, 13), c)
    return img
