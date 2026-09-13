# 제작용 원본

이 폴더는 게임에서 직접 불러오지 않는 참고 이미지와 편집용 자료를 보관합니다.

- `References`: 이전 콘셉트, 타이틀 시안과 초안 추출 리소스
- `NeonSignalPack/Concepts`: 전체 씬과 UI 시안
- `NeonSignalPack/Sources`: 분리 전 원본 아틀라스
- `NeonSignal`: 기존 타이틀 제작 원본

실제 게임용 이미지는 `Assets/Resources/UI/NeonSignalPack`에 있습니다.
`Tools/BuildNeonSignalAssets.ps1`은 게임용 결과와 제작용 원본을 각각 해당 폴더에 저장합니다.
원본과 `.meta`는 함께 유지해 Unity GUID를 보존합니다.
