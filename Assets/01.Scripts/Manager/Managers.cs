using System.Resources;
using UnityEngine;

namespace _01.Scripts.Manager
{
    public class Managers : MonoBehaviour
    {
        private static Managers _sInstance = null;

        private static readonly AdsManager s_adsManager = new AdsManager();
        // private static readonly GameManagerEx s_gameManager = new GameManagerEx();
        // private static readonly IAPManager s_iapManager = new IAPManager();
        private static DataManager s_dataManager = new DataManager();
        private static readonly UIManager s_uiManager = new UIManager();
        private static readonly ResourceManager s_resourceManager = new ResourceManager();
        private static readonly SceneManagerEx s_sceneManager = new SceneManagerEx();
        private static readonly SoundManager s_soundManager = new SoundManager();

        public static AdsManager Ads { get { Init(); return s_adsManager; } }
//        public static GameManagerEx Game { get { Init(); return s_gameManager; } }
        // public static IAPManager IAP { get { Init(); return s_iapManager; } }
        public static DataManager Data { get { Init(); return s_dataManager; } }
        public static UIManager UI { get { Init(); return s_uiManager; } }
        public static ResourceManager Resource { get { Init(); return s_resourceManager; } }
        public static SceneManagerEx Scene { get { Init(); return s_sceneManager; } }
        public static SoundManager Sound {  get { Init(); return s_soundManager; } }

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

        // ReSharper disable Unity.PerformanceAnalysis
        private static void Init()
        {
            if (_sInstance != null) return;
            
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
                go = new GameObject { name = "@Managers" };

            _sInstance = Utils.GetOrAddComponent<Managers>(go);
            DontDestroyOnLoad(go);

            s_adsManager.Init();
            //s_iapManager.Init();
            s_dataManager.Init();
            s_resourceManager.Init();
            s_sceneManager.Init();
            s_soundManager.Init();
                
            Application.targetFrameRate = 60;
        }
    }
}

