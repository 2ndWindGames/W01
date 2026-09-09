using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace _01.Scripts.Manager
{
    public class AdsManager
    {
        public const string AndroidBannerId = "ca-app-pub-3765914942296716/2410098921";
        public const string AndroidInterstitialId = "ca-app-pub-3765914942296716/4041676531";
        private readonly InterstitialSchedule m_Schedule = new InterstitialSchedule();
        private InterstitialAd m_Interstitial;
        private BannerView m_Banner;
        private bool m_Started, m_Initialized, m_Loading, m_InGame, m_AudioPaused;
        private double m_RetryAt, m_LoadedAt, m_BannerRetryAt = double.PositiveInfinity;
        public bool IsShowingInterstitial { get; private set; }
        public float BannerHeightPixels { get; private set; }
        public event Action<float> BannerHeightChanged;
        private static bool UseTestAds => Application.isEditor || Debug.isDebugBuild;
        private static double Now => Time.realtimeSinceStartupAsDouble;
        private static void OnMain(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        public void Init()
        {
            if (m_Started) return;
            m_Started = true;
#if UNITY_ANDROID || UNITY_EDITOR
            // UMP callbacks use OnMain before MobileAds.Initialize is reached.
            // ExecuteInUpdate only enqueues work; its executor must exist first.
            MobileAdsEventExecutor.Initialize();
            Debug.Log("[Ads] 동의 확인 시작 (" + (UseTestAds ? "테스트 광고" : "실제 광고") + ")");
            ConsentInformation.Update(new ConsentRequestParameters(), error => OnMain(() =>
            {
                Debug.Log("[Ads] 동의 확인 응답: " + ConsentInformation.ConsentStatus);
                if (error != null)
                {
                    Debug.LogWarning("광고 동의 상태 확인 실패: " + error.Message);
                    m_Started = false;
                    m_RetryAt = Now + 60;
                    InitializeSdkIfAllowed();
                    return;
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMain(() =>
                {
                    if (formError != null) Debug.LogWarning(formError.Message);
                    InitializeSdkIfAllowed();
                }));
            }));
#endif
        }

        private void InitializeSdkIfAllowed()
        {
            if (m_Initialized) return;
            if (!ConsentInformation.CanRequestAds())
            {
                Debug.LogWarning("[Ads] 동의 확인이 완료되지 않아 광고 요청을 대기합니다.");
                return;
            }
            Debug.Log("[Ads] SDK 초기화 시작");
            MobileAds.Initialize(status => OnMain(() =>
            {
                if (status == null)
                {
                    Debug.LogWarning("[Ads] SDK 초기화 응답이 비어 있습니다.");
                    return;
                }
                m_Initialized = true;
                Debug.Log("[Ads] SDK 초기화 완료");
                PrepareAds();
                if (m_InGame) CreateBanner();
            }));
        }

        // Preload only: ads are displayed exclusively at round completion.
        public void Tick()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            if (!m_Started && Now >= m_RetryAt) Init();
#endif
            if (!m_Initialized || IsShowingInterstitial) return;
            if (m_InGame && m_Banner != null && Now >= m_BannerRetryAt)
            {
                m_BannerRetryAt = double.PositiveInfinity;
                m_Banner.LoadAd(new AdRequest());
            }
            if (m_Interstitial != null && Now - m_LoadedAt > 3300)
            {
                m_Interstitial.Destroy();
                m_Interstitial = null;
            }
            if (m_Interstitial == null && !m_Loading && Now >= m_RetryAt) PrepareAds();
        }

        public void PrepareAds()
        {
            if (!m_Initialized || m_Loading || m_Interstitial != null) return;
            m_Loading = true;
            string id = UseTestAds ? "ca-app-pub-3940256099942544/1033173712" : AndroidInterstitialId;
            InterstitialAd.Load(id, new AdRequest(), (ad, error) => OnMain(() =>
            {
                m_Loading = false;
                if (error != null || ad == null)
                {
                    ad?.Destroy();
                    m_RetryAt = Now + 30;
                    Debug.LogWarning("전면 광고 로드 실패: " + error);
                    return;
                }
                m_Interstitial = ad;
                m_LoadedAt = Now;
                Debug.Log("[Ads] 전면 광고 로드 완료");
                ad.OnAdFullScreenContentOpened += () => OnMain(() =>
                {
                    m_Schedule.MarkShown(Now);
                    AudioListener.pause = true;
                });
                ad.OnAdFullScreenContentClosed += () => OnMain(() => FinishInterstitial(ad));
                ad.OnAdFullScreenContentFailed += error2 => OnMain(() =>
                {
                    Debug.LogWarning("전면 광고 표시 실패: " + error2);
                    FinishInterstitial(ad);
                });
            }));
        }

        public void RecordCompletedRound()
        {
            m_Schedule.RecordRound();
            if (m_InGame && m_Schedule.CanShow(Now)) ShowInterstitialAds();
        }

        public void ShowInterstitialAds()
        {
            if (!m_InGame || IsShowingInterstitial || !m_Schedule.CanShow(Now)) return;
            if (m_Interstitial == null || !m_Interstitial.CanShowAd())
            {
                m_Interstitial?.Destroy();
                m_Interstitial = null;
                PrepareAds();
                return;
            }
            IsShowingInterstitial = true;
            m_AudioPaused = AudioListener.pause;
            m_Banner?.Hide();
            try { m_Interstitial.Show(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                FinishInterstitial(m_Interstitial);
            }
        }

        private void FinishInterstitial(InterstitialAd ad)
        {
            if (m_Interstitial != ad) return;
            AudioListener.pause = m_AudioPaused;
            IsShowingInterstitial = false;
            ad?.Destroy();
            m_Interstitial = null;
            if (m_InGame) m_Banner?.Show();
            m_RetryAt = Now + 1;
        }

        public void EnterGameScene()
        {
            m_InGame = true;
            Debug.Log("[Ads] 게임씬 진입, SDK 준비=" + m_Initialized);
            if (m_Initialized) CreateBanner();
        }

        public void ExitGameScene()
        {
            m_InGame = false;
            m_Banner?.Destroy();
            m_Banner = null;
            m_BannerRetryAt = double.PositiveInfinity;
            SetBannerHeight(0);
        }

        private void CreateBanner()
        {
            if (m_Banner != null || !m_InGame) return;
            SetBannerHeight(50 * MobileAds.Utils.GetDeviceScale() + Screen.safeArea.yMin);
            string id = UseTestAds ? "ca-app-pub-3940256099942544/6300978111" : AndroidBannerId;
            var banner = new BannerView(id, AdSize.Banner, AdPosition.Bottom);
            m_Banner = banner;
            banner.OnBannerAdLoaded += () => OnMain(() =>
            {
                if (m_Banner != banner) return;
                m_BannerRetryAt = double.PositiveInfinity;
                SetBannerHeight(banner.GetHeightInPixels() + Screen.safeArea.yMin);
                Debug.Log("[Ads] 배너 광고 로드 완료");
                if (!m_InGame || IsShowingInterstitial) banner.Hide();
            });
            banner.OnBannerAdLoadFailed += error => OnMain(() =>
            {
                if (m_Banner != banner) return;
                m_BannerRetryAt = Now + 60;
                Debug.LogWarning("배너 광고 로드 실패: " + error);
            });
            banner.LoadAd(new AdRequest());
        }

        private void SetBannerHeight(float pixels)
        {
            BannerHeightPixels = pixels;
            BannerHeightChanged?.Invoke(pixels);
        }

        public bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus ==
            PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptions()
        {
            ConsentForm.ShowPrivacyOptionsForm(error => OnMain(() =>
            {
                if (error != null) Debug.LogWarning(error.Message);
            }));
        }
    }
}

