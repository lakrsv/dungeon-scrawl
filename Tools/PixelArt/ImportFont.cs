// Builds the pixel font's TextMesh Pro asset from the atlas and metrics fonts.py writes. Run it in the open Editor:
//   unity command script run --file Tools/PixelArt/ImportFont.cs --entry ImportFont.Run
// Running it again updates the asset in place, so text that uses it keeps its reference.

using System.Collections.Generic;
using System.Globalization;
using System.IO;

using TMPro;

using UnityEditor;

using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

public static class ImportFont
{
    private const string AtlasPath = "Assets/Fonts/munro/MunroSmallPixel.png";

    private const string AssetPath = "Assets/Fonts/munro/MunroSmall Pixel.asset";

    public static string Run()
    {
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        var face = new Dictionary<string, float>();
        var glyphs = new List<int[]>();
        foreach (var line in File.ReadAllLines("Tools/PixelArt/font.tsv"))
        {
            var f = line.Split('\t');
            if (f[0] == "face")
            {
                for (var i = 1; i < f.Length; i++)
                {
                    var kv = f[i].Split('=');
                    face[kv[0]] = float.Parse(kv[1], CultureInfo.InvariantCulture);
                }
            }
            else if (f[0] == "glyph")
            {
                var values = new int[8];
                for (var i = 0; i < 8; i++) values[i] = int.Parse(f[i + 1], CultureInfo.InvariantCulture);
                glyphs.Add(values);
            }
        }

        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        var created = asset == null;
        if (created)
        {
            asset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            asset.name = "MunroSmall Pixel";
        }

        // Stamp the current format first. A font asset without a version is taken for a legacy one, and the first
        // read "upgrades" it from the (empty) legacy fields, wiping everything set below.
        var version = new SerializedObject(asset);
        version.FindProperty("m_Version").stringValue = "1.1.0";
        version.ApplyModifiedPropertiesWithoutUndo();

        asset.faceInfo = new FaceInfo
        {
            familyName = "Munro Small",
            styleName = "Pixel",
            pointSize = face["pointSize"],
            scale = 1f,
            lineHeight = face["lineHeight"],
            ascentLine = face["ascentLine"],
            capLine = face["capLine"],
            meanLine = face["meanLine"],
            baseline = face["baseline"],
            descentLine = face["descentLine"],
            underlineOffset = face["underlineOffset"],
            underlineThickness = face["underlineThickness"],
            strikethroughOffset = face["strikethroughOffset"],
            strikethroughThickness = 1f,
            superscriptOffset = face["capLine"],
            superscriptSize = 0.5f,
            subscriptOffset = face["descentLine"],
            subscriptSize = 0.5f,
            tabWidth = face["tabWidth"],
        };

        asset.glyphTable.Clear();
        asset.characterTable.Clear();
        foreach (var g in glyphs)
        {
            // unicode, x, y, w, h, bearing x, bearing y, advance
            var glyph = new Glyph((uint)g[0], new GlyphMetrics(g[3], g[4], g[5], g[6], g[7]), new GlyphRect(g[1], g[2], g[3], g[4]), 1f, 0);
            asset.glyphTable.Add(glyph);
            asset.characterTable.Add(new TMP_Character((uint)g[0], asset, glyph));
        }

        var material = asset.material;
        if (material == null)
        {
            material = new Material(Shader.Find("TextMeshPro/Bitmap")) { name = "MunroSmall Pixel Material" };
            asset.material = material;
        }

        material.SetTexture("_MainTex", atlas);

        if (created)
        {
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.AddObjectToAsset(material, asset);
        }

        var so = new SerializedObject(asset);
        so.FindProperty("m_AtlasWidth").intValue = atlas.width;
        so.FindProperty("m_AtlasHeight").intValue = atlas.height;
        so.FindProperty("m_AtlasPadding").intValue = 0;
        so.FindProperty("m_AtlasRenderMode").intValue = (int)GlyphRenderMode.RASTER;
        so.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Static;
        var textures = so.FindProperty("m_AtlasTextures");
        textures.arraySize = 1;
        textures.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
        so.ApplyModifiedPropertiesWithoutUndo();

        asset.ReadFontAssetDefinition();
        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return string.Format("{0} {1}: {2} glyphs, atlas {3}x{4}", created ? "created" : "updated", AssetPath, glyphs.Count, atlas.width, atlas.height);
    }
}
