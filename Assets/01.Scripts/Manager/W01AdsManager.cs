using SWGUnity2DCore.Manager;

namespace _01.Scripts.Manager
{
    /// <summary>VioletTap's placement policy. SDK mechanics live in the AdMob package.</summary>
    public sealed class W01AdsManager : AdsManager
    {
        private readonly InterstitialSchedule m_Schedule = new InterstitialSchedule();
        private bool m_InGame;
        public bool AdsDisabled { get; private set; }

        public W01AdsManager(AdMobOptions options) : base(options)
        {
            InterstitialOpened += () => m_Schedule.MarkShown(UnityEngine.Time.realtimeSinceStartupAsDouble);
        }

        public void EnterGameScene()
        {
            m_InGame = true;
            if (!AdsDisabled) ShowBanner();
        }

        public void ExitGameScene()
        {
            m_InGame = false;
            HideBanner();
        }

        public void RecordCompletedRound()
        {
            if (AdsDisabled) return;
            m_Schedule.RecordRound();
        }

        public void SetAdsDisabled(bool disabled)
        {
            AdsDisabled = disabled;
            if (disabled)
                HideBanner();
            else if (m_InGame)
                ShowBanner();
        }

        public bool TryShowInterstitialBeforeRetry()
        {
            if (AdsDisabled || !m_InGame
                || !m_Schedule.CanShow(UnityEngine.Time.realtimeSinceStartupAsDouble)) return false;

            try
            {
                base.ShowInterstitialAds();
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogWarning("전면 광고를 시작하지 못해 바로 재시작합니다: " + exception.Message);
                return false;
            }
            return IsShowingInterstitial;
        }
    }
}
