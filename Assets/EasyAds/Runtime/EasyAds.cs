using System;
using UnityEngine;

namespace SecondWind.EasyAds
{
    /// <summary>Small, scene-independent facade for banner, interstitial and rewarded ads.</summary>
    public static class EasyAds
    {
        public static bool IsInitialized => EasyAdsService.Instance.IsInitialized;
        public static bool IsInterstitialReady => EasyAdsService.Instance.IsInterstitialReady;
        public static bool IsRewardedReady => EasyAdsService.Instance.IsRewardedReady;

        public static void Initialize(Action<bool> completed = null) =>
            EasyAdsService.Instance.Initialize(completed);

        public static void ShowBanner() => EasyAdsService.Instance.ShowBanner();
        public static void HideBanner() => EasyAdsService.Instance.HideBanner();

        public static void ShowInterstitial(Action completed = null) =>
            EasyAdsService.Instance.ShowInterstitial(completed);

        public static void ShowRewarded(Action reward, Action<bool> completed = null) =>
            EasyAdsService.Instance.ShowRewarded(reward, completed);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize() => Initialize();
    }
}
