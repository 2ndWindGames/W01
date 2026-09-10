# Neon Signal Production Asset Pack

씬 또는 프리팹에는 아직 연결하지 않은 제작용 리소스 세트입니다.

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
