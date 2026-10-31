"""Backdrops and the button frame for the splash screen, main menu and game over screen.

The splash screen and the main menu share one backdrop, so moving from one to the other never changes the scene.

Each is a small scene built from the game's own tiles and original sprites, lit by torchlight and fading into
the dark at the edges. They are 480x270, drawn at 4x on a 1920x1080 screen.
"""

import math
import os
import random

from PIL import Image

import fx
import tiles
from idle import original
from pixelart import grid

WIDTH, HEIGHT = 480, 270

# The colour the screens fade into: the dark the game's camera clears to, so every screen meets the next seamlessly.
EDGE = (0x07, 0x06, 0x0B)


def _sprite(name):
    paths = {
        "knight": "Resources/Sprites/Player.png",
        "bones": "Graphics/DeathSprite.png",
        "chest": "Graphics/Items/Chest1.png",
        "door": "Graphics/Items/Door.psd",
    }
    for kind in ("Zombie", "Demon", "Slime"):
        for i in range(1, 5):
            paths["%s%d" % (kind.lower(), i)] = "Graphics/Enemies/%s%d.png" % (kind, i)
    return original(paths[name])


def _floor_map(rows):
    h, w = len(rows), len(rows[0])

    def floor(x, y):
        return 0 <= x < w and 0 <= y < h and rows[y][x] == "#"

    return floor, w, h


def _paint_dungeon(rows, seed):
    floor, w, h = _floor_map(rows)
    plain, decorated = tiles.floor_variants()
    cliffs = tiles.cliff_tiles()
    rng = random.Random(seed)
    img = Image.new("RGBA", (w * 16, h * 16), EDGE + (255,))
    for y in range(h):
        for x in range(w):
            if floor(x, y):
                mask = 0
                for bit, dx, dy in [(tiles.N, 0, -1), (tiles.NE, 1, -1), (tiles.E, 1, 0), (tiles.SE, 1, 1),
                                    (tiles.S, 0, 1), (tiles.SW, -1, 1), (tiles.W, -1, 0), (tiles.NW, -1, -1)]:
                    if floor(x + dx, y + dy):
                        mask |= bit
                mask = tiles.canonical(mask)
                if mask == tiles.canonical(255):
                    tile = rng.choice(decorated) if rng.random() < 0.12 else rng.choice(plain)
                else:
                    tile = tiles.floor_tile(mask, plain[rng.randrange(2)])
                img.alpha_composite(tile, (x * 16, y * 16))
            elif floor(x, y - 1):
                left = not (floor(x - 1, y - 1) and not floor(x - 1, y))
                right = not (floor(x + 1, y - 1) and not floor(x + 1, y))
                layout = rng.randrange(tiles.CLIFF_LAYOUTS)
                img.alpha_composite(cliffs[layout * 4 + left + 2 * right], (x * 16, y * 16))
    return img


def _place(img, sprite, x, y, flip=False, shadow=True):
    """Places a 16x16 sprite on tile (x, y). Fractions offset within the tile."""
    if flip:
        sprite = sprite.transpose(Image.FLIP_LEFT_RIGHT)
    px, py = int(round(x * 16)), int(round(y * 16))
    if shadow:
        img.alpha_composite(fx.shadow(), (px, py))
    img.alpha_composite(sprite, (px, py))


def _light(img, lights, gloom, tint=(1.0, 1.0, 1.0)):
    """Torchlight, per tile like the game: each light is (tile x, tile y, radius, strength)."""
    px = img.load()
    w, h = img.size
    near = (1.00, 0.96, 0.86)
    far = (0.52, 0.48, 0.62)
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            tx, ty = x // 16 + 0.5, y // 16 + 0.5
            best = None
            for lx, ly, radius, strength in lights:
                d = math.hypot(tx - lx, ty - ly)
                if d <= radius:
                    t = (d / radius) ** 2
                    c = [(n + (f - n) * t) * strength + gloom[i] * (1 - strength) for i, (n, f) in enumerate(zip(near, far))]
                    if best is None or sum(c) > sum(best):
                        best = c
            light = best or gloom
            px[x, y] = (int(r * light[0] * tint[0]), int(g * light[1] * tint[1]), int(b * light[2] * tint[2]), a)


def _vignette(img, strength=1.0):
    """Fades smoothly into the edge colour, more at the corners."""
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            dx = (x - w / 2.0) / (w / 2.0)
            dy = (y - h / 2.0) / (h / 2.0)
            d = min(1.0, math.hypot(dx * 0.85, dy) / 1.15)
            k = max(0.0, (d - 0.45) / 0.55)
            k = min(1.0, k * k * (3 - 2 * k) * strength)
            r, g, b, a = px[x, y]
            px[x, y] = (int(r + (EDGE[0] - r) * k), int(g + (EDGE[1] - g) * k), int(b + (EDGE[2] - b) * k), 255)


def _crop(img, ox, oy):
    out = Image.new("RGBA", (WIDTH, HEIGHT), EDGE + (255,))
    out.alpha_composite(img.crop((ox, oy, ox + WIDTH, oy + HEIGHT)))
    return out


