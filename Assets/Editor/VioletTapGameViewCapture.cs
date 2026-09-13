#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class VioletTapGameViewCapture
{
    [MenuItem("Tools/VioletTap/Editor Language/System")]
    private static void UseSystemLanguage() => SetEditorLanguage("System");

    [MenuItem("Tools/VioletTap/Editor Language/Korean")]
    private static void UseKoreanLanguage() => SetEditorLanguage("Korean");

    [MenuItem("Tools/VioletTap/Editor Language/English")]
    private static void UseEnglishLanguage() => SetEditorLanguage("English");

    private static void SetEditorLanguage(string language)
    {
        EditorPrefs.SetString("VioletTap.EditorLanguage", language);
        Debug.Log("[VioletTap] Editor language: " + language + ". Restart Play mode to refresh UI text.");
    }

    [MenuItem("Tools/VioletTap/Capture Current Game View")]
    public static void CaptureCurrentGameView()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[VioletTap] Start Play mode before capturing the Game View.");
            return;
        }

        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../StoreListing/raw"));
        Directory.CreateDirectory(directory);
        string fileName = "game_view_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, fileName), 1);
    }
}
#endif
