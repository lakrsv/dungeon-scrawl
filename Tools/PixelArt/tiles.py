"""Dungeon tiles: floor platforms floating over the abyss, and the cliff face that hangs below them.

The floor autotiles over its 8 neighbours using the 47-case blob layout. Every tile is assembled from four
8x8 quadrants, so lighting stays consistent (no rotated sprites).
"""

import random

from pixelart import blank, grid, outline, put, rect, rgba

N, NE, E, SE, S, SW, W, NW = 1, 2, 4, 8, 16, 32, 64, 128


def canonical(mask):
    """Drops diagonal bits whose two neighbouring sides are not both floor. Gives the 47 blob cases."""
    if not (mask & N and mask & E):
        mask &= ~NE
    if not (mask & S and mask & E):
        mask &= ~SE
    if not (mask & S and mask & W):
        mask &= ~SW
    if not (mask & N and mask & W):
        mask &= ~NW
    return mask


def blob_masks():
    return sorted({canonical(m) for m in range(256)})


# ----------------------------------------------------------------------------------------------------------
# Floor surface

def _slab(img, x0, y0, w, h, rng, tone):
    """One flagstone. The bottom row and right column are grout; the top row catches the light."""
    fill, light, speck = tone
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            if y == y0 + h - 1 or x == x0 + w - 1:
                put(img, x, y, "st2")
            elif y == y0 and x < x0 + w - 2:
                put(img, x, y, light)
            elif x == x0 and y < y0 + h - 2:
                put(img, x, y, light)
            else:
                put(img, x, y, fill)
    # Grout corners are a touch darker.
    put(img, x0 + w - 1, y0 + h - 1, "st1")
    # Wear: specks and the odd chipped corner.
    for _ in range(rng.randint(1, 2)):
        put(img, x0 + rng.randint(2, w - 3), y0 + rng.randint(2, h - 3), speck)
    if rng.random() < 0.4:
        put(img, x0 + w - 2, y0 + h - 2, "st2")


# Flagstone tones: (fill, lit edge, speck). Mostly mid slate, some darker or worn stones.
TONES = [
    ("st3", "st4", "st2"),
    ("st3", "st4", "st2"),
    ("st3", "st4", "st4"),
    ("st2", "st3", "st1"),
    ("st3", "st5", "st2"),
]


def floor_base(seed=0):
    """A tile of irregular flagstones laid in two rows. Slabs never cross the tile edge, so any tile can sit
    next to any other."""
    rng = random.Random(seed)
    img = blank(16, 16)
    for y0 in (0, 8):
        cut = rng.choice([None, 5, 6, 7, 8, 9, 10, 11])
        spans = [(0, 16)] if cut is None else [(0, cut), (cut, 16 - cut)]
        for x0, w in spans:
            _slab(img, x0, y0, w, 8, rng, rng.choice(TONES))
    return img


def _decorate(img, kind, rng):
    if kind == "crack":
        x, y = 3, 3
        for step in [(1, 0), (1, 1), (0, 1), (1, 1), (1, 0), (1, 1), (0, 1), (1, 0)]:
            put(img, x, y, "st1")
            x, y = x + step[0], y + step[1]
        put(img, 6, 4, "st4")
    elif kind == "moss":
        for x, y, c in [(6, 6, "gr1"), (7, 6, "gr2"), (8, 6, "gr1"), (7, 5, "gr2"), (9, 7, "gr1"), (5, 7, "gr1"),
                        (7, 7, "gr2"), (8, 7, "gr3"), (11, 14, "gr1"), (12, 14, "gr2"), (12, 13, "gr1")]:
            put(img, x, y, c)
    elif kind == "pebbles":
        for x, y in [(4, 10), (10, 4), (12, 11)]:
            put(img, x, y, "st5")
            put(img, x + 1, y, "st4")
            put(img, x, y + 1, "st2")
            put(img, x + 1, y + 1, "st2")
    elif kind == "bones":
        for x, y, c in [(5, 9, "bn1"), (6, 10, "bn2"), (7, 11, "bn2"), (8, 12, "bn1"), (4, 9, "bn2"), (5, 8, "bn2"),
                        (8, 13, "bn2"), (9, 12, "bn2"), (10, 5, "bn1"), (11, 5, "bn2")]:
            put(img, x, y, c)
    elif kind == "rune":
        # A faint glyph scrawled on the stone.
        for x, y in [(5, 4), (6, 4), (7, 4), (6, 5), (6, 6), (5, 7), (7, 7), (8, 6), (9, 5), (10, 4), (10, 6), (9, 7),
                     (10, 8), (5, 9), (6, 10), (7, 11), (8, 10), (9, 9)]:
            put(img, x, y, "st5")
    elif kind == "puddle":
        for y, (x0, x1) in enumerate([(6, 9), (4, 11), (4, 11), (5, 10)]):
            for x in range(x0, x1 + 1):
                put(img, x, 9 + y, "bl1")
        for x in range(6, 9):
            put(img, x, 10, "bl2")
        put(img, 9, 10, "st5")
    elif kind == "grate":
        rect(img, 4, 4, 11, 11, "st1")
        for x in range(5, 11, 2):
            for y in range(5, 11):
                put(img, x, y, "st0")
        for x in range(4, 12):
            put(img, x, 4, "st4")
    return img


DECORATIONS = ["crack", "moss", "pebbles", "bones", "rune", "puddle", "grate"]