# Splash and main menu: the knight on one side, the dungeon's creatures on the other, the choice between them.
MENU_MAP = [
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    ".######..................####.",
    ".######..................####.",
    ".#######################.####.",
    ".######.................#####.",
    ".######..................####.",
    ".######..................####.",
    "...#.........................",
    "...#...............#######....",
    "...#################.......",
    "...................#######....",
    "..............................",
    "..............................",
]


def main_menu():
    rows = [r.ljust(30, ".") for r in MENU_MAP]
    img = _paint_dungeon(rows, 5)
    _place(img, _sprite("chest"), 2, 7)
    _place(img, _sprite("knight"), 4, 9.5, flip=True)
    _place(img, _sprite("demon1"), 26, 8)
    _place(img, _sprite("zombie3"), 27, 10)
    _place(img, _sprite("slime1"), 25, 10.5)
    _place(img, _sprite("bones"), 22, 13.5, shadow=False)
    _light(img, [(4.5, 9.8, 4.5, 1.0), (26.5, 9.5, 4.0, 0.8)], gloom=(0.22, 0.24, 0.38))
    img = _crop(img, 0, 8)
    _vignette(img)
    return img


# Game over: what is left of the knight, and the things that got him, under a red glow.
GAME_OVER_MAP = [
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    "..#####..................####.",
    "..#####..................####.",
    "..##########......########.##.",
    "..#####....#......#.....#####.",
    "..#####....##########...####..",
    "...........###########........",
    "...........###########........",
    "..............................",
    "..............................",
    "..............................",
    "..............................",
    "..............................",
]


def game_over():
    img = _paint_dungeon(GAME_OVER_MAP, 9)
    _place(img, _sprite("bones"), 15, 11.8, shadow=False)
    _place(img, _sprite("bones"), 13.4, 12.1, shadow=False)
    _place(img, _sprite("demon1"), 4, 7.5, flip=True)
    _place(img, _sprite("zombie2"), 3, 9)
    _place(img, _sprite("demon4"), 26, 7)
    _place(img, _sprite("zombie4"), 27, 9, flip=False)
    _place(img, _sprite("slime4"), 17.6, 11.9)
    _light(img, [(15.5, 11.5, 4.5, 0.9), (4.0, 8.5, 3.5, 0.6), (26.5, 8.0, 3.5, 0.6)],
           gloom=(0.20, 0.16, 0.24), tint=(1.0, 0.62, 0.66))
    img = _crop(img, 0, 24)
    _vignette(img, 1.1)
    return img


# The menu buttons: the original outlined box, as a cream frame with gold rivets around a dark face. 9-sliced, 4px.
BUTTON = [
    ".kkkkkkkkkkkkkkkkkkkkkk.",
    "kYCCCCCCCCCCCCCCCCCCCCYk",
    "kCkkkkkkkkkkkkkkkkkkkkCk",
    "kCkPPPPPPPPPPPPPPPPPPkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkFFFFFFFFFFFFFFFFFFkCk",
    "kCkkkkkkkkkkkkkkkkkkkkCk",
    "kYCCCCCCCCCCCCCCCCCCCCYk",
    "kSSSSSSSSSSSSSSSSSSSSSSk",
    ".kkkkkkkkkkkkkkkkkkkkkk.",
]
BUTTON_LEGEND = {"k": "#140c1c", "Y": "#dad45e", "C": "#deeed6", "P": "#30202e", "F": "#1e1424", "S": "#8595a1"}
BUTTON_BORDER = (4, 4, 4, 4)


def button():
    return grid(BUTTON, BUTTON_LEGEND)


# The volume buttons' icons: a note for the music and a speaker for the sound, then three rising bars lit up to the
# level. At level 0 the glyph greys out and every bar is dark.
MUSIC_GLYPH = [
    "..CCCCC",
    "..C...C",
    "..C...C",
    "..C...C",
    "..C...C",
    "..C...C",
    ".CC..CC",
    "CCC.CCC",
    ".C...C.",
]

SOUND_GLYPH = [
    "....C",
    "...CC",
    "..CCC",
    "CCCCC",
    "CCCCC",
    "CCCCC",
    "..CCC",
    "...CC",
    "....C",
]

VOLUME_LEVELS = 3


def volume_icon(glyph, level):
    width = len(glyph[0]) + 2 + 2 * VOLUME_LEVELS - 1
    height = len(glyph)
    img = Image.new("RGBA", (width, height + 1), (0, 0, 0, 0))
    lit = BUTTON_LEGEND["C"] if level else BUTTON_LEGEND["S"]
    for y, row in enumerate(glyph):
        for x, ch in enumerate(row):
            if ch == "C":
                img.putpixel((x, y), _hex(lit))
    for i in range(VOLUME_LEVELS):
        x = len(glyph[0]) + 2 + 2 * i
        for y in range(height - 3 * (i + 1), height):
            img.putpixel((x, y), _hex(BUTTON_LEGEND["Y"] if i < level else "#442434"))

    # A one-pixel ink drop shadow, like the button labels'.
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    for y in range(height):
        for x in range(width):
            if img.getpixel((x, y))[3] and not img.getpixel((x, y + 1))[3]:
                shadow.putpixel((x, y + 1), _hex(BUTTON_LEGEND["k"]))
    shadow.alpha_composite(img)
    return shadow


def _hex(value):
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), 255)
