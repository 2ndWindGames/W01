#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class NeonSignalAssetImporter : AssetPostprocessor
{
    private const string Root = "Assets/Resources/UI/NeonSignalPack/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root) || assetPath.Contains("/Concepts/") || assetPath.Contains("/Sources/"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

    }
}
#endif
