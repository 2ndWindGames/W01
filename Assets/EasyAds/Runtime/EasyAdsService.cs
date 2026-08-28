using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace SecondWind.EasyAds
{
    internal sealed class EasyAdsService : MonoBehaviour
    {
        private static EasyAdsService instance;
        private readonly Queue<Action> mainThreadActions = new();
        private readonly List<Action<bool>> initializeCallbacks = new();

        private EasyAdsSettings settings;
        private BannerView banner;
        private InterstitialAd interstitial;
        private RewardedAd rewarded;
        private bool initializing;
        private bool interstitialLoading;
        private bool rewardedLoading;
#if UNITY_EDITOR
        private bool editorBannerVisible;
        private GUIStyle editorBannerStyle;
        private GUIStyle editorBannerLabelStyle;
#endif

        internal static EasyAdsService Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindAnyObjectByType<EasyAdsService>();
                if (instance != null) return instance;
                var host = new GameObject("[Easy Ads]");
                DontDestroyOnLoad(host);
                return instance = host.AddComponent<EasyAdsService>();
            }
        }

        internal bool IsInitialized { get; private set; }
        internal bool IsInterstitialReady
        {
            get
            {
#if UNITY_EDITOR
                return IsInitialized;
#else
                return interstitial != null && interstitial.CanShowAd();
#endif
            }
        }

        internal bool IsRewardedReady
        {
            get
            {
#if UNITY_EDITOR
                return IsInitialized;
#else
                return rewarded != null && rewarded.CanShowAd();
#endif
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            settings = Resources.Load<EasyAdsSettings>("EasyAdsSettings");
            if (settings == null)
                settings = ScriptableObject.CreateInstance<EasyAdsSettings>();
        }

        private void Update()
        {
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0)
                    mainThreadActions.Dequeue()?.Invoke();
            }
        }

        private void OnDestroy()
        {
            banner?.Destroy();
            interstitial?.Destroy();
            rewarded?.Destroy();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!editorBannerVisible) return;

            const float bannerHeight = 60f;
            var bannerRect = new Rect(0f, Screen.height - bannerHeight, Screen.width, bannerHeight);
            var labelRect = new Rect(12f, Screen.height - bannerHeight, Screen.width - 24f, bannerHeight);

            editorBannerStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = Texture2D.whiteTexture }
            };
            editorBannerLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.12f, 0.18f, 0.25f) }
            };

            var previousColor = GUI.color;
            GUI.color = new Color(0.88f, 0.93f, 1f, 1f);
            GUI.Box(bannerRect, GUIContent.none, editorBannerStyle);
            GUI.color = previousColor;
            GUI.Label(labelRect, "EASY ADS · EDITOR TEST BANNER", editorBannerLabelStyle);
        }
