# 네온터치 / VioletTap 홍보 패키지

## 현재 상태 — 영상 미완료

게시용 문구 4종과 한·영 60초 편집 대본이 준비되어 있습니다.
최종 MP4, 녹음된 나레이션, 동기화된 SRT는 아직 없습니다. 이 폴더의 준비 파일을 완성 영상 납품으로 취급하지 마세요.

Unity 창 제어와 프로젝트에 설치된 영상·음성 도구 실행이 자동 승인 시스템 오류로 차단되어 촬영·음성 제작을 진행하지 못했습니다.
오류 원문: “Automatic approval review failed: parent compaction checkpoint is incompatible with the Guardian review model or its compatibility is unknown”.

이 오류를 우회하는 실행이나 시스템 설정 변경은 수행하지 않았습니다.

## 게시 문구

- Posting/YouTube_Shorts_KO.txt — 한국어 제목·설명·해시태그·고정 댓글
- Posting/YouTube_Shorts_EN.txt — English title, description, hashtags, pinned comment
- Posting/Instagram_Reels_KO.txt — 한국어 첫 문장·전체 캡션·해시태그·고정 댓글
- Posting/Instagram_Reels_EN.txt — English opening, full caption, hashtags, pinned comment

각 파일에서 항목 제목을 제외한 본문만 복사하면 됩니다. 인스타그램 ‘전체 캡션’에는 첫 문장과 해시태그가 이미 포함되어 있으므로 중복해서 붙이지 마세요.
유튜브 안내에는 ‘채널 프로필 링크 / channel profile link’, 인스타그램 안내에는 ‘프로필 링크 / profile link’를 사용했습니다. 설명과 고정 댓글 모두 게임 이름과 스토어 설치 경로를 포함합니다.

## 제작 준비

- Production/Storyboard_KO_EN.md — 60초 컷 구성, 두 언어 나레이션, 음성·화면 디렉션
- Production/Feature_Check.md — 확인된 기능의 소스 근거와 제외한 주장

게임 화면 녹화를 위한 에디터 전용 보조 스크립트의 CountdownTimer 네임스페이스 누락은 수정했고, 에디터 어셈블리 컴파일은 성공했습니다. 게임 규칙은 변경하지 않았습니다. 녹화 시작 요청은 생성하지 않았습니다.

## 완성 전 필수 검증 — 미수행

- 각 영상 59~60초, 1080×1920, 9:16 MP4
- 해당 언어 게임 UI와 실제 플레이 사용
- HUD·타겟·글자 잘림, 종횡비 왜곡 없음
- 실제 음성에 맞춘 자막의 철자·타이밍 확인
- 음성 발음·끝부분 잘림·클리핑·음악 대비 명료도 확인
- 최종 파일 전체 디코딩 및 시작·중간·끝 재생 확인
- 프로필의 해당 게임 링크와 스토어 접근 상태는 게시 담당자가 확인
