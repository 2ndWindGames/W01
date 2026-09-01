# Intro UI Resources

버튼은 텍스트와 아이콘이 없는 나인슬라이스용 PNG입니다.

## Buttons

- `btn_primary_normal.png`
- `btn_primary_pressed.png`
- `btn_primary_disabled.png`
- `btn_utility_normal.png`
- `btn_utility_pressed.png`
- `btn_utility_disabled.png`

Unity `Image` 컴포넌트의 `Type`을 `Sliced`로 설정하세요. Sprite Border는 `IntroUITexturePostprocessor`가 자동 적용합니다.

`Button > Transition`은 `Sprite Swap`으로 설정합니다.

```text
Target Graphic     : 버튼의 Background Image
Highlighted Sprite : normal 또는 pressed
Pressed Sprite     : pressed
Disabled Sprite    : disabled
```

## Icons

- `icon_settings.png`
- `icon_sound_on.png`
- `icon_sound_off.png`

아이콘은 버튼의 자식 `Image`로 배치하고 `Preserve Aspect`를 활성화하세요. 버튼 텍스트도 별도의 TMP 오브젝트로 구성합니다.

```text
Button
├─ Background Image (Sliced)
└─ Content
   ├─ Icon Image
   └─ Label (TextMeshProUGUI)
```

`Content`에 `HorizontalLayoutGroup`을 사용하면 아이콘 크기, 간격, 텍스트 위치를 버튼 이미지와 독립적으로 조정할 수 있습니다. 눌림 피드백으로 콘텐츠까지 아래로 이동하려면 `Content`의 Anchored Position만 별도로 애니메이션하세요.

Resources 로드 경로 예시:

```csharp
Resources.Load<Sprite>("UI/Intro/Buttons/btn_primary_normal");
Resources.Load<Sprite>("UI/Intro/Icons/icon_settings");
```

## Visuals

- `Visuals/intro_title.png`: 3072×1676, 원본의 날카로운 기하학적 형태를 보존한 실제 투명 알파 타이틀
- `Visuals/intro_energy_core.png`: 2048×2048, 실제 투명 알파 코어

코어 Image에 `IntroCoreAnimator`를 추가하면 느린 회전, 크기 호흡, 밝기 펄스와 UV 일렁임이 함께 적용됩니다. 애니메이션은 `Time.unscaledTime`을 사용하므로 인트로에서 `Time.timeScale`이 0이어도 작동합니다.
