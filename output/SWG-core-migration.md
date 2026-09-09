# W01 공통 모듈 분리

서브모듈 경로: `LocalPackages/SWGUnity2DCore`

## 게임에 남긴 코드

- `Assets/01.Scripts/Manager/Managers.cs`: W01 서비스 초기화와 생명주기
- `W01ServiceConfiguration.cs`: W01 광고 단위 ID, 리더보드 ID
- `W01AdsManager.cs`, `InterstitialSchedule.cs`: 게임 화면 및 3판/180초 광고 정책
- `SceneManager.cs`, `BaseScene.cs`, `W01SceneType.cs`: W01 씬 전환 정책
- `Game/GameConfig.cs`: 탭 게임의 라운드/피버/타깃 설정
- `Data/`, `DataManager.cs`, `IAPManager.cs`: 기존 게임 데이터 및 비활성 IAP 코드

## 서브모듈 패키지

- Core: UI/리소스/사운드/풀/유틸리티
- AdMob: 동의, 로딩, 배너/전면 표시, 재시도. 게임 주기 없음.
- UGS: 인증/랭킹. 리더보드 ID는 생성자로 전달.

기존 네임스페이스 일부는 직렬화/호출 호환을 위해 유지됩니다. W01의 `Managers`와 데이터 모델에 `SWGUnity2DCore` 네임스페이스가 남아 있어도 패키지 소속은 아닙니다.

## W04 적용

서브모듈 루트 `README.md`에 manifest 예제와 게임별 SDK 설정을 정리했습니다. 공통 저장소 수정 커밋을 먼저 푸시한 뒤 W04에서 가져오세요. 로컬 미커밋 변경은 W04로 자동 전달되지 않습니다.

## 검증 도구

- `output/validate-core-compile.py`: 패키지별 어셈블리와 W01 콘텐츠의 별도 컴파일
- `output/prepare-core-unity-validation.py`: W01 에디터를 종료하지 않고 검증할 별도 Unity 프로젝트 생성
- `output/core-validation/unity-validation-final.log`: Unity 실제 import/컴파일/프리팹/배포 씬 검증 결과 (Git 제외)

분리 과정에서 기존 스크립트 `.meta`를 함께 옮겼습니다. 기존 Android 광고 App ID, Gradle 설정, 게임 프리팹과 음원은 W01에서 유지합니다.

## 검증 결과 (2026-09-09)

- Core / AdMob / UGS / W01 콘텐츠 분리 컴파일: 오류 0, 경고 0.
- W01에서 갱신된 Assembly-CSharp 프로젝트 빌드: 오류 0, 경고 0.
- Unity 6000.3.23f1 실제 로딩: 세 로컬 패키지 해결 및 컴파일 성공.
- 전체 프리팹과 활성 빌드 씬 Intro/Game의 게임 오브젝트 82개: Missing Script 없음.
- Core 어셈블리: Google/UGS/Assembly-CSharp 역참조 없음.
- 기존 서브모듈의 스크립트 GUID 35개 보존 및 스크립트 GUID 중복 없음.
- W01 전면 광고 스케줄: 3판 조건과 180초 경계 확인.
- 빌드 대상이 아닌 `Assets/_Recovery/0.unity`에는 기존 MiniGameKitTapSample 누락 스크립트가 있어 최종 배포 씬 검사에서 제외. 원본은 수정하지 않음.
- 이번 작업에서 Android AAB 재빌드 및 실제 기기 광고/UGS 통신 테스트는 수행하지 않음.

기존 `Assets/SWGUnity2DCore` 디렉터리는 다른 Windows 프로세스가 잡고 있어 빈 폴더가 남을 수 있습니다. Git 서브모듈과 Unity 패키지 연결은 새 LocalPackages 경로를 사용합니다.
