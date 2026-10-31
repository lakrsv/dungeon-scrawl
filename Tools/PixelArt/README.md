# PixelArt

Generates part of the game's art as code: the dungeon tiles, the hit and death effects, the shadow under creatures, an idle frame for each creature, the backdrops and button frame of the splash screen, main menu, and game over screen, and the main menu's volume icons. The characters, items, and interface are hand-made sprites in `Assets/Graphics/` and `Assets/Resources/Sprites/`. This tool only reads those, to build idle frames and to stage the menu backdrops.

It lives outside `Assets/`, so Unity never imports it.

## Running it

You need Python 3 with Pillow, and the project open in the Unity Editor.

```sh
python Tools/PixelArt/build.py            # writes Assets/Graphics/{Tiles,Effects,IdleFrames,Menus,MenuIcons}.png and manifest.tsv
python Tools/PixelArt/build.py --preview  # also writes 4x previews to Tools/PixelArt/preview/

# Slice the sheets and apply pixel-art import settings in the open Editor:
unity command script run --file Tools/PixelArt/ImportArt.cs --entry ImportArt.Run
```

`ImportArt.cs` keeps sprite IDs by name, so you can rebuild the art as often as you like without breaking references.

## Files

- `pixelart.py`: the palette, the grid-of-characters sprite format, outlines, and the sheet packer.
- `tiles.py`: the floor and the cliff. Floor edges are built from four 8x8 quadrants, which gives the 47 blob cases with consistent lighting and no rotated sprites. `Assets/Scripts/Tiles/FloorTile.cs` picks among them at runtime.
- `fx.py`: hit sparks, death puffs, the shadow, and the one-pixel dust mote.
- `idle.py`: the idle frame for each creature. Everything above the creature's waist sinks one pixel. If you redraw a creature, rebuild so its idle frame matches.
- `menus.py`: the three menu backdrops, small lit scenes built from the tiles and the original sprites (480x270, shown at 4x), the 9-sliced menu button frame, and the music and sound volume icons (a glyph and three level bars, one sprite per level).
- `build.py`: packs the sheets and writes `manifest.tsv`, with one line per sprite rect, pivot, and 9-slice border.
- `ImportArt.cs`: applies the manifest in Unity.
