using UnityEditor;
using UnityEngine;

public sealed class VioletTapAudioImporter : AssetPostprocessor
{
    private const string SoundRoot = "Assets/Resources/Sounds/";

    // Reimport existing clips when the audio policy changes, including cached Vorbis effects.
    public override uint GetVersion() => 1;

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(SoundRoot))
            return;

        AudioImporter importer = (AudioImporter)assetImporter;
        bool isBgm = assetPath.Contains("/BGM/");
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.compressionFormat = isBgm ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
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
