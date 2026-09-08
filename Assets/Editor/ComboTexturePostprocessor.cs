using UnityEditor;
using UnityEngine;

public sealed class ComboTexturePostprocessor : AssetPostprocessor
{
    private const string ComboPath = "Assets/Resources/Combo/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ComboPath, System.StringComparison.Ordinal))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
