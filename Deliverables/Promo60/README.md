# 네온터치 / VioletTap 홍보 영상 패키지

한국어·영어 홍보 영상 각 1편을 완성했습니다. 두 영상은 각각 59.93초, 세로 9:16, 1080×1920·30fps MP4(H.264, AAC)입니다. 실제 Unity 게임 화면과 프로젝트의 음악·효과음, 해당 언어의 로컬 AI 나레이션 및 화면 자막을 사용했습니다.

## 완성 영상

- [한국어 영상](VioletTap_Promo_KO_1080x1920_60s.mp4) · [나레이션 SRT](VioletTap_Promo_KO.srt)
- [English video](VioletTap_Promo_EN_1080x1920_60s.mp4) · [narration SRT](VioletTap_Promo_EN.srt)
- [한국어 컷 미리보기](Production/Preview_KO.jpg) · [English shot preview](Production/Preview_EN.jpg)
- [검증 결과](Production/Validation.md)

영상에 쓰인 큰 자막은 화면에 포함되어 있습니다. SRT는 나레이션 실제 길이에 맞춘 별도 자막 파일입니다.

## 복사용 게시 문구

각 파일에 제목 또는 첫 문장, 설명글·전체 캡션, 해시태그, 고정 댓글이 있습니다. 인스타그램의 ‘전체 캡션’은 첫 문장과 해시태그를 이미 포함하므로 한 번만 복사하면 됩니다. 유튜브는 ‘설명글’ 끝에 해시태그를 포함했습니다.

- [유튜브 쇼츠 · 한국어](Posting/YouTube_Shorts_KO.txt)
- [YouTube Shorts · English](Posting/YouTube_Shorts_EN.txt)
- [인스타그램 릴스 · 한국어](Posting/Instagram_Reels_KO.txt)
- [Instagram Reels · English](Posting/Instagram_Reels_EN.txt)

네 문구 모두 **네온터치(VioletTap)** 다운로드 링크를 선택해 스토어에서 설치하는 경로를 설명글과 고정 댓글에 명시합니다. 유튜브는 ‘채널 프로필 링크’, 인스타그램은 ‘프로필 링크’로 구분했습니다. 실제 프로필의 게임별 링크와 스토어 공개 상태는 확인되지 않았으므로 게시 직전에 눌러 확인해야 합니다.

## 출처와 제작상 한계

게임 플레이는 `output/promo-production/capture-*/`에서 새로 촬영한 언어별 Unity Game View 녹화를 활용했습니다. 촬영은 1080×1920·30fps에서 게임 규칙과 타겟 수명을 바꾸지 않고 입력만 자동화했습니다. 인트로 컷은 `output/play-store-3.7.1/`의 해당 언어 게임 캡처를 사용했습니다. 영문 영상의 플레이·UI 소스는 모두 영문이며, 10·50콤보, 피버, 모래시계, 폭탄, 숫자 순서 장면은 실제 플레이 녹화에서 선택했습니다.

게임에 별도의 캐릭터가 없어 네온 타겟과 게임 타이틀을 중심 비주얼로 사용했습니다. 출시일·가격·순위·다운로드 수처럼 확인되지 않은 정보는 영상과 게시 문구에 넣지 않았습니다. 나레이션은 로컬 Supertonic 3 모델로 합성했고, 게시 문구에 AI 생성 음성 고지를 넣었습니다. 사람 성우 녹음으로 표기하지 않습니다.

`Production/Storyboard_KO_EN.md`에 최종 컷 구성, `Production/Feature_Check.md`에 기능 근거, `Production/Voice/`에 나레이션 대본과 WAV, `Production/build_promo.py`에 재렌더링 절차가 있습니다.
