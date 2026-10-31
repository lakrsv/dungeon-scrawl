"""Shared palette and drawing helpers for the DungeonScrawl pixel art.

Every sprite in the game is drawn from PALETTE. Sprites are authored as rows of characters, one character per
pixel, with a legend that maps characters to palette names. '.' is always transparent.
"""

from PIL import Image

# One palette for the whole game. Ramps are hue-shifted: shadows lean purple/blue, highlights lean warm.
PALETTE = {
    # Darks
    "void": "#07060b",
    "ink": "#100d18",
    "night": "#1b1727",
    # Stone: cool slate
    "st0": "#25203a",
    "st1": "#312c49",
    "st2": "#3d3a59",
    "st3": "#4b4a6a",
    "st4": "#5c5e7e",
    "st5": "#737895",
    "st6": "#959cb3",
    "st7": "#c0c6d6",
    # Wood and gold
    "wd0": "#2a1820",
    "wd1": "#45252a",
    "wd2": "#67372f",
    "wd3": "#8d4f37",
    "wd4": "#b26e42",
    "au1": "#d4934a",
    "au2": "#ecc160",
    "au3": "#fbe79a",
    # Reds
    "rd0": "#3b0e25",
    "rd1": "#6b1633",
    "rd2": "#a3223c",
    "rd3": "#d64040",
    "rd4": "#f07858",
    "rd5": "#ffb48c",
    # Greens
    "gr0": "#0f2620",
    "gr1": "#1a4232",
    "gr2": "#286b3d",
    "gr3": "#479a45",
    "gr4": "#86c85b",
    "gr5": "#cdeb8f",
    # Blues and teals
    "bl0": "#141f3e",
    "bl1": "#1b3a66",
    "bl2": "#255f92",
    "bl3": "#3a92bc",
    "bl4": "#6fc9d6",
    "bl5": "#c2f1ea",
    # Purples
    "pu0": "#28123e",
    "pu1": "#481e66",
    "pu2": "#743294",
    "pu3": "#a952c0",
    "pu4": "#de8de0",
    # Bone and parchment
    "bn0": "#5f5050",
    "bn1": "#958272",
    "bn2": "#c6b398",
    "bn3": "#e6dbbe",
    "bn4": "#fbf6e6",
    # Skin
    "sk0": "#6e3a33",
    "sk1": "#a8634c",
    "sk2": "#d9976c",
    "sk3": "#f2c79a",
    "white": "#ffffff",
}

OUTLINE = "ink"


def rgba(name_or_hex, alpha=255):
    value = PALETTE.get(name_or_hex, name_or_hex)
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


def grid(rows, legend, size=None):
    """Builds an image from rows of characters. legend maps a character to a palette name (or None)."""
    height = len(rows)
    width = max(len(r) for r in rows)
    if size is not None:
        width, height = size
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == "." or ch == " ":
                continue
            if ch not in legend:
                raise KeyError("Character '%s' at (%d, %d) has no legend entry" % (ch, x, y))
            name = legend[ch]
            if name is None:
                continue
            px[x, y] = rgba(name)
    return img


def outline(img, color=OUTLINE, diagonals=False):
    """Adds a 1px outline around every opaque pixel."""
    w, h = img.size
    src = img.load()
    out = img.copy()
    dst = out.load()
    c = rgba(color)
    offsets = [(1, 0), (-1, 0), (0, 1), (0, -1)]
    if diagonals:
        offsets += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
    for y in range(h):
        for x in range(w):
            if src[x, y][3] != 0:
                continue
            for dx, dy in offsets:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] != 0 and src[nx, ny][:3] != c[:3]:
                    dst[x, y] = c
                    break
    return out


def recolor(img, mapping):
    """Swaps palette colors. mapping maps palette names to palette names."""
    lookup = {rgba(k)[:3]: rgba(v)[:3] for k, v in mapping.items()}
    out = img.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in lookup:
                px[x, y] = lookup[(r, g, b)] + (a,)
    return out


def shift_region(img, box, dx, dy):
    """Moves the pixels inside box (x0, y0, x1, y1) by (dx, dy). Pixels moved over are replaced."""
    region = img.crop(box)
    out = img.copy()
    blank = Image.new("RGBA", region.size, (0, 0, 0, 0))
    out.paste(blank, box[:2])
    out.alpha_composite(region, (box[0] + dx, box[1] + dy))
    return out


