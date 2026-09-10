param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$sprite = @{
    BackgroundIntro = '2d30049a75675bc1f46e0c9c480747cc'
    BackgroundGame = '8ec1c9bb32f4760d8909eb39ee1749b4'
    Popup = '9218d2daad66b49c325948fa2bc16077'
    Hud = 'f20a337db1f595277dafb979a20fc77e'
    Primary = 'bcede17e7a5653c27871a748074079ef'
    PrimaryPressed = '91e60154fac8235d3a92b8599481c00a'
    PrimaryDisabled = 'a7bb578b49ae98f1067bc553da3fc455'
    IconButton = 'e5e3b5c7cd02352774da658870d28b6b'
    IconButtonPressed = 'a3afde0af34e62100d65bb6660aa4a6d'
    IconButtonDisabled = '6e7b37f793528f3b4515cef40b5abcda'
    ListRow = 'e54a9f5331af3a44c95689c769a5b3b5'
    SliderTrack = 'a97e3f0f141b1f9292c5653e259851bb'
    ScrollbarHandle = '305e40f3bf357bff34d9d5543687667e'
    Back = '1646b08e7256430c546c41169770cec1'
    Settings = 'e465c7e3d77089778953286a0addc7b4'
    Ranking = '0bfe3db24b4bfdad2736bc9849099360'
    Sound = 'bd643a1d60f16c3eca844fa29a13cec3'
    RemoveAds = '0cfd87c2ccc94655d2390509753ecd60'
    EnergyCore = '02a2a44c77fcce46cf7d897afdee31ea'
    Divider = '367939f6b221548f703d082addb00b32'
    Title = '91a314a45620938681756114dede4ac7'
}

function SpriteRef([string]$key) { return "{fileID: 21300000, guid: $($sprite[$key]), type: 3}" }

function Update-Prefab([string]$relativePath, [hashtable]$imageMap, [hashtable]$buttonMap) {
    $path = Join-Path $ProjectRoot $relativePath
    $text = [IO.File]::ReadAllText($path)
    $names = @{}
    [regex]::Matches($text, '(?ms)^--- !u!1 &(-?\d+)\r?\nGameObject:.*?^  m_Name: (.*?)\r?$') | ForEach-Object {
        $names[$_.Groups[1].Value] = $_.Groups[2].Value
    }

    $transforms = @{}
    [regex]::Matches($text, '(?ms)^--- !u!224 &(-?\d+)\r?\nRectTransform:.*?(?=^--- !u!|\z)') | ForEach-Object {
        $block = $_.Value
        $go = [regex]::Match($block, 'm_GameObject: \{fileID: (-?\d+)\}').Groups[1].Value
        $father = [regex]::Match($block, 'm_Father: \{fileID: (-?\d+)\}').Groups[1].Value
        $transforms[$_.Groups[1].Value] = @($go, $father)
    }

    $goToParentName = @{}
    foreach ($transformId in $transforms.Keys) {
        $go = $transforms[$transformId][0]
        $father = $transforms[$transformId][1]
        if ($transforms.ContainsKey($father)) { $goToParentName[$go] = $names[$transforms[$father][0]] }
    }

    $updated = [regex]::Replace($text, '(?ms)^--- !u!114 &(-?\d+)\r?\nMonoBehaviour:.*?(?=^--- !u!|\z)', {
        param($match)
        $block = $match.Value
        $goMatch = [regex]::Match($block, 'm_GameObject: \{fileID: (-?\d+)\}')
        if (-not $goMatch.Success) { return $block }
        $go = $goMatch.Groups[1].Value
        $name = $names[$go]
        $qualifiedName = if ($name -eq 'icon' -and $goToParentName.ContainsKey($go)) { "$($goToParentName[$go])/icon" } else { $name }

        if ($block -match '(?m)^  m_Sprite: ' -and $imageMap.ContainsKey($qualifiedName)) {
            $entry = $imageMap[$qualifiedName]
            $block = [regex]::Replace($block, '(?m)^  m_Sprite: .*$', "  m_Sprite: $(SpriteRef $entry[0])", 1)
            if ($entry[1]) { $block = [regex]::Replace($block, '(?m)^  m_Type: \d+$', '  m_Type: 1', 1) }
        }

        if ($block -match '(?m)^  m_SpriteState:' -and $buttonMap.ContainsKey($name)) {
            $states = $buttonMap[$name]
            $block = [regex]::Replace($block, '(?m)^  m_Transition: \d+$', '  m_Transition: 2', 1)
            $block = [regex]::Replace($block, '(?m)^    m_HighlightedSprite:.*$', "    m_HighlightedSprite: $(SpriteRef $states[0])", 1)
            $block = [regex]::Replace($block, '(?m)^    m_PressedSprite:.*$', "    m_PressedSprite: $(SpriteRef $states[0])", 1)
            $block = [regex]::Replace($block, '(?m)^    m_SelectedSprite:.*$', "    m_SelectedSprite: $(SpriteRef $states[0])", 1)
            $block = [regex]::Replace($block, '(?m)^    m_DisabledSprite:.*$', "    m_DisabledSprite: $(SpriteRef $states[1])", 1)
        }
        return $block
    })

    [IO.File]::WriteAllText($path, $updated, (New-Object Text.UTF8Encoding($false)))
    Write-Output "Applied Neon Signal sprites to $relativePath"
}

