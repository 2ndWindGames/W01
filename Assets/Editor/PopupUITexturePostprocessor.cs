using UnityEditor;
using UnityEngine;

public sealed class PopupUITexturePostprocessor : AssetPostprocessor
{
    private const string PopupPath = "Assets/Resources/UI/Popup/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PopupPath, System.StringComparison.Ordinal))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        if (assetPath.Contains("popup_button_"))
            importer.spriteBorder = new Vector4(36f, 24f, 36f, 24f);
        else if (assetPath.EndsWith("popup_panel.png", System.StringComparison.Ordinal))
            importer.spriteBorder = new Vector4(150f, 150f, 150f, 150f);
    }
}
