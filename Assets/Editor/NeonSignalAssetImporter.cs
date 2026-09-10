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

        if (assetPath.Contains("/NineSlice/"))
            importer.spriteBorder = GetBorder(assetPath);
    }

    private static Vector4 GetBorder(string path)
    {
        if (path.Contains("hud_card")) return new Vector4(40, 28, 40, 28);
        if (path.Contains("header_strip")) return new Vector4(64, 24, 64, 24);
        if (path.Contains("list_row")) return new Vector4(48, 28, 48, 28);
        if (path.Contains("icon_button")) return new Vector4(36, 36, 36, 36);
        if (path.Contains("button")) return new Vector4(52, 36, 52, 36);
        return new Vector4(64, 64, 64, 64);
    }
}
#endif
