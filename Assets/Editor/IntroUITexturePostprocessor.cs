using UnityEditor;

/// <summary>
/// Keeps large intro title artwork crisp when Unity reimports it.
/// </summary>
public sealed class IntroUITexturePostprocessor : AssetPostprocessor
{
    private const string IntroVisualsPath = "Assets/Resources/UI/Intro/Visuals/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(IntroVisualsPath, System.StringComparison.Ordinal))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
    }
}
