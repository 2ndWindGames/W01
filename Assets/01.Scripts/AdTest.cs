using UnityEngine;
using SecondWind.EasyAds;

public class AdTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 하단 배너
        EasyAds.ShowBanner();
        // EasyAds.HideBanner();

        // 스테이지 종료 시 전면 광고
        // EasyAds.ShowInterstitial(() => LoadNextStage());

        // 사용자가 버튼을 눌렀을 때 보상형 광고
        // EasyAds.ShowRewarded(
        //     reward: () => coins += 100,
        //     completed: earned => Debug.Log($"Reward earned: {earned}"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
