// Applies import settings to the generated sounds. Run it in the open Editor after building them:
//   unity command script run --file Tools/Audio/ImportAudio.cs --entry ImportAudio.Run
// The loops stream from disk as Vorbis. The short effects stay uncompressed in memory, so they play the instant
// they are triggered.

using System.Text;

using UnityEditor;

using UnityEngine;

public static class ImportAudio
{
    private static readonly string[] Loops = { "TitleTheme", "DungeonTheme", "DungeonAmbience", "ScrawlTheme" };

    private static readonly string[] Effects =
        {
            "Key", "Word", "Typo", "Death", "Hover", "Select", "Step1", "Step2", "Step3", "BossRumble", "BossCharge",
            "BossSlam", "BossBeam", "BossHit", "BossSink", "BossDeath"
        };

    public static string Run()
    {
        AssetDatabase.Refresh();
        var log = new StringBuilder();
        foreach (var name in Loops) Apply(name, AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, 0.6f, log);
        foreach (var name in Effects) Apply(name, AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, 1f, log);
        return log.ToString();
    }

    private static void Apply(string name, AudioClipLoadType loadType, AudioCompressionFormat format, float quality, StringBuilder log)
    {
        var path = "Assets/Audio/" + name + ".wav";
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
        {
            log.AppendLine("missing " + path);
            return;
        }

        var settings = importer.defaultSampleSettings;
        settings.loadType = loadType;
        settings.compressionFormat = format;
        settings.quality = quality;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = loadType == AudioClipLoadType.Streaming;
        importer.SaveAndReimport();

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        log.AppendLine(string.Format("{0}: {1}, {2}, {3:0.00}s", name, loadType, format, clip.length));
    }
}
