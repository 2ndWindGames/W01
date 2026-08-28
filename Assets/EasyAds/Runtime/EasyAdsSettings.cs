using UnityEngine;

namespace SecondWind.EasyAds
{
    [CreateAssetMenu(fileName = "EasyAdsSettings", menuName = "Second Wind/Easy Ads Settings")]
    public sealed class EasyAdsSettings : ScriptableObject
    {
        [Header("Android AdMob ad unit IDs")]
        [SerializeField] private string bannerId = "ca-app-pub-3940256099942544/6300978111";
        [SerializeField] private string interstitialId = "ca-app-pub-3940256099942544/1033173712";
        [SerializeField] private string rewardedId = "ca-app-pub-3940256099942544/5224354917";

        [Header("Loading")]
        [SerializeField, Min(0f)] private float retryDelaySeconds = 5f;
        [SerializeField] private bool preloadInterstitial = true;
        [SerializeField] private bool preloadRewarded = true;

        public string BannerId => bannerId;
        public string InterstitialId => interstitialId;
        public string RewardedId => rewardedId;
        public float RetryDelaySeconds => retryDelaySeconds;
        public bool PreloadInterstitial => preloadInterstitial;
        public bool PreloadRewarded => preloadRewarded;
    }
}
