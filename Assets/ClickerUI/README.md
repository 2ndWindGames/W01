# Clicker UI Starter

Unity UI Toolkit 기반의 모바일 세로형 2D 클리커 UI 시작 세트입니다.

## 포함 리소스

- `Resources/ClickerUI/ClickerMain.uxml`: 화면 구조
- `Resources/ClickerUI/ClickerMain.uss`: 색상 토큰, 패널, 버튼, 카드 스타일
- `Resources/ClickerUI/Art/clicker_crystal.png`: 중앙 클릭 대상 이미지
- `Scripts/ClickerUIBootstrap.cs`: UI 자동 생성과 클릭/자동 수익/업그레이드 샘플

플레이 모드로 진입하면 현재 씬 종류와 관계없이 UI가 자동 생성됩니다. 실제 게임 시스템과 연결할 때는 `ClickerUIBootstrap`의 샘플 수치와 구매 메서드를 게임 데이터 서비스로 교체하세요.

기준 해상도는 540×960이며 `Scale With Screen Size`로 다양한 화면 크기에 대응합니다.
