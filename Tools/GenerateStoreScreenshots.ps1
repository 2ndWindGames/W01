param(
    [Parameter(Mandatory = $true)]
    [string]$BackgroundPath
)

Add-Type -AssemblyName System.Drawing

$workspace = Split-Path -Parent $PSScriptRoot
$fontPath = Join-Path $workspace 'Assets/Resources/Fonts/NotoSansKR-Variable.ttf'
$fontCollection = [System.Drawing.Text.PrivateFontCollection]::new()
$fontCollection.AddFontFile($fontPath)
$fontFamily = $fontCollection.Families[0]
$background = [System.Drawing.Image]::FromFile($BackgroundPath)
$targetDirectory = Join-Path $workspace 'Assets/Resources/UI/NeonSignalPack/Targets'
$titlePath = Join-Path $workspace 'Assets/Resources/UI/Intro/Visuals/intro_title.png'
$corePath = Join-Path $workspace 'Assets/Resources/UI/Intro/Visuals/intro_energy_core.png'
$logoPath = Join-Path $workspace 'Assets/Resources/UI/NeonSignalPack/Branding/logo_secondwindgames.png'

function New-Font([float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Bold) {
    return [System.Drawing.Font]::new($fontFamily, $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-Text($graphics, [string]$text, [float]$size, [float]$y, $color, [float]$height = 120) {
    $font = New-Font $size
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = [System.Drawing.RectangleF]::new(70, $y, 940, $height)
    $shadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190, 5, 5, 30))
    $brush = [System.Drawing.SolidBrush]::new($color)
    $graphics.DrawString($text, $font, $shadow, [System.Drawing.RectangleF]::new(76, $y + 7, 940, $height), $format)
    $graphics.DrawString($text, $font, $brush, $rect, $format)
    $brush.Dispose(); $shadow.Dispose(); $format.Dispose(); $font.Dispose()
}

function Draw-ImageFit($graphics, [string]$path, [float]$x, [float]$y, [float]$width, [float]$height) {
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        $scale = [Math]::Min($width / $image.Width, $height / $image.Height)
        $drawWidth = $image.Width * $scale
        $drawHeight = $image.Height * $scale
        $graphics.DrawImage($image, $x + ($width - $drawWidth) / 2, $y + ($height - $drawHeight) / 2, $drawWidth, $drawHeight)
    } finally { $image.Dispose() }
}

function New-Canvas {
    $bitmap = [System.Drawing.Bitmap]::new(1080, 1920, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.DrawImage($background, 0, 0, 1080, 1920)
    $shade = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(75, 0, 0, 20))
    $graphics.FillRectangle($shade, 0, 0, 1080, 1920)
    $shade.Dispose()
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Save-Canvas($canvas, [string]$path) {
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Bitmap.Dispose()
}

function Draw-Panel($graphics, [float]$x, [float]$y, [float]$width, [float]$height) {
    $fill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(225, 7, 15, 55))
    $cyan = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(230, 42, 222, 255), 5)
    $magenta = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(220, 220, 55, 255), 2)
    $graphics.FillRectangle($fill, $x, $y, $width, $height)
    $graphics.DrawRectangle($cyan, $x, $y, $width, $height)
    $graphics.DrawRectangle($magenta, $x + 12, $y + 12, $width - 24, $height - 24)
    $fill.Dispose(); $cyan.Dispose(); $magenta.Dispose()
}

