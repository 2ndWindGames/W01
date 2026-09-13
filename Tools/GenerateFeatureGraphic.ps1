param(
    [Parameter(Mandatory = $true)]
    [string]$BackgroundPath
)

Add-Type -AssemblyName System.Drawing

$workspace = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $workspace 'StoreListing/common'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$titlePath = Join-Path $workspace 'Assets/Resources/UI/Intro/Visuals/intro_title.png'
$normalPath = Join-Path $workspace 'Assets/Resources/UI/NeonSignalPack/Targets/target_normal.png'
$quickPath = Join-Path $workspace 'Assets/Resources/UI/NeonSignalPack/Targets/target_quick.png'
$timePath = Join-Path $workspace 'Assets/Resources/UI/NeonSignalPack/Targets/target_time.png'
$fontPath = Join-Path $workspace 'Assets/Resources/Fonts/NotoSansKR-Variable.ttf'

$fonts = [System.Drawing.Text.PrivateFontCollection]::new()
$fonts.AddFontFile($fontPath)

function Draw-CroppedBackground($graphics, $image) {
    $targetRatio = 1024.0 / 500.0
    $sourceRatio = $image.Width / [double]$image.Height
    if ($sourceRatio -gt $targetRatio) {
        $sourceWidth = [int]($image.Height * $targetRatio)
        $sourceX = [int](($image.Width - $sourceWidth) / 2)
        $source = [System.Drawing.Rectangle]::new($sourceX, 0, $sourceWidth, $image.Height)
    } else {
        $sourceHeight = [int]($image.Width / $targetRatio)
        $sourceY = [int](($image.Height - $sourceHeight) / 2)
        $source = [System.Drawing.Rectangle]::new(0, $sourceY, $image.Width, $sourceHeight)
    }
    $graphics.DrawImage($image, [System.Drawing.Rectangle]::new(0, 0, 1024, 500), $source, [System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-Fit($graphics, [string]$path, [float]$x, [float]$y, [float]$width, [float]$height) {
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        $scale = [Math]::Min($width / $image.Width, $height / $image.Height)
        $w = $image.Width * $scale
        $h = $image.Height * $scale
        $graphics.DrawImage($image, $x + ($width - $w) / 2, $y + ($height - $h) / 2, $w, $h)
    } finally { $image.Dispose() }
}

function Create-Feature([string]$locale, [string]$subtitle) {
    $background = [System.Drawing.Image]::FromFile($BackgroundPath)
    $bitmap = [System.Drawing.Bitmap]::new(1024, 500, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        Draw-CroppedBackground $graphics $background

        $shade = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            [System.Drawing.Point]::new(0, 0), [System.Drawing.Point]::new(750, 0),
            [System.Drawing.Color]::FromArgb(210, 0, 4, 28), [System.Drawing.Color]::FromArgb(15, 0, 4, 28))
        $graphics.FillRectangle($shade, 0, 0, 780, 500)
        $shade.Dispose()

        Draw-Fit $graphics $titlePath 55 72 520 280
        Draw-Fit $graphics $normalPath 665 58 225 225
        Draw-Fit $graphics $quickPath 805 218 170 170
        Draw-Fit $graphics $timePath 602 292 145 145

        $font = [System.Drawing.Font]::new($fonts.Families[0], 34, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $format = [System.Drawing.StringFormat]::new()
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 64, 224, 255))
        $shadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(220, 0, 0, 15))
        $rect = [System.Drawing.RectangleF]::new(45, 355, 540, 80)
        $graphics.DrawString($subtitle, $font, $shadow, [System.Drawing.RectangleF]::new(49, 359, 540, 80), $format)
        $graphics.DrawString($subtitle, $font, $brush, $rect, $format)
        $shadow.Dispose(); $brush.Dispose(); $format.Dispose(); $font.Dispose()

        $bitmap.Save((Join-Path $outputDirectory "feature_graphic_$locale.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose(); $bitmap.Dispose(); $background.Dispose()
    }
}

Create-Feature 'en-US' 'TAP · STREAK · FEVER'
Create-Feature 'ko-KR' '터치 · 연속 기록 · 피버'
$fonts.Dispose()
