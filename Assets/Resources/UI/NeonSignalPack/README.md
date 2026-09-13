# Neon Signal Production Asset Pack

인트로, 게임, 사운드, 랭킹 UI 프리팹에 연결된 제작용 리소스 세트입니다.

## 폴더

- `Backgrounds`: 인트로/게임 세로 배경
- `NineSlice`: 패널, 버튼 상태, 리스트 행. Unity Sprite Border 자동 설정
- `Controls`: 체크박스, 토글, 슬라이더, 스크롤바
- `Icons`: 공통 기능 아이콘 16종
- `Targets`: 게임 타깃 8종
- `VFX`: 타격/퍼펙트/실패/콤보/시간/피버/폭탄/터치 효과
- `Progress`: 에너지, 콤보, 등급, 상위 랭크 배지
- `Decorations`: 인트로 코어, 궤도, 발판, 장식선, 배지
- `Branding`: 게임 타이틀과 회사 로고

전체 씬 시안은 `Assets/ArtSource/NeonSignalPack/Concepts`, 원본 아틀라스는
`Assets/ArtSource/NeonSignalPack/Sources`, 이전 참고 자료는 `Assets/ArtSource/References`에 보관합니다.
제작용 원본을 런타임 `Resources`와 분리해 앱에 자동 포함되지 않도록 합니다.

모든 분리 리소스는 텍스트가 없는 PNG입니다. 화면 문구는 TMP로 구성합니다.
`NeonSignalAssetImporter`가 이 폴더의 PNG를 Sprite로 가져옵니다. Nine-slice Border는 Sprite Editor에서 설정한 값을 보존합니다.

## 현재 적용 위치

- 인트로: 배경, 타이틀, 에너지 코어, 시작/유틸리티 버튼과 아이콘
- 게임: 배경, HUD 카드, 시작/재시도 버튼, 뒤로가기, 광고 제거, 구분선
- 팝업: 공통 패널, 닫기 버튼, 구분선
- 랭킹: 리스트 행, 가로/세로 스크롤바
- 플레이 타깃: 일반, 퀵, 시간 보너스, 폭탄 타입별 스프라이트

나머지 Controls, VFX, Progress, Decorations 항목은 이후 기능 확장을 위한 바로 사용 가능한 리소스입니다.
