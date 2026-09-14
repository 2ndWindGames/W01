using System;
using TMPro;
using UnityEngine;

namespace SWGUnity2DCore.Manager
{
    public enum GameFontRole { Body, H1, H2, H3, H4 }

    [Serializable]
    public struct GameFontPair
    {
        [SerializeField] private TMP_FontAsset englishFont;
        [SerializeField] private TMP_FontAsset koreanFont;

        public TMP_FontAsset EnglishFont => englishFont;
        public TMP_FontAsset KoreanFont => koreanFont;
    }

    [CreateAssetMenu(fileName = "GameFontSettings", menuName = "Violet Tap/Game Font Settings")]
    public sealed class GameFontSettings : ScriptableObject
    {
        [Header("Body (default)")]
        [SerializeField] private TMP_FontAsset englishFont;
        [SerializeField] private TMP_FontAsset koreanFont;
        [Header("Selectable combinations")]
        [SerializeField] private GameFontPair h1;
        [SerializeField] private GameFontPair h2;
        [SerializeField] private GameFontPair h3;
        [SerializeField] private GameFontPair h4;

        public GameFontPair GetPair(GameFontRole role)
        {
            switch (role)
            {
                case GameFontRole.H1: return h1;
                case GameFontRole.H2: return h2;
                case GameFontRole.H3: return h3;
                case GameFontRole.H4: return h4;
                default: return default;
            }
        }

        public TMP_FontAsset BodyEnglishFont => englishFont;
        public TMP_FontAsset BodyKoreanFont => koreanFont;

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += RefreshFonts;
        }

        private static void RefreshFonts()
        {
            foreach (ApplyFont component in UnityEngine.Object.FindObjectsByType<ApplyFont>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                component.Refresh();

            if (!Application.isPlaying) return;
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                GameLocalization.ApplyFont(text);
        }
#endif
    }
}
