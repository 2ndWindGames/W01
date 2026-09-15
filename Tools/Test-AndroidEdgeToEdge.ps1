param(
    [string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceManifest = Join-Path $projectRoot 'Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/src/main/AndroidManifest.xml'
$sourceHash = (Get-FileHash -LiteralPath $sourceManifest).Hash
$fixtureRoot = Join-Path $projectRoot ('output/android-edge-to-edge-audit/fixtures-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'src/main') -Force | Out-Null
$fixtureManifest = Join-Path $fixtureRoot 'src/main/AndroidManifest.xml'
Copy-Item -LiteralPath $sourceManifest -Destination $fixtureManifest
Add-Type -Path (Join-Path $projectRoot 'Assets/Editor/AndroidEdgeToEdgeProject.cs')
$androidNamespace = 'http://schemas.android.com/apk/res/android'
$checks = 0
function Assert-Check([bool]$condition, [string]$label) {
    if (-not $condition) { throw "FAIL: $label" }
    $script:checks++
    Write-Output "PASS: $label"
}
function Expect-Rejection([string]$expected) {
    $message = ''
    try { [VioletTap.Editor.AndroidEdgeToEdgeProject]::Apply($fixtureRoot) }
    catch { $message = $_.Exception.ToString() }
    Assert-Check ($message.Contains($expected)) "Invalid configuration rejected: $expected"
}

[xml]$before = Get-Content -Raw -LiteralPath $fixtureManifest
$beforeActivity = @($before.manifest.application.activity | Where-Object { $_.GetAttribute('name', $androidNamespace) -eq 'com.unity3d.player.UnityPlayerGameActivity' })[0]
$beforeActivity.SetAttribute('theme', $androidNamespace, '@style/BaseUnityGameActivityTheme') | Out-Null
# A third-party activity fixture must never inherit the game's theme.
$adsActivity = $before.CreateElement('activity')
$adsActivity.SetAttribute('name', $androidNamespace, 'com.google.android.gms.ads.AdActivity') | Out-Null
$adsActivity.SetAttribute('theme', $androidNamespace, '@style/AdSdkTheme') | Out-Null
$before.manifest.application.AppendChild($adsActivity) | Out-Null
$before.Save($fixtureManifest)
$originalFixture = [IO.File]::ReadAllText($fixtureManifest)
[VioletTap.Editor.AndroidEdgeToEdgeProject]::Apply($fixtureRoot)
[xml]$after = Get-Content -Raw -LiteralPath $fixtureManifest
$activity = @($after.manifest.application.activity | Where-Object { $_.GetAttribute('name', $androidNamespace) -eq 'com.unity3d.player.UnityPlayerGameActivity' })[0]
Assert-Check ($activity.GetAttribute('theme', $androidNamespace) -eq '@style/VioletTapGameActivityTheme') 'Only the game launch theme is selected'
$activity.SetAttribute('theme', $androidNamespace, '@style/BaseUnityGameActivityTheme') | Out-Null
Assert-Check ($after.OuterXml -eq $before.OuterXml) 'Permissions, metadata, deep links, activity properties and SDK activity are preserved'
$basePath = Join-Path $fixtureRoot 'src/main/res/values/violettap_edge_to_edge.xml'
$api35Path = Join-Path $fixtureRoot 'src/main/res/values-v35/violettap_edge_to_edge.xml'
[xml]$baseTheme = Get-Content -Raw -LiteralPath $basePath
[xml]$api35Theme = Get-Content -Raw -LiteralPath $api35Path
Assert-Check ($baseTheme.resources.style.parent -eq '@style/BaseUnityGameActivityTheme' -and $baseTheme.SelectNodes('/resources/style/item').Count -eq 0) 'Android 14 and older retain the Unity theme'
Assert-Check ($api35Theme.resources.style.parent -eq '@style/BaseUnityGameActivityTheme') 'Android 15+ preserves the Unity splash parent'
$cutoutItem = $api35Theme.SelectSingleNode('/resources/style/item')
Assert-Check ($cutoutItem.GetAttribute('name') -eq 'android:windowLayoutInDisplayCutoutMode' -and $cutoutItem.InnerText -eq 'always') 'Android 15+ launch explicitly uses ALWAYS'
$firstManifest = [IO.File]::ReadAllText($fixtureManifest)
$firstTheme = [IO.File]::ReadAllText($api35Path)
[VioletTap.Editor.AndroidEdgeToEdgeProject]::Apply($fixtureRoot)
Assert-Check ($firstManifest -eq [IO.File]::ReadAllText($fixtureManifest) -and $firstTheme -eq [IO.File]::ReadAllText($api35Path)) 'Repeated post-processing is idempotent'
Assert-Check (-not $firstTheme.Contains('windowOptOutEdgeToEdgeEnforcement') -and -not $firstTheme.Contains('shortEdges')) 'No opt-out or deprecated SHORT_EDGES added'

[xml]$invalid = $originalFixture
$invalidActivity = @($invalid.manifest.application.activity | Where-Object { $_.GetAttribute('name', $androidNamespace) -eq 'com.unity3d.player.UnityPlayerGameActivity' })[0]
$invalidActivity.SetAttribute('theme', $androidNamespace, '@style/UnrelatedCustomTheme') | Out-Null
$invalid.Save($fixtureManifest)
Expect-Rejection 'Unexpected Android game theme'
$invalidActivity.SetAttribute('theme', $androidNamespace, '@style/BaseUnityGameActivityTheme') | Out-Null
$metadata = @($invalid.manifest.application.'meta-data' | Where-Object { $_.GetAttribute('name', $androidNamespace) -eq 'unity.render-outside-safearea' })[0]
$metadata.SetAttribute('value', $androidNamespace, 'false') | Out-Null
$invalid.Save($fixtureManifest)
Expect-Rejection 'Enable Android Render Outside Safe Area'
$metadata.SetAttribute('value', $androidNamespace, 'true') | Out-Null
$invalid.manifest.application.RemoveChild($invalidActivity) | Out-Null
$invalid.Save($fixtureManifest)
Expect-Rejection 'requires UnityPlayerGameActivity'
Assert-Check ($sourceHash -eq (Get-FileHash -LiteralPath $sourceManifest).Hash) 'Real generated Gradle project remains unchanged by these tests'

# Compile and exercise the actual managed viewport math; no scene/Play Mode or JNI.
$engineAssembly = Join-Path $UnityEditorPath 'Data/Managed/UnityEngine/UnityEngine.CoreModule.dll'
[Reflection.Assembly]::LoadFrom($engineAssembly) | Out-Null
$frameworkReferences = @(
    $engineAssembly,
    (Join-Path $PSHOME 'ref/netstandard.dll'),
    (Join-Path $PSHOME 'ref/System.Runtime.dll')
)
Add-Type -Path (Join-Path $projectRoot 'Assets/01.Scripts/UI/ResponsiveGameViewport.cs') -ReferencedAssemblies $frameworkReferences
foreach ($case in @(
    @(1080, 2340, 0, 96, 1080, 2124, 0),
    @(1080, 2340, 0, 48, 1080, 2172, 90),
    @(2340, 1080, 120, 0, 2124, 1050, 90),
    @(1536, 2048, 0, 96, 1536, 1904, 0),
    @(540, 2340, 0, 48, 540, 2172, 90)
)) {
    $safe = [UnityEngine.Rect]::new($case[2], $case[3], $case[4], $case[5])
    $view = [_01.Scripts.UI.ResponsiveGameViewport]::CalculatePixelRect($case[0], $case[1], $safe, $case[6], 9.0 / 16.0)
    Assert-Check ($view.width -gt 0 -and $view.height -gt 0 -and $view.xMin -ge $safe.xMin -and $view.xMax -le ($safe.xMax + 0.1) -and $view.yMin -ge ($safe.yMin + $case[6] - 0.1) -and $view.yMax -le ($safe.yMax + 0.1)) "Safe viewport: $($case -join ',')"
}
Write-Output "Passed $checks checks. Fixtures: $fixtureRoot"
