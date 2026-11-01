"""Builds the generated sprite sheets into Assets/Graphics and writes manifest.tsv for the Unity importer.

    python Tools/PixelArt/build.py            # write the sheets and the manifest
    python Tools/PixelArt/build.py --preview  # also write upscaled previews to Tools/PixelArt/preview/

Then slice and import them in the open Editor (see README.md).
"""

import os
import sys

import boss
import fonts
import fx
import hud
import idle
import menus
import tiles
from pixelart import Sheet, preview

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT_DIR = os.path.join(PROJECT, "Assets", "Graphics")
PREVIEW_DIR = os.path.join(HERE, "preview")

# How many floor bases the edge tiles come in. Interior tiles have their own variants.
EDGE_BASES = 2


def build_tiles():
    # Extruded so tiles never bleed into their neighbours in the atlas.
    sheet = Sheet("Tiles", cell=(16, 16), columns=12, padding=0, extrude=1)
    plain, decorated = tiles.floor_variants()
    for b in range(EDGE_BASES):
        for mask in tiles.blob_masks():
            sheet.add("Floor%d_%03d" % (b, mask), tiles.floor_tile(mask, plain[b]))
    for i, img in enumerate(plain):
        sheet.add("FloorPlain_%d" % i, img)
    for kind, img in zip(tiles.DECORATIONS, decorated):
        sheet.add("FloorDecor_%s" % kind, img)
    for i, img in enumerate(tiles.cliff_tiles()):
        sheet.add("Cliff_%d" % i, img)
    for name, img in sorted(boss.bridge_tiles().items()):
        sheet.add(name, img)
    return sheet


def build_effects():
    sheet = Sheet("Effects", cell=(16, 16), columns=8, padding=2)
    for f in range(fx.HIT_FRAMES):
        sheet.add("Hit_%d" % f, fx.hit(f))
    for f in range(fx.SMOKE_FRAMES):
        sheet.add("Smoke_%d" % f, fx.smoke(f))
    sheet.add("Shadow", fx.shadow())
    sheet.add("Pixel", fx.pixel())
    return sheet


def build_idle():
    sheet = Sheet("IdleFrames", cell=(16, 16), columns=8, padding=2)
    for name, img in idle.idle_frames():
        sheet.add(name, img)
    return sheet


def build_menus():
    sheet = Sheet("Menus", cell=(menus.WIDTH, menus.HEIGHT), columns=2, padding=2)
    sheet.add("MenuBackdrop", menus.main_menu())
    sheet.add("GameOverBackdrop", menus.game_over())
    sheet.add("VictoryBackdrop", menus.victory())
    sheet.add("MenuButton", menus.button(), border=menus.BUTTON_BORDER)
    return sheet


def build_menu_icons():
    sheet = Sheet("MenuIcons", cell=(16, 12), columns=4, padding=2)
    for name, glyph in (("Music", menus.MUSIC_GLYPH), ("Sound", menus.SOUND_GLYPH)):
        for level in range(menus.VOLUME_LEVELS + 1):
            sheet.add("%s%d" % (name, level), menus.volume_icon(glyph, level))
    sheet.add("Moves", menus.move_icon())
    return sheet


def build_interface():
    sheet = Sheet("Interface", cell=(16, 16), columns=8, padding=2)
    for name, img, border in hud.all_sprites():
        sheet.add(name, img, border=border)
    return sheet


def build_boss():
    sheet = Sheet("Boss", cell=(80, 128), columns=4, padding=2)
    for name, img, pivot in boss.sprites():
        sheet.add(name, img, pivot=pivot)
    return sheet


SHEETS = [build_tiles, build_effects, build_idle, build_menus, build_menu_icons, build_interface, build_boss]


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    lines = ["ppu\t16"]
    previews = []
    for builder in SHEETS:
        sheet = builder()
        atlas, meta = sheet.build()
        atlas.save(os.path.join(OUT_DIR, sheet.name + ".png"))
        previews.append((sheet.name, atlas))
        print("%-10s %3d sprites  %dx%d" % (sheet.name, len(meta), atlas.size[0], atlas.size[1]))

        # One line per sheet and per sprite. Rects use Unity's bottom-left origin; borders are left, bottom,
        # right, top.
        lines.append("sheet\tAssets/Graphics/%s.png" % sheet.name)
        for s in meta:
            fields = [s["name"], s["x"], s["y"], s["w"], s["h"], s["pivotX"], s["pivotY"]] + s["border"]
            lines.append("\t".join(["sprite"] + [str(v) for v in fields]))

    with open(os.path.join(HERE, "manifest.tsv"), "w", newline="\n") as f:
        f.write("\n".join(lines) + "\n")

    fonts.build()
    print("Font       MunroSmallPixel.png and font.tsv")

    if "--preview" in sys.argv:
        os.makedirs(PREVIEW_DIR, exist_ok=True)
        for name, atlas in previews:
            preview([atlas], scale=4, columns=1).save(os.path.join(PREVIEW_DIR, name + ".png"))


if __name__ == "__main__":
    main()
