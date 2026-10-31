// Slices the sheets that build.py lists in manifest.tsv and applies pixel-art import settings, in the open Editor:
//
//   unity command script run --file Tools/PixelArt/ImportArt.cs --entry ImportArt.Run
//
// Sprite IDs are kept by name, so rebuilding the art never breaks references to existing sprites.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEditor.U2D.Sprites;

using UnityEngine;

public static class ImportArt
{
    public static string Run()
    {
        var pixelsPerUnit = 16;
        var sheets = new List<SheetMeta>();
        foreach (var line in File.ReadAllLines("Tools/PixelArt/manifest.tsv"))
        {
            var f = line.Split('\t');
            switch (f[0])
            {
                case "ppu":
                    pixelsPerUnit = int.Parse(f[1]);
                    break;
                case "sheet":
                    sheets.Add(new SheetMeta { path = f[1] });
                    break;
                case "sprite":
                    sheets[sheets.Count - 1].sprites.Add(
                        new SpriteMeta
                            {
                                name = f[1],
                                x = int.Parse(f[2]),
                                y = int.Parse(f[3]),
                                w = int.Parse(f[4]),
                                h = int.Parse(f[5]),
                                pivotX = float.Parse(f[6], System.Globalization.CultureInfo.InvariantCulture),
                                pivotY = float.Parse(f[7], System.Globalization.CultureInfo.InvariantCulture),
                                border = new[] { int.Parse(f[8]), int.Parse(f[9]), int.Parse(f[10]), int.Parse(f[11]) }
                            });
                    break;
            }
        }

        AssetDatabase.Refresh();

        var log = new StringBuilder();
        foreach (var sheet in sheets)
        {
            log.AppendLine(Import(sheet, pixelsPerUnit));
        }

        return log.ToString();
    }

    private static string Import(SheetMeta sheet, int pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(sheet.path) as TextureImporter;
        if (importer == null) throw new Exception("No texture importer for " + sheet.path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var editCapability = dataProvider.GetDataProvider<ISpriteFrameEditCapability>();
        if (editCapability == null) throw new Exception("Sprite editing is not supported for " + sheet.path);

        var capability = editCapability.GetEditCapability();
        if (!capability.HasCapability(EEditCapability.CreateAndDeleteSprite)
            || !capability.HasCapability(EEditCapability.EditBorder)
            || !capability.HasCapability(EEditCapability.EditPivot))
        {
            throw new Exception("The importer cannot create sprites, borders or pivots for " + sheet.path);
        }

        var existing = dataProvider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);

        var rects = new List<SpriteRect>();
        foreach (var s in sheet.sprites)
        {
            GUID id;
            if (!existing.TryGetValue(s.name, out id)) id = GUID.Generate();

            rects.Add(
                new SpriteRect
                    {
                        name = s.name,
                        spriteID = id,
                        rect = new Rect(s.x, s.y, s.w, s.h),
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(s.pivotX, s.pivotY),
                        border = new Vector4(s.border[0], s.border[1], s.border[2], s.border[3])
                    });
        }

        dataProvider.SetSpriteRects(rects.ToArray());

        var nameFileIds = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIds != null)
        {
            nameFileIds.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        }

        dataProvider.Apply();
        importer.SaveAndReimport();

        var kept = rects.Count(r => existing.ContainsKey(r.name));
        return string.Format("{0}: {1} sprites ({2} kept, {3} new)", sheet.path, rects.Count, kept, rects.Count - kept);
    }

    private class SheetMeta
    {
        public string path;

        public List<SpriteMeta> sprites = new List<SpriteMeta>();
    }

    private class SpriteMeta
    {
        public string name;

        public int x;

        public int y;

        public int w;

        public int h;

        public float pivotX;

        public float pivotY;

        public int[] border;
    }
}
