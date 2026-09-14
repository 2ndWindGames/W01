# 홍보 문구 기능 근거

현재 프로젝트 소스와 기존 등록정보를 대조해 작성한 기록입니다. 스토어에 실제 배포된 버전과 같다는 뜻은 아닙니다.

| 문구 | 프로젝트 근거 |
|---|---|
| 빛나는 타겟 터치로 득점 | Assets/01.Scripts/Scene/GameScene.cs — HandleTargetTapped |
| 10콤보부터 ×2, 50콤보부터 ×3 | GameScene.cs — GetScoreMultiplier |
| 피버 중 타겟 증가 | Assets/01.Scripts/Game/GameConfig.cs — feverTargetCount 및 관련 설명; GameScene.cs — StartFever/RefillTargets |
| 모래시계로 시간 추가 | GameScene.cs — TimeBonus 분기, TimeSizeBonus, m_Timer.AddTime |
| 폭탄은 시간 감소 및 콤보 해제 | GameScene.cs — Bomb 분기, AddTime(-2f), m_Combo = 0 |
| 점차 빨리 사라지는 타겟 | Assets/01.Scripts/Game/RoundPacing.cs — Lifetime |
| 1→2→3 순서 터치 | GameScene.cs — StartSequence 및 m_SequenceNextOrder 검증 |
| 개인 최고 기록에 도전 | GameScene.cs — 최고 점수와 결과 처리; StoreListing/ko-KR/store-listing.md |

의도적으로 제외한 주장: 출시일, 정식 출시 완료, 무료 다운로드, 전 세계 특정 순위, 다운로드 수, 실시간 멀티플레이, 실제 사용자 후기, 사람 성우 녹음.

다운로드 안내는 사용자 요청에 따라 프로필의 게임별 링크를 선택하도록 작성했습니다. 실제 프로필 링크의 존재·접속·스토어 공개 상태는 확인하지 않았습니다. 게시 전 ‘네온터치(VioletTap)’ 링크가 맞는 스토어로 연결되는지 확인해야 합니다. 임의 URL은 넣지 않았습니다.