def floor_variants():
    """Interior floor tiles: plain slabs (weighted heavily) and decorated ones."""
    plain = [floor_base(seed) for seed in range(4)]
    decorated = []
    for i, kind in enumerate(DECORATIONS):
        decorated.append(_decorate(floor_base(10 + i), kind, random.Random(i)))
    return plain, decorated


# ----------------------------------------------------------------------------------------------------------
# Rims. Each side of a floor tile that faces the abyss gets a lit lip; corners are rounded off.

TOP = [("st5", 0), ("st4", 1)]
LEFT = [("st5", 0), ("st4", 1)]
RIGHT = [("st1", 15), ("st2", 14)]
BOTTOM = [("st6", 15), ("st5", 14)]


def _draw_quadrant(img, qx, qy, vertical_open, horizontal_open, diagonal_open):
    """qx/qy select the quadrant. *_open means that neighbour is abyss."""
    xs = range(qx * 8, qx * 8 + 8)
    ys = range(qy * 8, qy * 8 + 8)
    vrim = TOP if qy == 0 else BOTTOM
    hrim = LEFT if qx == 0 else RIGHT
    cx = 0 if qx == 0 else 15
    cy = 0 if qy == 0 else 15
    inward_x = 1 if qx == 0 else -1
    inward_y = 1 if qy == 0 else -1

    if vertical_open:
        for color, row in vrim:
            for x in xs:
                put(img, x, row, color)
    if horizontal_open:
        for color, col in hrim:
            for y in ys:
                put(img, col, y, color)
    if vertical_open and horizontal_open:
        # Outer corner: round it off.
        img.putpixel((cx, cy), (0, 0, 0, 0))
        put(img, cx + inward_x, cy, vrim[0][0])
        put(img, cx, cy + inward_y, hrim[0][0])
        put(img, cx + inward_x, cy + inward_y, vrim[1][0])
    elif vertical_open:
        pass
    elif horizontal_open:
        pass
    elif diagonal_open:
        # Inner corner: a small notch of rim where the abyss touches diagonally.
        put(img, cx, cy, vrim[0][0])
        put(img, cx + inward_x, cy, vrim[1][0])
        put(img, cx, cy + inward_y, hrim[1][0])


def floor_tile(mask, base):
    img = base.copy()
    n, e, s, w = bool(mask & N), bool(mask & E), bool(mask & S), bool(mask & W)
    _draw_quadrant(img, 0, 0, not n, not w, not (mask & NW))
    _draw_quadrant(img, 1, 0, not n, not e, not (mask & NE))
    _draw_quadrant(img, 0, 1, not s, not w, not (mask & SW))
    _draw_quadrant(img, 1, 1, not s, not e, not (mask & SE))
    return img


# ----------------------------------------------------------------------------------------------------------
# Cliff: the rock face below a floor edge, with stalactites dripping into the dark.

# Stalactite layouts: (centre column, length in pixels). Every layout meets the tile edges at the face depth,
# so any layout can sit next to any other.
_FACE_DEPTH = 7
_LAYOUTS = [
    [(4, 15), (11, 13)],
    [(3, 11), (8, 16), (13, 11)],
    [(5, 12), (10, 15)],
]


def _cliff_length(x, spikes):
    length = _FACE_DEPTH
    for cx, spike in spikes:
        length = max(length, spike - abs(x - cx) * 2)
    return min(length, 16)


def cliff_tile(layout, left_open, right_open):
    spikes = _LAYOUTS[layout]
    img = blank(16, 16)
    for x in range(16):
        length = _cliff_length(x, spikes)
        # The left face of each stalactite catches the light.
        nearest = min(spikes, key=lambda s: abs(x - s[0]))[0]
        lit = x < nearest
        for y in range(length):
            if y == 0:
                c = "st1"
            elif y <= 2:
                c = "st3"
            elif y < _FACE_DEPTH:
                c = "st2"
            elif y < length - 2:
                c = "st2" if lit else "st1"
            elif y < length - 1:
                c = "st1" if lit else "st0"
            else:
                c = "st0"
            put(img, x, y, c)
        if x == nearest - 1 and length > _FACE_DEPTH + 2:
            for y in range(_FACE_DEPTH, length - 3):
                put(img, x, y, "st3")
    # Strata in the face: a seam, a few cracks, a few lit chips.
    for x in range(16):
        if (x + layout) % 6 != 0:
            put(img, x, 3, "st1")
    for x, y in [(4, 1), (4, 2), (9, 4), (9, 5), (13, 1), (13, 2)]:
        put(img, (x + layout * 5) % 16, y, "st1")
    for x, y in [(1, 1), (7, 1), (11, 5), (2, 5)]:
        put(img, (x + layout * 3) % 16, y, "st4")
    if left_open:
        for y in range(16):
            img.putpixel((0, y), (0, 0, 0, 0))
        for y in range(3, 16):
            img.putpixel((1, y), (0, 0, 0, 0))
    if right_open:
        for y in range(16):
            img.putpixel((15, y), (0, 0, 0, 0))
        for y in range(3, 16):
            img.putpixel((14, y), (0, 0, 0, 0))
    return img


CLIFF_LAYOUTS = len(_LAYOUTS)


def cliff_tiles():
    """Indexed by layout * 4 + (left_open ? 1 : 0) + (right_open ? 2 : 0)."""
    out = []
    for layout in range(CLIFF_LAYOUTS):
        for left_open, right_open in [(False, False), (True, False), (False, True), (True, True)]:
            out.append(cliff_tile(layout, left_open, right_open))
    return out
