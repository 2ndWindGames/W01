# Neon Signal Production Asset Pack

인트로, 게임, 설정, 사운드, 랭킹 UI 프리팹에 연결된 제작용 리소스 세트입니다.

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
- `Concepts`: 전체 씬 및 UI 시안 이미지 5장
- `Sources`: 생성 원본 아틀라스. 실제 UI에서는 분리 파일을 사용

모든 분리 리소스는 텍스트가 없는 PNG입니다. 화면 문구는 TMP로 구성합니다.
`NeonSignalAssetImporter`가 이 폴더의 PNG를 Sprite로 가져오고, `NineSlice` 폴더에 Border를 설정합니다.

## 현재 적용 위치

- 인트로: 배경, 타이틀, 에너지 코어, 시작/유틸리티 버튼과 아이콘
- 게임: 배경, HUD 카드, 시작/재시도 버튼, 뒤로가기, 광고 제거, 구분선
- 팝업: 공통 패널, 닫기 버튼, 구분선
- 랭킹: 리스트 행, 가로/세로 스크롤바
- 플레이 타깃: 일반, 퀵, 시간 보너스, 폭탄 타입별 스프라이트

나머지 Controls, VFX, Progress, Decorations 항목은 이후 기능 확장을 위한 바로 사용 가능한 리소스입니다.
