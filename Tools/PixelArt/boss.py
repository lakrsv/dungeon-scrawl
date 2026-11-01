"""The Scrawl, the level 5 boss, and its bridge (see BOSS.md).

The bridge is drawn in the world tiles' palette, so it belongs to the dungeon. The Scrawl is drawn like the original
creatures (an ink outline, the DawnBringer colours), only enormous: a body of ink edged in plum so it reads against
the void, gold runes, red eyes, and a pale bone mask.

The big shapes are built from simple geometry, then shaded by rule: lit from the top-left, rimmed, outlined in ink.
"""

import math
import random

from PIL import Image

from pixelart import outline, rgba

INK = "#140c1c"

# The ink body: dark, with a violet sheen, so it reads against the void without stopping being ink.
BODY = "#241a33"
BODY_MID = "#33264a"
BODY_LIGHT = "#4a3866"
RIM = "#7d5aa0"
GOLD = "#dad45e"
EMBER = "#d27d2c"
RED = "#d04648"
CREAM = "#deeed6"
BONE = "#c4c9b8"
BONE_SHADE = "#8595a1"
BONE_DEEP = "#4e4a4e"


# ---- Shape helpers ---------------------------------------------------------------------------------------------


class Shape:
    """A set of pixels, built up from simple pieces."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = set()

    def ellipse(self, cx, cy, rx, ry):
        for y in range(self.h):
            for x in range(self.w):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                    self.px.add((x, y))
        return self

    def rect(self, x0, y0, x1, y1, r=0):
        """Inclusive corners, with the corners rounded by r."""
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                dx = max(x0 + r - x, 0, x - (x1 - r))
                dy = max(y0 + r - y, 0, y - (y1 - r))
                if dx * dx + dy * dy <= r * r + 0.5:
                    self.px.add((x, y))
        return self

    def cut(self, other):
        self.px -= other.px
        return self

    def __contains__(self, p):
        return p in self.px


def shade(shape, base, light, dark, rim=None):
    """Lit from the top-left: a pixel with open space above or to its left is lit, one with open space below or
    to its right is in shade. The rim colour (if any) lines the lower-right edge, as if lit from behind."""
    img = Image.new("RGBA", (shape.w, shape.h), (0, 0, 0, 0))
    for (x, y) in shape.px:
        colour = base
        if (x, y - 1) not in shape or (x - 1, y) not in shape:
            colour = light
        elif rim and ((x + 1, y) not in shape or (x, y + 1) not in shape):
            colour = rim
        elif (x + 1, y + 1) not in shape or (x, y + 2) not in shape:
            colour = dark
        img.putpixel((x, y), rgba(colour))
    return img


def put(img, x, y, colour):
    if 0 <= x < img.size[0] and 0 <= y < img.size[1]:
        img.putpixel((x, y), rgba(colour))


def glow(img, colour, alpha=150):
    """A soft one-pixel halo outside the outline, for things that are charging."""
    w, h = img.size
    out = Image.new("RGBA", (w + 2, h + 2), (0, 0, 0, 0))
    out.alpha_composite(img, (1, 1))
    src = out.copy().load()
    dst = out.load()
    c = rgba(colour, alpha)
    for y in range(h + 2):
        for x in range(w + 2):
            if src[x, y][3]:
                continue
            if any(0 <= x + dx < w + 2 and 0 <= y + dy < h + 2 and src[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                dst[x, y] = c
    return out


def mirror(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


# ---- The bridge ------------------------------------------------------------------------------------------------

# Planks run across the bridge, four to a tile, each 3 high with a dark gap. The outer columns are the side beams
# with a chain along them.
PLANK = ["wd3", "wd2", "wd2"]


def _bridge(side, seed):
    rng = random.Random(seed)
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for plank in range(4):
        y0 = plank * 4
        shift = rng.randrange(3)
        for y in range(3):
            for x in range(16):
                colour = PLANK[y]
                if y == 0 and rng.random() < 0.18:
                    colour = "wd4"
                if y == 2 and rng.random() < 0.12:
                    colour = "wd1"
                put(img, x, y0 + y, colour)
        for x in range(16):
            put(img, x, y0 + 3, "wd0")
        # A seam where two planks meet, and the odd nail.
        seam = (5 + shift * 4) % 16
        for y in range(3):
            put(img, seam, y0 + y, "wd1")
        if rng.random() < 0.6:
            put(img, (seam + 2) % 16, y0 + 1, "st5")
    if side:
        # The side beam: dark wood with a lit top, and a chain hanging along it.
        xs = range(0, 3) if side < 0 else range(13, 16)
        for y in range(16):
            for i, x in enumerate(xs):
                edge = (i == 0) if side < 0 else (i == 2)
                put(img, x, y, "wd0" if edge else ("wd2" if i == 1 else "wd1"))
        chain = 1 if side < 0 else 14
        for y in range(16):
            if y % 4 in (0, 1):
                put(img, chain, y, "st6" if y % 4 == 0 else "st4")
    return img


def bridge_tiles():
    """Left, middle and right, two variants each."""
    out = {}
    for name, side in (("BridgeLeft", -1), ("BridgeMid", 0), ("BridgeRight", 1)):
        for v in range(2):
            out["%s%d" % (name, v)] = _bridge(side, 100 + v * 7 + side)
    return out


# ---- Shapes from geometry --------------------------------------------------------------------------------------


def polygon(shape, points):
    """Fills a polygon into the shape (scanline, pixel centres)."""
    n = len(points)
    for y in range(shape.h):
        cy = y + 0.5
        xs = []
        for i in range(n):
            (x0, y0), (x1, y1) = points[i], points[(i + 1) % n]
            if (y0 <= cy < y1) or (y1 <= cy < y0):
                xs.append(x0 + (cy - y0) * (x1 - x0) / (y1 - y0))
        xs.sort()
        for a, b in zip(xs[0::2], xs[1::2]):
            for x in range(int(math.ceil(a - 0.5)), int(math.floor(b - 0.5)) + 1):
                if 0 <= x < shape.w:
                    shape.px.add((x, y))
    return shape


def stroke(shape, points, r0, r1, out=None):
    """A tapering stroke along a polyline, from radius r0 to r1. `out` collects (pixel, how far along) pairs."""
    lengths = [math.hypot(points[i + 1][0] - points[i][0], points[i + 1][1] - points[i][1]) for i in range(len(points) - 1)]
    total = sum(lengths)
    done = 0.0
    for i, seg in enumerate(lengths):
        (x0, y0), (x1, y1) = points[i], points[i + 1]
        steps = max(1, int(seg * 3))
        for k in range(steps + 1):
            t = k / steps
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            along = (done + seg * t) / total
            r = r0 + (r1 - r0) * along
            for px in range(int(x - r - 1), int(x + r + 2)):
                for py in range(int(y - r - 1), int(y + r + 2)):
                    if (px + 0.5 - x) ** 2 + (py + 0.5 - y) ** 2 <= r * r + 0.15:
                        if 0 <= px < shape.w and 0 <= py < shape.h:
                            shape.px.add((px, py))
                            if out is not None and ((px, py) not in out or out[(px, py)] < along):
                                out[(px, py)] = along
        done += seg
    return shape


def _inside(shape, p):
    x, y = p
    return all((x + dx, y + dy) in shape for dx in (-1, 0, 1) for dy in (-1, 0, 1))


def _veins(img, shape, rng, starts, steps, colour=GOLD):
    """Handwriting glowing under the ink: lines that wander like a pen, gold at heart and ember at the ends."""
    for sx, sy in starts:
        x, y = sx, sy
        heading = rng.choice((0, 1))
        trail = []
        for _ in range(steps):
            if not _inside(shape, (x, y)):
                break
            trail.append((x, y))
            if rng.random() < 0.35:
                heading = 1 - heading
            if heading == 0:
                x += 1
                y += rng.choice((-1, 0, 0, 1))
            else:
                y += rng.choice((-1, 1))
                x += rng.choice((0, 1))
        for i, (x, y) in enumerate(trail):
            put(img, x, y, colour if 0 < i < len(trail) - 1 else EMBER)


# ---- The hands -------------------------------------------------------------------------------------------------

# The left hand, palm down, its long fingers reaching right for the bridge. Each finger is three bones, slightly
# curled, ending in a bone talon.


def _finger_path(kx, ky, angle, length, curl):
    """Three bones from the knuckle, each bending a little further (curl, in degrees per joint)."""
    pts = [(kx, ky)]
    a = math.radians(angle)
    for bone, part in enumerate((0.42, 0.33, 0.25)):
        a += math.radians(curl)
        x, y = pts[-1]
        pts.append((x + math.cos(a) * length * part, y + math.sin(a) * length * part))
    return pts


def _claw(fingers, thumb, palm, wrist, w, h, glowing):
    s = Shape(w, h)
    polygon(s, palm)
    polygon(s, wrist)
    along = {}
    paths = []
    for kx, ky, angle, length, curl in fingers + [thumb]:
        path = _finger_path(kx, ky, angle, length, curl)
        paths.append(path)
        stroke(s, path, 1.6, 0.6, along)
    img = shade(s, BODY_MID, BODY_LIGHT, BODY, RIM)
    # The knuckles and joints catch the light.
    for path in paths:
        for (x, y) in path[:-1]:
            put(img, int(x), int(y) - 1, BODY_LIGHT)
    # Bone talons on the last stretch of every finger.
    for (x, y), t in along.items():
        if t > 0.8:
            put(img, x, y, CREAM if t > 0.92 else BONE_SHADE)
    # A sigil of handwriting glowing on the back of the hand.
    _veins(img, s, random.Random(7), [(palm[0][0] + 3, (palm[0][1] + palm[3][1]) // 2)], 12, CREAM if glowing else GOLD)
    img = outline(img, INK)
    if glowing:
        img = glow(glow(img, RED, 210), RED, 90)
    return img


def claw_spread(charging=False):
    """Raised, fingers splayed, about to slam down talons first."""
    fingers = [(18, 10, -32, 20, 8), (19, 14, -12, 23, 7), (19, 19, 8, 22, 6), (18, 23, 26, 18, 5)]
    thumb = (12, 25, 62, 12, -10)
    palm = [(6, 11), (18, 8), (20, 24), (8, 27)]
    wrist = [(0, 14), (8, 12), (9, 25), (0, 23)]
    return _claw(fingers, thumb, palm, wrist, 46, 40, charging)


def claw_grip():
    """Talons hooked over the edge of the bridge after a slam: open to attack."""
    fingers = [(18, 9, -8, 17, 10), (19, 13, -2, 19, 11), (19, 18, 3, 18, 11), (18, 22, 9, 15, 10)]
    thumb = (12, 24, 60, 10, 12)
    palm = [(6, 10), (18, 7), (20, 23), (8, 26)]
    wrist = [(0, 13), (8, 11), (9, 24), (0, 22)]
    return _claw(fingers, thumb, palm, wrist, 42, 34, False)


# ---- The head --------------------------------------------------------------------------------------------------


def _horn(shape, base, tip, width, along):
    """A pen nib for a horn: wide at the hood, sweeping to a point."""
    mid = ((base[0] + tip[0]) / 2 - (tip[1] - base[1]) * 0.15, (base[1] + tip[1]) / 2)
    stroke(shape, [base, mid, tip], width, 0.4, along)


def mask(cracked=False, charging=False):
    """A long bone mask with a pointed chin, hollow eyes and a mouth stitched shut, in a peaked hood of ink that
    frays into the void. Two pen nibs sweep up from the hood like horns."""
    w, h = 64, 76
    cx = 32

    hood = Shape(w, h)
    polygon(hood, [(cx, 10), (cx + 11, 14), (cx + 19, 24), (cx + 23, 40), (cx + 26, 58), (cx + 28, 66),
                   (cx + 21, 61), (cx + 17, 70), (cx + 12, 62), (cx + 6, 73), (cx, 64), (cx - 6, 73), (cx - 12, 62),
                   (cx - 17, 70), (cx - 21, 61), (cx - 28, 66), (cx - 26, 58), (cx - 23, 40), (cx - 19, 24),
                   (cx - 11, 14)])
    nibs = {}
    for side in (-1, 1):
        _horn(hood, (cx + side * 13, 18), (cx + side * 27, 1), 3.2, nibs)
    img = shade(hood, BODY, BODY_LIGHT, BODY, RIM)
    # The nibs' slits, gold, and their points, ink.
    for side in (-1, 1):
        for i in range(3, 13):
            t = i / 16
            x = int(cx + side * (13 + 14 * t) - (-17) * 0.15 * 4 * t * (1 - t))
            put(img, x, int(18 - 17 * t), GOLD if i < 9 else INK)
    # Deep shadow inside the hood, round the face.
    inner = Shape(w, h)
    polygon(inner, [(cx, 15), (cx + 15, 24), (cx + 18, 42), (cx + 12, 60), (cx, 66), (cx - 12, 60), (cx - 18, 42),
                    (cx - 15, 24)])
    for (x, y) in inner.px:
        if (x, y) in hood:
            put(img, x, y, INK if y > 20 else BODY)

    face = Shape(w, h)
    top = 19
    polygon(face, [(cx - 7, top + 3), (cx, top), (cx + 7, top + 3), (cx + 12, top + 11), (cx + 13, top + 20),
                   (cx + 10, top + 28), (cx + 6, top + 37), (cx, top + 45), (cx - 6, top + 37), (cx - 10, top + 28),
                   (cx - 13, top + 20), (cx - 12, top + 11)])
    bone = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for (x, y) in face.px:
        # Lit from the left: the left half bone, the right half in shade, with a hard line down the nose.
        left = x < cx
        colour = BONE if left else BONE_SHADE
        if left and ((x - 1, y) not in face or (x, y - 1) not in face):
            colour = CREAM
        if not left and ((x + 1, y) not in face or (x, y + 1) not in face):
            colour = BONE_DEEP
        bone.putpixel((x, y), rgba(colour))
    img.alpha_composite(bone)

    # Brows in a scowl, lit; the hollows of the cheeks.
    for side in (-1, 1):
        for i in range(7):
            put(img, cx + side * (11 - i), top + 9 + i // 2, CREAM if side < 0 else BONE)
        for i in range(6):
            put(img, cx + side * (9 - i // 3), top + 26 + i, BONE_SHADE if side < 0 else BONE_DEEP)

    # Eye sockets: deep, slanted down to the nose, a pinpoint of fire in each.
    for side in (-1, 1):
        socket = Shape(w, h)
        polygon(socket, [(cx + side * 11, top + 12), (cx + side * 3, top + 15), (cx + side * 3, top + 19),
                         (cx + side * 9, top + 18)])
        for (x, y) in socket.px:
            put(img, x, y, INK)
        ex = cx + side * 6
        if charging:
            put(img, ex, top + 16, CREAM)
            put(img, ex + side, top + 16, GOLD)
            put(img, ex, top + 17, GOLD)
        else:
            put(img, ex, top + 16, RED)
            put(img, ex + side, top + 16, EMBER)

    # The nose: two narrow slits.
    for y in range(top + 23, top + 27):
        put(img, cx - 1, y, BONE_DEEP)
        put(img, cx + 1, y, INK)

    # The mouth, stitched shut; for its beam, the stitches burst and it glows.
    my = top + 33
    for x in range(cx - 6, cx + 7):
        put(img, x, my, GOLD if charging else INK)
    if charging:
        for x in range(cx - 4, cx + 5):
            put(img, x, my - 1, EMBER)
            put(img, x, my + 1, EMBER)
    else:
        for x in range(cx - 5, cx + 6, 2):
            put(img, x, my - 1, INK)
            put(img, x, my + 1, INK)

    # A sigil on the brow.
    for x, y in ((0, 4), (0, 5), (0, 6), (0, 7), (-1, 5), (1, 5), (-2, 7), (2, 7)):
        put(img, cx + x, top + y, GOLD)

    if cracked:
        rng = random.Random(4)
        x, y = cx + 4, top + 2
        while y < top + 30:
            put(img, x, y, INK)
            x += rng.choice((0, 0, 1, -1)) if x < cx + 10 else -1
            y += 1

    img = outline(img, INK)
    return glow(img, RED, 120) if charging else img


# ---- The beam --------------------------------------------------------------------------------------------------


def beam(length, glyphs):
    """A lane-wide stream of glowing letters: the head's spoken word."""
    w = 14
    img = Image.new("RGBA", (w, length), (0, 0, 0, 0))
    for y in range(length):
        for x in range(w):
            edge = min(x, w - 1 - x)
            if edge == 0:
                img.putpixel((x, y), rgba(RED, 120))
            elif edge == 1:
                img.putpixel((x, y), rgba(EMBER, 170))
            else:
                img.putpixel((x, y), rgba(GOLD, 90))
    for i, g in enumerate(glyphs):
        img.alpha_composite(g, ((w - g.size[0]) // 2, 2 + i * 8))
    return img


def arm(length=120, angle=32):
    """The left forearm: from the wrist (top right) down and out into the void (bottom left), tapering wider and
    dithering away as it sinks out of the light. Returns the image and the wrist's pixel, where it meets the hand."""
    a = math.radians(angle)
    ux, uy = -math.sin(a), math.cos(a)
    nx, ny = -uy, ux
    w = int(length * math.sin(a)) + 22
    h = int(length * math.cos(a)) + 12
    x0, y0 = w - 11, 5
    s = Shape(w, h)
    along = {}
    for i in range(length):
        half = 4 + 3 * i / length
        for j in range(int(-half * 2), int(half * 2) + 1):
            o = j / 2
            x, y = int(round(x0 + ux * i + nx * o)), int(round(y0 + uy * i + ny * o))
            if 0 <= x < w and 0 <= y < h:
                s.px.add((x, y))
                along[(x, y)] = max(along.get((x, y), 0.0), i / length)
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for (x, y) in s.px:
        # Which side of the arm: lit on the upper edge, rimmed on the lower.
        o = (x - x0) * nx + (y - y0) * ny
        half = 4 + 3 * along[(x, y)]
        edge = (o + half) / (2 * half)
        colour = BODY_LIGHT if edge > 0.8 else (RIM if edge < 0.2 else BODY_MID)
        img.putpixel((x, y), rgba(colour))
    img = outline(img, INK)
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    px = img.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0:
                continue
            t = along.get((x, y), along.get((x - 1, y), along.get((x + 1, y), along.get((x, y - 1), 1.0))))
            fade = max(0.0, (t - 0.3) / 0.7)
            if fade * 16 > bayer[y % 4][x % 4]:
                px[x, y] = (0, 0, 0, 0)
    return img, (x0, y0)


def beam_segment():
    """One tile of the head's beam, to repeat down a lane: a hot core between ember and red edges."""
    w = 14
    img = Image.new("RGBA", (w, 16), (0, 0, 0, 0))
    for y in range(16):
        for x in range(w):
            edge = min(x, w - 1 - x)
            colour = RED if edge == 0 else (EMBER if edge == 1 else (GOLD if edge < 4 else CREAM))
            img.putpixel((x, y), rgba(colour, 140 if edge == 0 else (190 if edge == 1 else 110)))
    return img


def sprites():
    """(name, image, pivot) for the Boss sheet. Pivots are centres, except the arm's, which is its wrist."""
    out = [
        ("ClawSpread", claw_spread(), (0.5, 0.5)),
        ("ClawSpreadCharging", claw_spread(True), (0.5, 0.5)),
        ("ClawGrip", claw_grip(), (0.5, 0.5)),
        ("Mask", mask(), (0.5, 0.5)),
        ("MaskCharging", mask(charging=True), (0.5, 0.5)),
        ("MaskCracked", mask(cracked=True), (0.5, 0.5)),
        ("MaskCrackedCharging", mask(cracked=True, charging=True), (0.5, 0.5)),
    ]
    arm_img, (wx, wy) = arm()
    out.append(("Arm", arm_img, (wx / arm_img.size[0], 1.0 - wy / arm_img.size[1])))
    out.append(("BeamSegment", beam_segment(), (0.5, 0.5)))
    white = Image.new("RGBA", (4, 4), (255, 255, 255, 255))
    out.append(("White", white, (0.5, 0.5)))
    return out
