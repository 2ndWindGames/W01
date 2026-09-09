namespace SWGUnity2DCore.Manager
{
    // These IDs belong to W01. W04 must provide its own configuration.
    internal static class W01ServiceConfiguration
    {
        public const string LeaderboardId = "violettap_ranking_01";

        public static AdMobOptions CreateAdsOptions() => new AdMobOptions
        {
            AndroidBannerId = "ca-app-pub-3765914942296716/2410098921",
            AndroidInterstitialId = "ca-app-pub-3765914942296716/4041676531",
            ForceTestAds = false
        };
    }
}
