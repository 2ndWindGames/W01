using _01.Scripts.Manager;
using SWGUnity2DCore.Data;
using SWGUnity2DCore.Util;
using TMPro;
using UnityEngine;
using ResourceManager = SWGUnity2DCore.Manager.ResourceManager;

namespace SWGUnity2DCore.Manager
{
    public class Managers : MonoBehaviour
    {
        private const string BgmEnabledKey = "VioletTap.Audio.BgmEnabled";
        private const string EffectEnabledKey = "VioletTap.Audio.EffectEnabled";
        private static Managers _sInstance = null;

        private static readonly W01AdsManager s_adsManager = new W01AdsManager(W01ServiceConfiguration.CreateAdsOptions());
        private static readonly IAPManager s_iapManager = new IAPManager(s_adsManager);
        // private static readonly GameManagerEx s_gameManager = new GameManagerEx();
        private static DataManager s_dataManager = new DataManager();
        private static readonly SceneManagerEx s_sceneManager = new SceneManagerEx();
        private static readonly RankManager s_rankManager = new RankManager(W01ServiceConfiguration.LeaderboardId);

        public static W01AdsManager Ads { get { Init(); return s_adsManager; } }
        public static IAPManager IAP { get { Init(); return s_iapManager; } }
//        public static GameManagerEx Game { get { Init(); return s_gameManager; } }
        public static DataManager Data { get { Init(); return s_dataManager; } }
        public static UIManager UI { get { Init(); return CoreServices.UI; } }
        public static ResourceManager Resource { get { Init(); return CoreServices.Resource; } }
        public static SceneManagerEx Scene { get { Init(); return s_sceneManager; } }
        public static SoundManager Sound {  get { Init(); return CoreServices.Sound; } }
        public static RankManager Rank {  get { Init(); return s_rankManager; } }

        public static bool IsBgmEnabled => PlayerPrefs.GetInt(BgmEnabledKey, 1) == 1;
        public static bool IsEffectEnabled => PlayerPrefs.GetInt(EffectEnabledKey, 1) == 1;

        public static void SetBgmEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(BgmEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Sound.SetMuted(Define.Sound.Bgm, !enabled);
        }

        public static void SetEffectEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(EffectEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Sound.SetMuted(Define.Sound.Effect, !enabled);
        }

        public static string GetText(int id)
	    {
            if (!Managers.Data.Texts.TryGetValue(id, out TextData value))
                return "";
     
            // return value.kor.Replace("{userName}", Managers.Game.Name);
            return value.kor.Replace("{userName}", "");
	    }

        private void Start()
        {
            Init();
        }

        private void Update()
        {
            s_adsManager.Tick();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private static void Init()
        {
            if (_sInstance != null) return;
            
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
                go = new GameObject { name = "@Managers" };

            _sInstance = Utils.GetOrAddComponent<Managers>(go);
            DontDestroyOnLoad(go);

            CoreServices.UI.Configure(-20, 100f);
            CoreServices.ButtonClicked = () => CoreServices.Sound.Play(Define.Sound.Effect, "SFX/Button_Click", 0.65f);
            s_adsManager.Init();
            s_iapManager.Init();
            s_dataManager.Init();
            CoreServices.Resource.Init();
            s_sceneManager.Init();
            var sound = CoreServices.Sound;
            sound.SetMuted(Define.Sound.Bgm, !IsBgmEnabled);
            sound.SetMuted(Define.Sound.Effect, !IsEffectEnabled);
            s_rankManager.Init();
                
            Application.targetFrameRate = 60;
        }
    }

    public static class GameLocalization
    {
        private static GameFontSettings s_FontSettings;

        public static bool IsKorean
        {
            get
            {
#if UNITY_EDITOR
                string editorLanguage = UnityEditor.EditorPrefs.GetString("VioletTap.EditorLanguage", "System");
                if (editorLanguage == "Korean") return true;
                if (editorLanguage == "English") return false;
#endif
                return Application.systemLanguage == SystemLanguage.Korean;
            }
        }
        public static string T(string english, string korean) => IsKorean ? korean : english;

        public static void ApplyFont(Component root)
        {
            if (root == null) return;
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                ApplyFont(text);
        }

        public static void ApplyFont(TMP_Text text)
        {
            ApplyFont(text, false);
        }

        private static void ApplyFont(TMP_Text text, bool forceKorean)
        {
            if (text == null) return;
            SWGUnity2DCore.Manager.ApplyFont component = text.GetComponent<SWGUnity2DCore.Manager.ApplyFont>();
            TMP_FontAsset fontAsset = GetFont(
                component == null ? GameFontRole.Body : component.FontCombination, forceKorean);
            if (fontAsset != null) text.font = fontAsset;
        }

        public static void ApplyNicknameFont(TMP_Text text)
        {
            // Nicknames can mix Latin and Hangul regardless of interface language.
            ApplyFont(text, true);
        }

        private static TMP_FontAsset GetFont(GameFontRole role, bool forceKorean)
        {
            GameFontSettings settings = GetFontSettings();
            if (settings == null) return null;

            GameFontPair pair = settings.GetPair(role);
            TMP_FontAsset english = pair.EnglishFont != null ? pair.EnglishFont : settings.BodyEnglishFont;
            TMP_FontAsset korean = pair.KoreanFont != null ? pair.KoreanFont : settings.BodyKoreanFont;
            if (english == null || korean == null)
            {
                Debug.LogError("Assign both English Font and Korean Font in GameFontSettings.");
                return null;
            }

            return forceKorean || IsKorean ? korean : english;
        }

        private static GameFontSettings GetFontSettings()
        {
            if (s_FontSettings != null) return s_FontSettings;
            s_FontSettings = Resources.Load<GameFontSettings>("Fonts/GameFontSettings");
            if (s_FontSettings == null)
                Debug.LogError("GameFontSettings could not be loaded from Resources/Fonts.");
            return s_FontSettings;
        }
    }
}
