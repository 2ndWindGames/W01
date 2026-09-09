$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
function Move-Asset([string]$from, [string]$to) {
    $source = [IO.Path]::GetFullPath((Join-Path $workspace $from))
    $destination = [IO.Path]::GetFullPath((Join-Path $workspace $to))
    foreach ($path in @($source, $destination)) {
        if (!$path.StartsWith($workspace + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Outside workspace: $path" }
    }
    if (!(Test-Path -LiteralPath $source)) { throw "Missing source: $source" }
    if (Test-Path -LiteralPath $destination) { throw "Destination exists: $destination" }
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    Move-Item -LiteralPath $source -Destination $destination
    if (Test-Path -LiteralPath ($source + '.meta')) { Move-Item -LiteralPath ($source + '.meta') -Destination ($destination + '.meta') }
}
$repo = 'LocalPackages/SWGUnity2DCore'
$core = "$repo/Packages/com.secondwind.core/Runtime"
Move-Asset "$repo/Data" 'Assets/01.Scripts/Data'
Move-Asset "$repo/GameConfig.cs" 'Assets/01.Scripts/Game/GameConfig.cs'
foreach ($name in @('Managers', 'DataManager', 'SceneManager', 'IAPManager', 'InterstitialSchedule')) {
    Move-Asset "$repo/Manager/$name.cs" "Assets/01.Scripts/Manager/$name.cs"
}
Move-Asset "$repo/Scene/BaseScene.cs" 'Assets/01.Scripts/Scene/BaseScene.cs'
Move-Asset "$repo/UI" "$core/UI"
Move-Asset "$repo/Util" "$core/Util"
foreach ($name in @('ResourceManager', 'SoundManager', 'UIManager')) {
    Move-Asset "$repo/Manager/$name.cs" "$core/Manager/$name.cs"
}
Move-Asset 'Assets/01.Scripts/UI/Popup/UI_Popup.cs' "$core/UI/UI_Popup.cs"
Move-Asset 'Assets/01.Scripts/Pool/GameObjectPool.cs' "$core/Pool/GameObjectPool.cs"
Move-Asset "$repo/Manager/AdsManager.cs" "$repo/Packages/com.secondwind.ads.admob/Runtime/AdsManager.cs"
Move-Asset "$repo/Manager/RankManager.cs" "$repo/Packages/com.secondwind.leaderboards.ugs/Runtime/RankManager.cs"
