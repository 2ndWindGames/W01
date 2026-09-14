Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$raw = Join-Path $root 'StoreListing/raw'
$common = Join-Path $root 'StoreListing/common'
$backgroundPath = Join-Path $root 'StoreListing/source/feature_energy_background.png'
$fontPath = Join-Path $root 'Assets/Resources/Fonts/NotoSansKR-Variable.ttf'
$mapping = @{
    'en-US' = [ordered]@{
        '01_intro.png' = 'game_view_20260913_102037.png'
        '02_ready.png' = 'game_view_20260913_104429.png'
        '03_gameplay.png' = 'game_view_20260913_103619.png'
        '04_sound.png' = 'game_view_20260913_104316.png'
    }
    'ko-KR' = [ordered]@{
        '01_intro.png' = 'game_view_20260913_102635.png'
        '02_ready.png' = 'game_view_20260913_104715.png'
        '03_gameplay.png' = 'game_view_20260913_103335.png'
        '04_sound.png' = 'game_view_20260913_104610.png'
    }
}

$fonts = [System.Drawing.Text.PrivateFontCollection]::new()
$fonts.AddFontFile($fontPath)
try {
    foreach ($locale in $mapping.Keys) {
        $screenshots = Join-Path $root "StoreListing/$locale/screenshots"
        New-Item -ItemType Directory -Force -Path $screenshots | Out-Null
        foreach ($name in $mapping[$locale].Keys) {
            $source = Join-Path $raw $mapping[$locale][$name]
            if (-not (Test-Path -LiteralPath $source)) { throw "Missing Unity capture: $source" }
            Copy-Item -LiteralPath $source -Destination (Join-Path $screenshots $name) -Force
        }

        # Preview only: the four untouched portrait captures are the store uploads.
        $sheet = [System.Drawing.Bitmap]::new(1520, 860)
        $g = [System.Drawing.Graphics]::FromImage($sheet)
        try {
            $g.Clear([System.Drawing.Color]::FromArgb(7, 9, 30))
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
            $labelFont = [System.Drawing.Font]::new($fonts.Families[0], 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
            $labelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(217, 237, 255))
            try {
                $labels = if ($locale -eq 'ko-KR') { @('인트로', '시작 전', '플레이', '사운드') } else { @('INTRO', 'READY', 'GAMEPLAY', 'SOUND') }
                $i = 0
                foreach ($name in $mapping[$locale].Keys) {
                    $capture = [System.Drawing.Image]::FromFile((Join-Path $screenshots $name))
                    try {
                        $x = 20 + 380 * $i
                        $g.DrawImage($capture, [System.Drawing.Rectangle]::new($x, 20, 360, 740))
                        $g.DrawString($labels[$i], $labelFont, $labelBrush, [float]($x + 10), 786.0)
                    }
                    finally { $capture.Dispose() }
                    $i++
                }
            }
            finally { $labelFont.Dispose(); $labelBrush.Dispose() }
            $sheet.Save((Join-Path $common "contact_sheet_$locale.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $g.Dispose(); $sheet.Dispose() }

        # Promotional key art, intentionally not a gameplay capture.
        $base = [System.Drawing.Image]::FromFile($backgroundPath)
        $feature = [System.Drawing.Bitmap]::new(1024, 500)
        $gfx = [System.Drawing.Graphics]::FromImage($feature)
        try {
            $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $gfx.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
            $gfx.DrawImage($base, [System.Drawing.Rectangle]::new(0, 0, 1024, 500))
            $title = if ($locale -eq 'ko-KR') { '네온터치' } else { 'VIOLET TAP' }
            $subtitle = if ($locale -eq 'ko-KR') { '빛나는 타겟을 터치하세요' } else { 'TAP THE GLOWING TARGETS' }
            $titleSize = if ($locale -eq 'ko-KR') { 78 } else { 66 }
            $titleFont = [System.Drawing.Font]::new($fonts.Families[0], $titleSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
            $subFont = [System.Drawing.Font]::new($fonts.Families[0], 24, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
            $shadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(145, 146, 53, 241))
            $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(239, 247, 255))
            $cyan = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(99, 224, 255))
            try {
                $gfx.DrawString($title, $titleFont, $shadow, 40.0, 187.0)
                $gfx.DrawString($title, $titleFont, $white, 36.0, 183.0)
                $gfx.DrawString($subtitle, $subFont, $cyan, 40.0, 298.0)
            }
            finally { $titleFont.Dispose(); $subFont.Dispose(); $shadow.Dispose(); $white.Dispose(); $cyan.Dispose() }
            $feature.Save((Join-Path $common "feature_graphic_$locale.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $gfx.Dispose(); $feature.Dispose(); $base.Dispose() }
    }
}
finally { $fonts.Dispose() }
