# Android 15/16 edge-to-edge 점검

## 상태

프로젝트의 Android 시작 테마를 보강했습니다. **Play Console의 SDK 내부 `SHORT_EDGES` 경고가 해결된 상태는 아닙니다.** 새 AAB 빌드·업로드와 Android 실기기 검증은 이번 작업에서 수행하지 않았습니다.

사용자 화면의 경고 대상은 **10 (3.7)**입니다. 로컬에서 조사한 AAB는 **12 (3.7.2)**이며 두 아티팩트를 동일한 빌드로 취급하지 않습니다. 기존 `violettap.aab`는 이번 변경을 포함하지 않으며 덮어쓰지 않았습니다. 버전, 광고·결제 설정, SDK 버전도 변경하지 않았습니다.

## 확인한 원인

- 경고 파라미터: `LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES`.
- 경고 위치: `m3.j.m`.
- 로컬 3.7.2의 R8 mapping에도 같은 이름이 있습니다. `GoogleSignatureVerifier$$ExternalSyntheticApiModelOutline2`라는 합성 클래스에 모인 API 접근 코드입니다. 클래스 이름만으로 Google 인증 기능을 원인이라고 판단하면 안 됩니다.
- 로컬 AAB의 DEX에서 `m3.j.m(WindowManager.LayoutParams)` 호출자는 다음 두 곳으로 확인했습니다.
  - `com.google.android.gms.ads.internal.overlay.zzm.zzj`: `SHORT_EDGES` 사용은 API 28~34로 제한돼 있습니다.
  - `com.google.android.play.core.hsdp.service.HsdpShimActivity.onCreate`: API 28 이상에서 `SHORT_EDGES`를 설정하고, API 35를 제외하는 상한 검사가 없습니다.
- `HsdpShimActivity`는 `play-services-ads-api:25.4.0`이 가져오는 `com.google.android.play:hsdp:2.0.1` 소속입니다. 게임의 C# 스크립트 또는 결제 매니저에서 직접 작성한 코드가 아닙니다.
- Unity 6000.3.23f1의 `DisplayCutoutSupport.setLayoutCutoutMode`는 API 35 이상에서 이미 `ALWAYS`(3)를 설정합니다. 낮은 API용 호환 분기는 남아 있습니다.

이는 **로컬 3.7.2**에서 확인한 내용입니다. 업로드된 3.7의 호출 관계를 완전히 확정하려면 그 아티팩트의 AAB/mapping이 필요합니다.

## 적용한 변경

- `Assets/Editor/AndroidEdgeToEdgeProject.cs`: Gradle 생성 후 게임 Activity만 `VioletTapGameActivityTheme`으로 연결합니다.
- Android 15 이상 전용 `values-v35` 테마에 `android:windowLayoutInDisplayCutoutMode=always`를 명시합니다. Android 16도 해당 리소스를 사용합니다.
- 기본 테마는 Unity의 `BaseUnityGameActivityTheme`을 상속합니다. Unity의 Android 12+ 스플래시 리소스와 Android 14 이하 동작을 유지합니다.
- `Assets/Editor/AndroidEdgeToEdgeBuild.cs`: Render Outside Safe Area가 꺼졌거나, GameActivity/테마 구조가 예상과 다르면 조용히 덮어쓰지 않고 빌드 오류로 알립니다.
- 기존 Intro/Game의 `ResponsiveGameViewport`와 4종 팝업의 `ViewportCanvasScaler` 연결을 확인했습니다. 안전 영역·배너 여백 계산은 유지했습니다.
- 광고 Activity, SDK AAR/JAR, Unity 설치 파일, 패키지 이름, 권한, 딥링크는 수정하지 않았습니다. `windowOptOutEdgeToEdgeEnforcement` 우회도 추가하지 않았습니다.

게임 시작 테마를 수정해도 **광고 SDK DEX 안에 있는 `SHORT_EDGES` 호출 자체는 제거되지 않습니다.** 따라서 이 변경만으로 두 번째 경고가 사라진다고 보장할 수 없습니다. SDK를 임의로 삭제하거나 바이너리를 패치하면 광고 표시·설치 흐름을 손상할 수 있으므로 수행하지 않았습니다.

## 검증 결과

- Unity의 실제 컴파일 응답 파일로 전체 Editor 어셈블리 C# 컴파일 성공(새 파일 포함).
- `Tools/Test-AndroidEdgeToEdge.ps1`: **16개 검사 통과**.
  - 게임 테마 선택, 기존 권한/메타데이터/딥링크/SDK Activity 유지.
  - OS별 테마, 스플래시 부모, 반복 실행 결과 동일, 옵트아웃 미추가.
  - 예상하지 못한 테마, 비활성 안전 영역, GameActivity 누락 시 오류.
  - 실제 Gradle 원본은 테스트에서 변경하지 않음.
  - 실제 `ResponsiveGameViewport.CalculatePixelRect` 코드로 세로/가로/태블릿/좁은 화면 및 노치·시스템 바·배너 여백 5종 검사.
- Android SDK 36의 `aapt2 compile`로 생성한 테마 XML 리소스 컴파일 성공.
- 실기기 화면, 광고 열기/닫기, Android 15/16 시스템 바 제스처, 최종 AAB 링크/서명/Play Console 재검사는 **미수행**입니다. 수학/리소스 검증은 실기기 QA를 대체하지 않습니다.

## 다음 배포 확인

1. Unity에서 다시 Android AAB를 빌드합니다. 이미 스토어에 등록된 코드라면 미사용 버전 코드가 필요합니다.
2. 최종 병합 매니페스트에서 게임 Activity의 `@style/VioletTapGameActivityTheme`과 `unity.render-outside-safearea=true`를 확인합니다.
3. Android 15/16에서 제스처·3버튼 탐색, 노치, 가로 전환, 멀티윈도우를 확인합니다. 인트로/도움말/사운드/랭킹/닉네임 입력/게임/결과 및 광고 복귀 화면을 포함합니다.
4. 신규 아티팩트에 대해 Play Console을 재검사합니다. 과거 10 (3.7)의 경고는 새 코드만 수정한다고 갱신되지 않습니다.
5. `SHORT_EDGES` 경고가 남으면 신규 AAB와 해당 mapping으로 호출자를 다시 확인합니다. Google Ads/HSDP 수정 릴리스 또는 공식 지원 답변을 확인한 후 SDK 교체를 검토하고 광고 회귀 테스트를 수행합니다.

## 공식 참고

- [Android 15 변경 사항: edge-to-edge와 cutout 모드](https://developer.android.com/about/versions/15/behavior-changes-15#edge-to-edge)
- [Unity 게임의 큰 화면 및 안전 영역 처리](https://developer.android.com/games/engines/unity/unity-large-screen)
- [Unity 6.3 Screen.safeArea](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-safeArea.html)
- [Google Mobile Ads Android 릴리스 노트](https://developers.google.com/admob/android/rel-notes)
- [Google Mobile Ads Unity 플러그인 릴리스](https://github.com/googleads/googleads-mobile-unity/releases)