#endif

        internal void Initialize(Action<bool> completed)
        {
            if (completed != null) initializeCallbacks.Add(completed);
            if (IsInitialized)
            {
                FlushInitialization(true);
                return;
            }
            if (initializing) return;
            initializing = true;

#if UNITY_EDITOR
            Enqueue(() => FinishInitialization(true));
#else
            MobileAds.Initialize(status => Enqueue(() => FinishInitialization(status != null)));
#endif
        }

        internal void ShowBanner()
        {
            RunWhenInitialized(success =>
            {
                if (!success) return;
#if UNITY_EDITOR
                editorBannerVisible = true;
                Debug.Log("[Easy Ads] Editor mock banner shown.");
#else
                if (banner == null)
                {
                    banner = new BannerView(settings.BannerId, AdSize.Banner, AdPosition.Bottom);
                    banner.OnBannerAdLoadFailed += error => Debug.LogWarning($"[Easy Ads] Banner load failed: {error}");
                    banner.LoadAd(new AdRequest());
                }
                banner.Show();
#endif
            });
        }

        internal void HideBanner()
        {
#if UNITY_EDITOR
            editorBannerVisible = false;
            Debug.Log("[Easy Ads] Editor mock banner hidden.");
#else
            banner?.Hide();
#endif
        }

        internal void ShowInterstitial(Action completed)
        {
            RunWhenInitialized(success =>
            {
                if (!success || !IsInterstitialReady)
                {
                    Debug.LogWarning("[Easy Ads] Interstitial is not ready.");
                    completed?.Invoke();
                    if (success) LoadInterstitial();
                    return;
                }

#if UNITY_EDITOR
                Debug.Log("[Easy Ads] Editor mock interstitial shown.");
                completed?.Invoke();
#else
                var ad = interstitial;
                interstitial = null;
                var finished = false;
                void Finish()
                {
                    if (finished) return;
                    finished = true;
                    Enqueue(() => { completed?.Invoke(); ad.Destroy(); LoadInterstitial(); });
                }
                ad.OnAdFullScreenContentClosed += Finish;
                ad.OnAdFullScreenContentFailed += _ => Finish();
                ad.Show();
#endif
            });
        }

        internal void ShowRewarded(Action reward, Action<bool> completed)
        {
            RunWhenInitialized(success =>
            {
                if (!success || !IsRewardedReady)
                {
                    Debug.LogWarning("[Easy Ads] Rewarded ad is not ready.");
                    completed?.Invoke(false);
                    if (success) LoadRewarded();
                    return;
                }

#if UNITY_EDITOR
                Debug.Log("[Easy Ads] Editor mock rewarded ad shown.");
                reward?.Invoke();
                completed?.Invoke(true);
#else
                var ad = rewarded;
                rewarded = null;
                var earned = false;
                var finished = false;
                void Finish()
                {
                    if (finished) return;
                    finished = true;
                    Enqueue(() => { completed?.Invoke(earned); ad.Destroy(); LoadRewarded(); });
                }
                ad.OnAdFullScreenContentClosed += Finish;
                ad.OnAdFullScreenContentFailed += _ => Finish();
                ad.Show(_ => Enqueue(() => { earned = true; reward?.Invoke(); }));
#endif
            });
        }

        private void FinishInitialization(bool success)
        {
            initializing = false;
            IsInitialized = success;
            if (success)
            {
                if (settings.PreloadInterstitial) LoadInterstitial();
                if (settings.PreloadRewarded) LoadRewarded();
            }
            else Debug.LogError("[Easy Ads] Google Mobile Ads initialization failed.");
            FlushInitialization(success);
        }

        private void RunWhenInitialized(Action<bool> action)
        {
            if (IsInitialized) action(true);
            else Initialize(action);
        }

        private void FlushInitialization(bool success)
        {
            var callbacks = initializeCallbacks.ToArray();
            initializeCallbacks.Clear();
            foreach (var callback in callbacks) callback?.Invoke(success);
        }

        private void LoadInterstitial()
        {
#if UNITY_EDITOR
            return;
#else
            if (interstitialLoading || IsInterstitialReady) return;
            interstitialLoading = true;
            InterstitialAd.Load(settings.InterstitialId, new AdRequest(), (ad, error) => Enqueue(() =>
            {
                interstitialLoading = false;
                interstitial?.Destroy();
                interstitial = ad;
                if (error != null || ad == null) StartCoroutine(Retry(LoadInterstitial));
            }));
#endif
        }

        private void LoadRewarded()
        {
#if UNITY_EDITOR
            return;
#else
            if (rewardedLoading || IsRewardedReady) return;
            rewardedLoading = true;
            RewardedAd.Load(settings.RewardedId, new AdRequest(), (ad, error) => Enqueue(() =>
            {
                rewardedLoading = false;
                rewarded?.Destroy();
                rewarded = ad;
                if (error != null || ad == null) StartCoroutine(Retry(LoadRewarded));
            }));
#endif
        }

        private IEnumerator Retry(Action load)
        {
            yield return new WaitForSecondsRealtime(settings.RetryDelaySeconds);
            load();
        }

        private void Enqueue(Action action)
        {
            lock (mainThreadActions) mainThreadActions.Enqueue(action);
        }
    }
}
