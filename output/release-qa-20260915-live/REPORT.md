# Violet Tap 3.7.2 라이브 배포 QA 보고서

- 검수일: 2026-09-15 (KST)
- 대상: Android 라이브 배포 후보
- 패키지: `com.secondwindgames.violettap`
- 버전: `3.7.2` (`versionCode 12`)
- 최종 판정: **NO-GO — 현재 상태로 Play Console에 업로드하지 말 것**

자동 테스트와 Android 빌드는 모두 통과했다. 다만 서명키 노출, 개인정보처리방침 불일치, 공개 닉네임 정책, 루트·서브모듈의 미커밋 소스 때문에 현재 산출물은 재현 가능한 안전한 라이브 후보가 아니다. 아래 P0 항목을 해결한 뒤 새 키/새 커밋 기준으로 AAB를 다시 만들어야 한다.

## 통과한 항목

| 영역 | 결과 |
|---|---:|
| Unity 전체 회귀 스위트 | 91 PASS / 0 FAIL |
| 집중 QA 19개 스위트 | 1,798 PASS / 0 FAIL |
| 총 assertion | **1,889 PASS / 0 FAIL** |
| Android AAB 빌드 | Succeeded / errors 0 / warnings 1 |
| bundletool validate 및 manifest/config dump | PASS |
| JAR 서명 검증 | PASS |
| ARM64 ELF 16 KB page alignment | PASS |
| Universal APK 16 KB zipalign | PASS |
| Edge-to-edge 소스·fixture 검사 | 16 PASS |
| Git diff whitespace 검사 | 루트/서브모듈 PASS |
| 충돌 마커 검사 | 0건 |

빌드 경고 1건은 iOS용 `unity-plugin-library.xcframework`가 Android에서 지원되지 않아 제외됐다는 패키지 경고다. Android 빌드는 정상 완료됐다. 1차 BuildReport에는 이전 Play Mode QA에서 Microsoft GDK 패키지가 Google Ads 읽기 전용 샘플 prefab 저장을 시도한 콘솔 오류 2건이 포함됐고, 콘솔을 정리한 뒤 같은 소스로 다시 빌드한 최종 BuildReport는 errors 0이다. `android-build-messages.txt`는 이 1차 원인 기록이고 `android-build-result.txt`가 최종 결과다.

## 검증한 AAB

- 파일: `Builds/Android/violettap-3.7.2-release-candidate.aab`
- 크기: 110,089,394 bytes
- SHA-256: `87D51A8FF54B683E789DE6CB704F3540012A5765FBD9CA0B513F163E95ECE248`
- Android: IL2CPP, ARM64, minSdk 25, targetSdk 36, AAB, development build 해제
- manifest: `debuggable`/`testOnly` 없음, edge-to-edge 테마 적용
- BundleConfig: `PAGE_ALIGNMENT_16K`
- Universal APK: 89,709,696 bytes, 예상 다운로드 87,642,269 bytes
- 실제 빌드 메타데이터에서 `SECOND PULSE`, `ShowRewardedAd`, 운영용 보상형 광고 단위 포함을 확인했다.

이 파일은 기능·패키징 검증용이다. 아래 서명키 문제 때문에 라이브 업로드 파일로 사용하면 안 된다.

## P0 배포 차단 항목

### 1. Android 서명키가 Git 이력에 포함됨

`user.keystore`가 현재 추적 파일이며 원격 `main` 이력의 최초 커밋부터 존재한다. 현재 AAB 서명자도 이 키와 일치한다. 생성된 Gradle/Bee 캐시에는 서명 자격 증명이 평문으로 남을 수 있고, 인증서는 RSA 2048이지만 레거시 `SHA1withRSA` 서명을 사용한다.

필수 조치:

1. Play Console의 **앱 서명** 화면에서 이 키가 업로드 키인지 앱 서명 키인지 확인한다.
2. 업로드 키라면 재설정/교체하고, 신규 앱이라면 SHA-256 기반의 새 키를 사용한다. 앱 서명 키라면 로컬에서 임의 교체하지 말고 Play의 키 업그레이드 절차를 따른다.
3. Git 이력에서 키를 제거하고 `*.keystore`, `*.jks`, `*.p12`를 ignore 처리한다.
4. 기존 GitHub Actions 캐시를 삭제하고 Bee/Gradle 서명 출력물을 캐시에서 제외한다.
5. 새 서명 상태로 AAB를 다시 빌드하고 인증서 지문을 Play Console과 대조한다.

