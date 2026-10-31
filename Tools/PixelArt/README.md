# PixelArt

Generates part of the game's art as code: the dungeon tiles, the hit and death effects, the shadow under creatures, an idle frame for each creature, the backdrops and button frame of the splash screen, main menu, game over and victory screens, the main menu's volume icons, the in-game interface (the prompt plaques and their icons, the arrows, and the status panel's hearts, gems and icons), and a pixel font for TextMesh Pro. The characters and items are hand-made sprites in `Assets/Graphics/` and `Assets/Resources/Sprites/`. This tool only reads those, to build idle frames and to stage the menu backdrops.

It lives outside `Assets/`, so Unity never imports it.

## Running it

You need Python 3 with Pillow, and the project open in the Unity Editor.

```sh
python Tools/PixelArt/build.py            # writes Assets/Graphics/{Tiles,Effects,IdleFrames,Menus,MenuIcons,Interface}.png and manifest.tsv,
                                          # and the pixel font's atlas (Assets/Fonts/munro/MunroSmallPixel.png) and font.tsv
python Tools/PixelArt/build.py --preview  # also writes 4x previews to Tools/PixelArt/preview/

# Slice the sheets and apply pixel-art import settings in the open Editor:
unity command script run --file Tools/PixelArt/ImportArt.cs --entry ImportArt.Run

# Build the pixel font's TextMesh Pro asset (Assets/Fonts/munro/MunroSmall Pixel.asset):
unity command script run --file Tools/PixelArt/ImportFont.cs --entry ImportFont.Run
```

`ImportArt.cs` keeps sprite IDs by name, so you can rebuild the art as often as you like without breaking references.

## Files

- `pixelart.py`: the palette, the grid-of-characters sprite format, outlines, and the sheet packer.
- `tiles.py`: the floor and the cliff. Floor edges are built from four 8x8 quadrants, which gives the 47 blob cases with consistent lighting and no rotated sprites. `Assets/Scripts/Tiles/FloorTile.cs` picks among them at runtime.
- `fx.py`: hit sparks, death puffs, the shadow, and the one-pixel dust mote.
- `idle.py`: the idle frame for each creature. Everything above the creature's waist sinks one pixel. If you redraw a creature, rebuild so its idle frame matches.
- `menus.py`: the menu backdrops, small lit scenes built from the tiles and the original sprites (480x270, shown at 4x). The game over and victory backdrops share one map: their run report covers the middle, so they tell their story in the side rooms and in the one sprite that shows between the two buttons. Also the 9-sliced menu button frame, and the music and sound volume icons (a glyph and three level bars, one sprite per level).
- `hud.py`: the in-game interface. The prompt plaque is the menu button's frame at a third of the size (9-sliced: 3 at the sides and top, 4 at the bottom), with a face 7 high for the 5-high capitals. Its 7x7 icons (a scroll to cast, a chest to open) and arrows sit inside or beside it at one pixel per art pixel. The status panel's hearts, gems and 9x9 icons are drawn for the menus' 4x.
- `fonts.py`: Munro Small as a true pixel font. TextMesh Pro samples fonts at whole point sizes, but Munro's pixel grid is 10.24 points, so its own font assets come out with letters running together. This renders each glyph at exactly 10.24 points into an atlas and writes their metrics, with the em at 16, so a prompt at font size 1 (UGUI) or a popup at font size 10 (world space) is one font pixel per art pixel.
- `build.py`: packs the sheets and writes `manifest.tsv`, with one line per sprite rect, pivot, and 9-slice border, then builds the font atlas.
- `ImportArt.cs`: applies the manifest in Unity.
- `ImportFont.cs`: builds the TextMesh Pro font asset from the atlas and `font.tsv`, updating it in place. It stamps the asset's format version first: TextMesh Pro takes an unversioned font asset for a legacy one and "upgrades" it to nothing.
