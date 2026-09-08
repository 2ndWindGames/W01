using UnityEditor;
using UnityEngine;

public sealed class VioletTapAudioImporter : AssetPostprocessor
{
    private const string SoundRoot = "Assets/Resources/Sounds/";

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(SoundRoot))
            return;

        AudioImporter importer = (AudioImporter)assetImporter;
        bool isBgm = assetPath.Contains("/BGM/");
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = isBgm ? 0.72f : 0.86f;
        settings.loadType = isBgm
            ? AudioClipLoadType.Streaming
            : AudioClipLoadType.DecompressOnLoad;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = !isBgm;

        importer.defaultSampleSettings = settings;
        importer.forceToMono = false;
        importer.loadInBackground = isBgm;
    }
}
