namespace SWGUnity2DCore.Manager
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
            ShowInterstitialAds();
        }

        public void SetAdsDisabled(bool disabled)
        {
            AdsDisabled = disabled;
            if (disabled)
                HideBanner();
            else if (m_InGame)
                ShowBanner();
        }

        public new void ShowInterstitialAds()
        {
            if (!AdsDisabled && m_InGame && m_Schedule.CanShow(UnityEngine.Time.realtimeSinceStartupAsDouble))
                base.ShowInterstitialAds();
        }
    }
}
