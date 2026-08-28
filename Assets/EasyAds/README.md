# Easy Ads

Android 게임에서 AdMob 배너, 전면, 보상형 광고를 한 줄 API로 사용하는 래퍼입니다. 씬에 오브젝트를 놓을 필요 없이 앱 시작 시 자동 초기화하며, 광고를 닫으면 다음 광고를 자동으로 미리 불러옵니다. Unity Editor에서는 실제 광고 대신 안전한 mock 동작을 사용합니다.

## 최초 설정

1. Unity가 패키지 복원을 마칠 때까지 기다립니다. 프로젝트는 공식 Google Mobile Ads 패키지 `11.4.0`을 OpenUPM으로 설치합니다.
2. `Assets > Google Mobile Ads > Settings`에서 테스트 App ID를 실제 AdMob Android App ID로 교체합니다.
3. `Tools > Second Wind > Easy Ads > Create or Select Settings`를 선택하고 실제 Android 광고 단위 ID 3개를 입력합니다.
4. 현재 App ID와 광고 단위 ID는 모두 Google 공식 테스트 값이므로 바로 실행할 수 있습니다. 개발 중에는 이 값을 유지하세요. 실제 광고를 개발/테스트 클릭에 사용하면 안 됩니다.
5. Android Player Settings에서 Minimum API Level 23 이상, Target API Level 35 이상을 사용합니다.

EEA/영국/스위스 등 동의가 필요한 지역에 배포한다면 UMP 동의 절차를 `EasyAds.Initialize`보다 먼저 완료하도록 자동 초기화를 프로젝트 정책에 맞게 조정해야 합니다.

## 사용 예

```csharp
using SecondWind.EasyAds;

// 하단 배너
EasyAds.ShowBanner();
EasyAds.HideBanner();

// 스테이지 종료 시 전면 광고
EasyAds.ShowInterstitial(() => LoadNextStage());

// 사용자가 버튼을 눌렀을 때 보상형 광고
EasyAds.ShowRewarded(
    reward: () => coins += 100,
    completed: earned => Debug.Log($"Reward earned: {earned}"));
```

`IsInterstitialReady`와 `IsRewardedReady`로 버튼 활성 상태를 제어할 수 있습니다. 준비되지 않은 광고를 요청하면 사용자 흐름을 막지 않고 완료 콜백을 즉시 호출한 뒤 다시 로드합니다.