$primaryStates = @('PrimaryPressed', 'PrimaryDisabled')
$iconStates = @('IconButtonPressed', 'IconButtonDisabled')

Update-Prefab 'Assets\Resources\Prefabs\UI\Popup\UI_IntroPopup.prefab' @{
    img_energy_core = @('EnergyCore', $false)
    btn_start = @('Primary', $true)
    'btn_sound/icon' = @('Sound', $false)
    'btn_rank/icon' = @('Ranking', $false)
    btn_setting = @('IconButton', $true)
    'btn_setting/icon' = @('Settings', $false)
    btn_sound = @('IconButton', $true)
    btn_rank = @('IconButton', $true)
    img_bg = @('BackgroundIntro', $false)
    img_title = @('Title', $false)
} @{
    btn_start = $primaryStates
    btn_setting = $iconStates
    btn_sound = $iconStates
    btn_rank = $iconStates
}

Update-Prefab 'Assets\Resources\Prefabs\UI\Popup\UI_GamePopup.prefab' @{
    score = @('Hud', $true)
    time = @('Hud', $true)
    best = @('Hud', $true)
    btnStart = @('Primary', $true)
    btnRetry = @('Primary', $true)
    btnBack = @('Back', $false)
    btnAds = @('RemoveAds', $false)
    img_line_0 = @('Divider', $false)
    img_line_1 = @('Divider', $false)
    img_bg = @('BackgroundGame', $false)
} @{
    btnStart = $primaryStates
    btnRetry = $primaryStates
}

foreach ($popup in @('UI_Rankpopup.prefab', 'UI_SettingPopup.prefab', 'UI_SoundPopup.prefab')) {
    $popupImages = @{
        imgBg = @('Popup', $true)
        underline = @('Divider', $false)
        btnClose = @('IconButton', $true)
    }
    if ($popup -eq 'UI_Rankpopup.prefab') {
        $popupImages['Scrollbar Vertical'] = @('SliderTrack', $true)
        $popupImages['Scrollbar Horizontal'] = @('SliderTrack', $true)
        $popupImages['Handle'] = @('ScrollbarHandle', $true)
    }
    Update-Prefab "Assets\Resources\Prefabs\UI\Popup\$popup" $popupImages @{ btnClose = $iconStates }
}

Update-Prefab 'Assets\Resources\Prefabs\UI\SubItem\itemRanking.prefab' @{
    itemRanking = @('ListRow', $true)
} @{}
