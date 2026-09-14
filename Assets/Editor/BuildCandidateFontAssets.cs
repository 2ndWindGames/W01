using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class BuildCandidateFontAssets
{
    private const string Folder = "Assets/Resources/Fonts/Candidates";

    private static readonly (string directory, string file)[] Fonts =
    {
        ("oxanium", "Oxanium-Regular.ttf"),
        ("rajdhani", "Rajdhani-Regular.ttf"),
        ("exo2", "Exo2-Regular.ttf"),
        ("chakrapetch", "ChakraPetch-Regular.ttf"),
        ("orbitron", "Orbitron-Regular.ttf"),
        ("ibmplexsanskr", "IBMPlexSansKR-Regular.ttf"),
        ("gothica1", "GothicA1-Regular.ttf"),
        ("dohyeon", "DoHyeon-Regular.ttf"),
        ("blackhansans", "BlackHanSans-Regular.ttf"),
    };

    static BuildCandidateFontAssets()
    {
        EditorApplication.delayCall += CreateMissing;
    }

    [MenuItem("Tools/Fonts/Create Candidate TMP Assets")]
    public static void CreateMissing()
    {
        foreach (var (directory, file) in Fonts)
        {
            string path = $"{Folder}/{directory}/{file}";
            string assetPath = path.Substring(0, path.Length - 4) + " SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && existing.faceInfo.styleName == "Regular") continue;
            if (existing != null) AssetDatabase.DeleteAsset(assetPath);

            Font source = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (source == null)
            {
                Debug.LogError($"Candidate font source missing: {path}");
                continue;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source);
            if (asset == null)
            {
                Debug.LogError($"Could not create TMP font asset: {path}");
                continue;
            }

            asset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            foreach (Texture2D texture in asset.atlasTextures)
                if (texture != null) AssetDatabase.AddObjectToAsset(texture, asset);
            if (asset.material != null) AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            Debug.Log($"Created candidate TMP font asset: {assetPath}");
        }

        AssetDatabase.SaveAssets();
    }
}
