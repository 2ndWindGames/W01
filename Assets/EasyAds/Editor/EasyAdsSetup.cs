#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecondWind.EasyAds.Editor
{
    internal static class EasyAdsSetup
    {
        private const string ResourcesFolder = "Assets/EasyAds/Resources";
        private const string SettingsPath = ResourcesFolder + "/EasyAdsSettings.asset";

        [MenuItem("Tools/Second Wind/Easy Ads/Create or Select Settings")]
        private static void CreateOrSelectSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<EasyAdsSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(ResourcesFolder);
                settings = ScriptableObject.CreateInstance<EasyAdsSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
#endif