def bob(img, waist):
    """Idle 'breathing' frame: everything above the waist row sinks by one pixel."""
    w, _ = img.size
    lower = img.crop((0, waist, w, img.size[1]))
    upper = img.crop((0, 0, w, waist))
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.alpha_composite(lower, (0, waist))
    out.alpha_composite(upper, (0, 1))
    return out


def overlay(base, top, pos=(0, 0)):
    out = base.copy()
    out.alpha_composite(top, pos)
    return out


def flip(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def put(img, x, y, color):
    if 0 <= x < img.size[0] and 0 <= y < img.size[1]:
        img.putpixel((x, y), rgba(color))


def rect(img, x0, y0, x1, y1, color):
    """Fills the inclusive rectangle."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(img, x, y, color)


def colors_used(img):
    names = {rgba(v)[:3]: k for k, v in PALETTE.items()}
    used = set()
    for r, g, b, a in img.getdata():
        if a:
            used.add(names.get((r, g, b), "#%02x%02x%02x" % (r, g, b)))
    return used


class Sheet:
    """Packs sprites into a grid and records their rects for the Unity importer.

    Rects are stored with a bottom-left origin, which is what Unity uses. Each cell can be extruded so that
    tiles never sample their neighbours.
    """

    def __init__(self, name, cell=(16, 16), columns=8, padding=0, extrude=0):
        self.name = name
        self.cell = cell
        self.columns = columns
        self.padding = padding
        self.extrude = extrude
        self.items = []

    def add(self, sprite_name, img, pivot=(0.5, 0.5), border=None, size=None):
        self.items.append({"name": sprite_name, "img": img, "pivot": pivot, "border": border, "size": size})

    def build(self):
        cw, ch = self.cell
        stride_x = cw + self.padding + self.extrude * 2
        stride_y = ch + self.padding + self.extrude * 2
        rows = (len(self.items) + self.columns - 1) // self.columns
        width = _pow2(stride_x * min(self.columns, len(self.items)) + self.padding)
        height = _pow2(stride_y * rows + self.padding)
        atlas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        meta = []
        for i, item in enumerate(self.items):
            col, row = i % self.columns, i // self.columns
            x = self.padding + col * stride_x + self.extrude
            y = self.padding + row * stride_y + self.extrude
            img = item["img"]
            iw, ih = img.size
            atlas.alpha_composite(img, (x, y))
            if self.extrude:
                _extrude(atlas, x, y, iw, ih, self.extrude)
            meta.append(
                {
                    "name": item["name"],
                    "x": x,
                    "y": height - y - ih,
                    "w": iw,
                    "h": ih,
                    "pivotX": item["pivot"][0],
                    "pivotY": item["pivot"][1],
                    "border": list(item["border"]) if item["border"] else [0, 0, 0, 0],
                }
            )
        return atlas, meta


def _pow2(v):
    p = 1
    while p < v:
        p *= 2
    return p


def _extrude(atlas, x, y, w, h, amount):
    px = atlas.load()
    for i in range(1, amount + 1):
        for yy in range(y, y + h):
            px[x - i, yy] = px[x, yy]
            px[x + w - 1 + i, yy] = px[x + w - 1, yy]
        for xx in range(x - i, x + w + i):
            px[xx, y - i] = px[min(max(xx, x), x + w - 1), y]
            px[xx, y + h - 1 + i] = px[min(max(xx, x), x + w - 1), y + h - 1]


def preview(images, scale=6, columns=8, background="#2a2438", gap=4):
    """Upscaled contact sheet for checking art by eye."""
    cells = []
    for img in images:
        cells.append(img.resize((img.size[0] * scale, img.size[1] * scale), Image.NEAREST))
    cw = max(c.size[0] for c in cells)
    ch = max(c.size[1] for c in cells)
    rows = (len(cells) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * (cw + gap) + gap, rows * (ch + gap) + gap), rgba(background))
    for i, c in enumerate(cells):
        x = gap + (i % columns) * (cw + gap)
        y = gap + (i // columns) * (ch + gap)
        sheet.alpha_composite(c, (x, y))
    return sheet
