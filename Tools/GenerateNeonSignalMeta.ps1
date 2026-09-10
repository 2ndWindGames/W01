param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$assetRoot = Join-Path $ProjectRoot 'Assets\Resources\UI\NeonSignalPack'

function Get-StableGuid([string]$assetPath) {
    $normalized = $assetPath.Replace('\', '/').ToLowerInvariant()
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        return ([BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant()
    }
    finally { $md5.Dispose() }
}

function Get-SpriteBorder([string]$relativePath) {
    $name = [IO.Path]::GetFileNameWithoutExtension($relativePath)
    if ($relativePath -match '^NineSlice[/\\]Panels') {
        if ($name -eq 'hud_card') { return '40, 28, 40, 28' }
        if ($name -eq 'header_strip') { return '64, 24, 64, 24' }
        return '64, 64, 64, 64'
    }
    if ($relativePath -match '^NineSlice[/\\]Buttons') {
        if ($name -like 'icon_button*') { return '36, 36, 36, 36' }
        return '52, 36, 52, 36'
    }
    if ($relativePath -match '^NineSlice[/\\]Lists') { return '48, 28, 48, 28' }
    return '0, 0, 0, 0'
}

$template = @'
fileFormatVersion: 2
guid: __GUID__
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 4096
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: __BX__, y: __BY__, z: __BZ__, w: __BW__}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
'@

$utf8NoBom = New-Object Text.UTF8Encoding($false)
$count = 0
Get-ChildItem -LiteralPath $assetRoot -Recurse -Filter '*.png' | ForEach-Object {
    $relative = $_.FullName.Substring($ProjectRoot.Length + 1).Replace('\', '/')
    $border = (Get-SpriteBorder $_.FullName.Substring($assetRoot.Length + 1)).Split(',').ForEach({ $_.Trim() })
    $meta = $template.Replace('__GUID__', (Get-StableGuid $relative))
    $meta = $meta.Replace('__BX__', $border[0]).Replace('__BY__', $border[1]).Replace('__BZ__', $border[2]).Replace('__BW__', $border[3])
    [IO.File]::WriteAllText($_.FullName + '.meta', $meta, $utf8NoBom)
    $count++
}

Write-Output "Generated $count sprite meta files in $assetRoot"