function Generate-Locale([string]$locale, [hashtable]$t) {
    $outDirectory = Join-Path $workspace "StoreListing/$locale/screenshots"
    New-Item -ItemType Directory -Force -Path $outDirectory | Out-Null
    $white = [System.Drawing.Color]::White
    $cyan = [System.Drawing.Color]::FromArgb(255, 77, 235, 255)
    $pink = [System.Drawing.Color]::FromArgb(255, 255, 72, 222)
    $yellow = [System.Drawing.Color]::FromArgb(255, 255, 220, 75)

    $c = New-Canvas
    Draw-ImageFit $c.Graphics $titlePath 150 105 780 330
    Draw-Text $c.Graphics $t.IntroHeadline 58 440 $white 120
    Draw-Text $c.Graphics $t.IntroSub 34 555 $cyan 95
    Draw-ImageFit $c.Graphics $corePath 170 690 740 740
    Draw-Panel $c.Graphics 190 1515 700 150
    Draw-Text $c.Graphics $t.Start 43 1530 $white 115
    Draw-ImageFit $c.Graphics $logoPath 390 1730 300 125
    Save-Canvas $c (Join-Path $outDirectory '01_intro.png')

    $c = New-Canvas
    Draw-Text $c.Graphics $t.GameHeadline 58 90 $white 120
    Draw-Text $c.Graphics $t.GameSub 32 205 $cyan 90
    Draw-Panel $c.Graphics 85 330 910 180
    Draw-Text $c.Graphics ($t.Score + '  01250       ' + $t.Best + '  04320       ' + $t.Time + '  18.6') 26 355 $white 120
    $targets = @('target_normal.png','target_quick.png','target_time.png','target_danger.png')
    $positions = @(@(105,620),@(565,610),@(115,1110),@(565,1100))
    for($i=0;$i-lt4;$i++){ Draw-ImageFit $c.Graphics (Join-Path $targetDirectory $targets[$i]) $positions[$i][0] $positions[$i][1] 400 400 }
    Draw-Panel $c.Graphics 145 1590 790 175
    Draw-Text $c.Graphics $t.TargetGuide 35 1615 $white 120
    Save-Canvas $c (Join-Path $outDirectory '02_gameplay.png')

    $c = New-Canvas
    Draw-Text $c.Graphics $t.FeverHeadline 68 105 $pink 130
    Draw-Text $c.Graphics $t.FeverSub 34 230 $white 100
    Draw-ImageFit $c.Graphics (Join-Path $targetDirectory 'target_overload.png') 135 465 810 810
    Draw-Text $c.Graphics ($t.Streak + ' 32  ×3') 55 1335 $yellow 120
    Draw-Panel $c.Graphics 130 1515 820 190
    Draw-Text $c.Graphics $t.FeverGuide 35 1535 $white 145
    Save-Canvas $c (Join-Path $outDirectory '03_fever.png')

    $c = New-Canvas
    Draw-Text $c.Graphics $t.RankHeadline 58 100 $white 120
    Draw-Text $c.Graphics $t.RankSub 32 215 $cyan 90
    Draw-Panel $c.Graphics 90 375 900 1130
    $rows = @(
        @('01','NOVA','1,258'), @('02','ORION','982'), @('03','LUMI','876'),
        @('04','NONAME','653'), @('05','ECHO','498'), @('06','VIOLET','441')
    )
    for($i=0;$i-lt$rows.Count;$i++){
        $y=500+$i*145
        if($i-eq3){$highlight=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190,105,22,150));$c.Graphics.FillRectangle($highlight,125,$y-12,830,115);$highlight.Dispose()}
        Draw-Text $c.Graphics ($rows[$i][0] + '       ' + $rows[$i][1] + '       ' + $rows[$i][2]) 31 $y $([System.Drawing.Color]::White) 90
    }
    Draw-Text $c.Graphics $t.YouMarker 32 1435 $pink 75
    Draw-ImageFit $c.Graphics $logoPath 315 1650 450 170
    Save-Canvas $c (Join-Path $outDirectory '04_ranking.png')
}

Generate-Locale 'en-US' @{
    IntroHeadline='A NEON REFLEX CHALLENGE'; IntroSub='Tap fast. Build your streak.'; Start='TOUCH TO START';
    GameHeadline='READ THE SIGNAL. TAP FAST.'; GameSub='Four targets. One split-second decision.';
    Score='SCORE'; Best='BEST'; Time='TIME'; TargetGuide='Tap bonuses. Avoid danger.';
    FeverHeadline='UNLEASH FEVER MODE'; FeverSub='Keep your streak alive and multiply the score.';
    Streak='STREAK'; FeverGuide='Faster targets. Bigger rewards.';
    RankHeadline='CLIMB THE GLOBAL RANKING'; RankSub='Set a record and make your mark.'; YouMarker='★ YOUR BEST SCORE'
}

Generate-Locale 'ko-KR' @{
    IntroHeadline='네온 반응 속도 챌린지'; IntroSub='빠르게 터치하고 연속 기록을 이어가세요.'; Start='터치해서 시작';
    GameHeadline='신호를 읽고 빠르게 터치!'; GameSub='네 가지 타겟, 순간의 선택이 점수를 결정합니다.';
    Score='점수'; Best='최고'; Time='시간'; TargetGuide='보너스를 터치하고 위험 타겟을 피하세요.';
    FeverHeadline='피버 모드 발동'; FeverSub='연속 터치를 유지해 점수 배율을 높이세요.';
    Streak='연속 터치'; FeverGuide='더 빠른 타겟, 더 높은 점수!';
    RankHeadline='글로벌 랭킹에 도전하세요'; RankSub='최고 기록을 세우고 이름을 남기세요.'; YouMarker='★ 나의 최고 기록'
}

$background.Dispose()
$fontCollection.Dispose()