참고: [Android 앱 서명 공식 문서](https://developer.android.com/studio/publish/app-signing?hl=ko)

### 2. 현재 AAB의 루트·서브모듈 소스가 모두 미커밋 상태

루트 저장소는 게임 코드, UI prefab, 폰트, QA runner를 포함한 tracked 파일 19개가 수정된 상태다. 현재 AAB는 루트 HEAD `95856b0a190d9c1afba5005d1d72e115333a9497`만으로 재현되지 않는다.

`LocalPackages/SWGUnity2DCore`의 다음 파일이 수정됐지만 내부 커밋이 없다.

- `Packages/com.secondwind.ads.admob/Runtime/AdMobOptions.cs`
- `Packages/com.secondwind.ads.admob/Runtime/AdsManager.cs`
- `Packages/com.secondwind.leaderboards.ugs/Runtime/RankManager.cs`
- `README.md`

부모 저장소의 gitlink는 여전히 `e5f778f90aae0eeaa19146e79ef28781bb00d392`를 가리킨다. 현재 로컬 AAB에는 수정이 들어갔지만 clean clone과 CI에서는 재현되지 않는다. 서브모듈 변경을 먼저 커밋·푸시하고 부모 저장소의 gitlink를 갱신한 뒤, 루트 변경 전체를 커밋하고 그 커밋에서 AAB를 다시 만들어야 한다.

### 3. 개인정보처리방침이 실제 앱 동작과 불일치

`index.html`은 개인정보를 수집·저장하지 않고 광고·로그인·분석 도구를 사용하지 않는 구성이라고 명시한다. 실제 앱은 AdMob/UMP, Unity 익명 인증, UGS PlayerId·닉네임·점수, Google Play Billing을 사용한다. 앱 안에는 UMP의 개인정보 옵션만 있고 개인정보처리방침을 여는 링크는 없다.

필수 조치:

- Violet Tap/사업자·연락처, 광고 및 기기 식별자, Unity 인증/리더보드 데이터, 구매 정보, 제3자 제공, 보존·삭제·문의 절차를 실제 동작에 맞게 작성한다.
- 앱 내부와 Play Console 스토어 등록정보에 공개 URL을 연결한다.
- Play Console 데이터 보안 응답 및 AdMob 개인정보 URL과 같은 내용으로 맞춘다.

Google Play는 앱과 스토어 등록정보에 정확한 개인정보처리방침을 요구한다. [Google Play 사용자 데이터 정책](https://support.google.com/googleplay/android-developer/answer/10144311?hl=en-GB)

### 4. 공개 닉네임에 UGC 보호 장치가 없음

최고 기록 사용자는 최대 50자의 자유 입력 닉네임을 전 세계 랭킹에 공개할 수 있다. 현재 제어 문자/리치 텍스트 방지만 있고 약관 동의, 금칙어·모더레이션, 신고, 차단 기능은 없다.

출시 전 다음 중 하나를 선택해야 한다.

- 빠른 출시안: 서버/앱이 생성하는 닉네임 또는 허용 목록 조합만 사용
- 자유 입력 유지안: 이용약관 동의, 필터·모더레이션, 신고 및 차단 기능 구현

[Google Play 사용자 제작 콘텐츠 정책](https://support.google.com/googleplay/android-developer/answer/9876937?hl=ko)

### 5. 실기기 및 실제 서비스 E2E 검사가 생략됨

사용자 요청에 따라 이번 검수에서는 USB 실기기 검사를 생략했다. 또한 에디터 QA는 `GameScene.SuppressSecondPulseForQa = true`로 실행되어 실제 보상형 광고의 성공·스킵·실패와 +10초 지급 경로를 직접 실행하지 않는다. 구매 QA도 실제 Play 결제가 아닌 합성 콜백이다.

내부 테스트 트랙에서 다음을 확인해야 한다.

- UMP 동의 허용/거부/개인정보 옵션 재열기
- 배너, 3회 재시작·시간 조건 전면 광고, no-fill·오프라인 복구
- 보상형 광고 성공/스킵/실패, +10초 1회 지급, 백그라운드 전환
- 결제 성공/취소/보류/재설치/환불/계정 변경
- UGS 점수 제출·조회·오프라인 재시도
- Android 15/16 실제 기기의 컷아웃, 시스템 바, 키보드, 진동·사운드·성능

## P1 출시 전 수정 권장

- 랭킹 제출 실패 또는 결과창에서 빠르게 이탈하면 기록을 잃을 수 있다. pending queue와 재시도가 필요하다.
- UMP/AdMob 초기화 실패 후 자동 재시도 경로와 광고 callback watchdog이 없다.
- 보상형 광고 닫힘과 보상 callback의 순서가 늦어지는 mediation 환경에서 보상을 놓칠 수 있다.
- 광고 제거 구매자도 UMP/AdMob을 초기화하고 전면·보상형 광고를 미리 로드한다.
- IAP 연결/상품 조회 재시도와 보류·환불·계정 변경 복구를 보강해야 한다.
- 릴리스 로그에 PlayerId, 닉네임, 점수가 출력된다. 삭제하거나 비식별화해야 한다.
- 리더보드 점수는 클라이언트 제출값을 신뢰한다. UGS 대시보드의 접근 제어와 부정 점수 대응을 확인해야 한다.
- `app-ads.txt`가 저장소에서 확인되지 않았다.
- CI는 현재 APK 위주이며 서명 AAB의 재현성을 검증하지 않는다. checkout에 `submodules: recursive`도 없어 clean runner가 `LocalPackages`를 받지 못할 수 있다. 전체 `Library` 캐시는 서명 산출물을 포함할 수 있다.
- WorkManager가 `FOREGROUND_SERVICE`를 병합하므로 Play Console 선언 필요 여부를 확인해야 한다.
- AAB는 ARM64 전용이다. 32비트 기기 지원 제외가 의도한 배포 범위인지 확인해야 한다.
- Play Console의 광고 포함 여부, 콘텐츠 등급, 타겟 연령층, 데이터 보안, 앱 접근 권한 선언은 로컬에서 검증할 수 없어 별도 대조가 필요하다.

## UI·연출 점검

랭킹 창은 세로 공간, 스크롤 영역, 고정된 내 순위 행, 색상 구분이 의도대로 적용됐고 순위/점수가 네온 프레임과 겹치지 않았다. 도움말 제목·본문·버튼 여백도 정상 범위다.

남은 시각 이슈:

- SCORE/BEST 값 `100000`은 카드 경계에 약 1.3 px 닿는다.
- 10/50 콤보 지속 프레임에서 SCORE/TIME/BEST가 한 프레임 사라지는 캡처가 있다. 의도한 글리치라면 실제 재생 속도와 빈도를 실기기에서 확인해야 한다.
- 10 COMBO 녹색 글자가 50 COMBO 노란색보다 어두워 가독성과 임팩트가 약하다.
- 도움말의 글리치 프레임에서 아이콘/프레임이 잠시 사라졌다가 다음 프레임에 복구된다.

관련 이미지: `screenshots/hud-values-english.png`, `screenshots/combo-10-sustained.png`, `screenshots/combo-50-sustained.png`, `screenshots/help-korean-glitch-frame.png`.

## 용량 및 산출물 관리

| 경로 | 파일 수 | 크기 |
|---|---:|---:|
| Assets | 1,040 | 187,883,440 bytes |
| LocalPackages/SWGUnity2DCore | 4,209 | 37,944,744 bytes |
| Builds | 2,350 | 2,968,445,811 bytes |
| Deliverables | 40 | 88,953,495 bytes |
| output | 15,410 | 1,824,679,581 bytes |
| Library | 35,414 | 9,476,763,501 bytes |

`Builds`에는 과거 백업·mapping·symbols 및 패키지/버전이 다른 오래된 APK/AAB가 함께 있다. 배포 자동화가 잘못된 파일을 선택하지 않도록 최종 AAB 경로를 고정하고 과거 산출물은 별도 보관하는 편이 안전하다.

## GO 전환 조건

- [ ] Play 앱 서명 키 역할 확인 및 노출된 키 조치 완료
- [ ] 서브모듈 커밋·푸시, 부모 gitlink 갱신, 루트 변경 전체 커밋
- [ ] 개인정보처리방침, 앱 내부 링크, 데이터 보안, AdMob 설정 일치
- [ ] 공개 닉네임 UGC 대응 방식 적용
- [ ] Play Console에서 `versionCode 12` 사용 여부 확인; 이미 사용했다면 증가
- [ ] 내부 트랙 실기기 광고·결제·랭킹·동의·Android 15/16 검사 통과
- [ ] 새 커밋·서명 상태로 AAB 재빌드 및 동일 검증 재실행

Google Play의 현재 신규 앱/업데이트 target API 요구 수준과 비교하면 이 후보의 targetSdk 36은 충족한다. [Google Play 대상 API 수준 요구사항](https://support.google.com/googleplay/android-developer/answer/11926878?hl=ko)

## 증빙

- `qa-summary.txt`: 자동 테스트 총계
- `focused-summary.tsv`: 19개 집중 스위트별 결과
- `android-build-result.txt`: 최종 Unity BuildReport
- `manifest-checks.txt`, `android-manifest.xml`, `bundle-config.txt`: 패키지/SDK/manifest 검증
- `signing-summary.txt`, `signing-certificate.txt`, `jarsigner-verify.txt`: 서명 검증
- `elf-16kb.txt`, `zipalign-16kb.txt`, `universal-apk-checks.txt`: 16 KB 및 APK 검사
- `compiled-feature-presence.txt`: 보상형 기능의 최종 바이너리 포함 여부
- `screenshots/`: 대표 UI 캡처 12장

QA 중 생성한 임시 빌드 훅, sentinel, universal APK 및 테스트 변형 파일은 제거했다. 폰트/TMP 자산은 사전 해시와 대조해 QA 전 상태로 복구했다. 동일 초에 여러 QA를 실행할 때 결과 폴더 충돌과 `latest-run.txt` 잠금 오류가 발생해 `Assets/Editor/VioletTapQaRunner.cs`의 결과 폴더 정밀도와 재시도 저장 로직을 보강했다.
