"""A true pixel font for TextMesh Pro: Munro Small, one font pixel per atlas pixel.

TextMesh Pro samples fonts at whole point sizes, but Munro's pixel grid is 10.24 points, so its own font assets
come out with letters running into each other. This renders each glyph at exactly 10.24 points instead, packs
them into an atlas, and writes their metrics for ImportFont.cs to build the font asset from.

Metrics are in font pixels, with the em at 16: so a prompt at font size 1 (UGUI) or a popup at font size 10
(world space, which TextMesh Pro scales by 0.1) draws one font pixel per art pixel, 1/16 of a world unit.
"""

import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
SOURCE = os.path.join(PROJECT, "Assets", "Fonts", "munro", "MunroSmall.ttf")
ATLAS = os.path.join(PROJECT, "Assets", "Fonts", "munro", "MunroSmallPixel.png")
MANIFEST = os.path.join(HERE, "font.tsv")

# The size at which one font pixel is one image pixel.
PIXEL_SIZE = 10.24
ATLAS_SIZE = (128, 64)
GUTTER = 1

# Face metrics, in font pixels. The ascent and descent are trimmed to the capitals (5 high) plus one pixel each,
# so that middle alignment centres the capitals exactly.
FACE = {
    "pointSize": 16,
    "lineHeight": 8,
    "ascentLine": 6,
    "capLine": 5,
    "meanLine": 5,
    "baseline": 0,
    "descentLine": -1,
    "underlineOffset": -1,
    "underlineThickness": 1,
    "strikethroughOffset": 2,
    "tabWidth": 12,
}


def _glyphs():
    font = ImageFont.truetype(SOURCE, PIXEL_SIZE)
    for code in range(32, 127):
        ch = chr(code)
        canvas = Image.new("L", (24, 24), 0)
        draw = ImageDraw.Draw(canvas)
        draw.fontmode = "1"
        origin = (4, 16)
        draw.text(origin, ch, font=font, fill=255, anchor="ls")
        ink = canvas.getbbox()
        advance = int(round(font.getlength(ch)))
        if ink is None:
            yield code, None, 0, 0, advance
            continue
        glyph = canvas.crop(ink)
        yield code, glyph, ink[0] - origin[0], origin[1] - ink[1], advance


def build():
    atlas = Image.new("RGBA", ATLAS_SIZE, (255, 255, 255, 0))
    lines = ["face\t" + "\t".join("%s=%s" % kv for kv in FACE.items())]
    x, y, row = GUTTER, GUTTER, 0
    for code, glyph, bearing_x, bearing_y, advance in _glyphs():
        if glyph is None:
            lines.append("glyph\t%d\t0\t0\t0\t0\t0\t0\t%d" % (code, advance))
            continue
        w, h = glyph.size
        if x + w + GUTTER > ATLAS_SIZE[0]:
            x, y, row = GUTTER, y + row + GUTTER, 0
        mask = glyph.point(lambda v: 255 if v > 127 else 0)
        atlas.paste((255, 255, 255, 255), (x, y), mask)
        # Unity's atlas rects start at the bottom-left.
        lines.append("glyph\t%d\t%d\t%d\t%d\t%d\t%d\t%d\t%d" % (
            code, x, ATLAS_SIZE[1] - y - h, w, h, bearing_x, bearing_y, advance))
        x += w + GUTTER
        row = max(row, h)
    if y + row + GUTTER > ATLAS_SIZE[1]:
        raise ValueError("the font atlas is too small")
    atlas.save(ATLAS)
    with open(MANIFEST, "w", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    return atlas


if __name__ == "__main__":
    build()
