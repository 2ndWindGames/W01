namespace SWGUnity2DCore.Manager
{
    /// <summary>VioletTap's placement policy. SDK mechanics live in the AdMob package.</summary>
    public sealed class W01AdsManager : AdsManager
    {
        private readonly InterstitialSchedule m_Schedule = new InterstitialSchedule();
        private bool m_InGame;

        public W01AdsManager(AdMobOptions options) : base(options)
        {
            InterstitialOpened += () => m_Schedule.MarkShown(UnityEngine.Time.realtimeSinceStartupAsDouble);
        }

        public void EnterGameScene()
        {
            m_InGame = true;
            ShowBanner();
        }

        public void ExitGameScene()
        {
            m_InGame = false;
            HideBanner();
        }

        public void RecordCompletedRound()
        {
            m_Schedule.RecordRound();
            ShowInterstitialAds();
        }

        public new void ShowInterstitialAds()
        {
            if (m_InGame && m_Schedule.CanShow(UnityEngine.Time.realtimeSinceStartupAsDouble))
                base.ShowInterstitialAds();
        }
    }
}
