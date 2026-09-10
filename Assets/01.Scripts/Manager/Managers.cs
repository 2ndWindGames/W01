using SWGUnity2DCore.Data;
using SWGUnity2DCore.Util;
using UnityEngine;
using ResourceManager = SWGUnity2DCore.Manager.ResourceManager;

namespace SWGUnity2DCore.Manager
{
    public class Managers : MonoBehaviour
    {
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
            _ = CoreServices.Sound;
            s_rankManager.Init();
                
            Application.targetFrameRate = 60;
        }
    }
}
