$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sourceRoot = 'C:/Users/rlatm/.codex/generated_images/01a04851-0e1c-7553-83f1-9dd881d3f3f8'
$outputRoot = Join-Path $PSScriptRoot '../Assets/Resources/UI/NeonSignalPack'

$folders = @(
    'Backgrounds', 'NineSlice/Panels', 'NineSlice/Buttons', 'NineSlice/Lists',
    'Controls', 'Icons', 'Targets', 'VFX', 'Progress', 'Decorations', 'Branding', 'Concepts', 'Sources'
)
foreach ($folder in $folders) {
    New-Item -ItemType Directory -Path (Join-Path $outputRoot $folder) -Force | Out-Null
}

Add-Type -ReferencedAssemblies @(
    [System.Drawing.Bitmap].Assembly.Location,
    [System.Drawing.Rectangle].Assembly.Location
) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class NeonSignalImageTools
{
    public static void Extract(string source, string destination, int columns, int rows, int index, bool chroma, bool checker)
    {
        using (var input = new Bitmap(source))
        {
            int cellWidth = input.Width / columns;
            int cellHeight = input.Height / rows;
            int column = index % columns;
            int row = index / columns;
            var sourceRect = new Rectangle(column * cellWidth, row * cellHeight, cellWidth, cellHeight);
            using (var tile = new Bitmap(cellWidth, cellHeight, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(tile))
                    graphics.DrawImage(input, new Rectangle(0, 0, cellWidth, cellHeight), sourceRect, GraphicsUnit.Pixel);
                for (int y = 0; y < tile.Height; y++)
                for (int x = 0; x < tile.Width; x++)
                {
                    Color c = tile.GetPixel(x, y);
                    int alpha = 255;
                    int red = c.R, green = c.G, blue = c.B;

                    if (chroma)
                    {
                        int dominance = green - Math.Max(red, blue);
                        if (dominance >= 155) alpha = 0;
                        else if (dominance > 45) alpha = (int)(255f * (155 - dominance) / 110f);
                        if (alpha < 250) green = Math.Min(green, Math.Max(red, blue) + 12);
                    }
                    else if (checker)
                    {
                        int max = Math.Max(red, Math.Max(green, blue));
                        int min = Math.Min(red, Math.Min(green, blue));
                        int brightness = (red + green + blue) / 3;
                        if (max - min < 22 && brightness >= 82 && brightness <= 242) alpha = 0;
                    }

                    tile.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                }
                tile.Save(destination, ImageFormat.Png);
            }
        }
    }

    public static void ExtractRect(string source, string destination, int x, int y, int width, int height, bool chroma, bool checker)
    {
        using (var input = new Bitmap(source))
        using (var tile = input.Clone(new Rectangle(x, y, width, height), PixelFormat.Format32bppArgb))
        {
            for (int py = 0; py < tile.Height; py++)
            for (int px = 0; px < tile.Width; px++)
            {
                Color c = tile.GetPixel(px, py);
                int red = c.R, green = c.G, blue = c.B, alpha = 255;
                if (chroma)
                {
                    int dominance = green - Math.Max(red, blue);
                    if (dominance >= 155) alpha = 0;
                    else if (dominance > 45) alpha = (int)(255f * (155 - dominance) / 110f);
                    if (alpha < 250) green = Math.Min(green, Math.Max(red, blue) + 12);
                }
                else if (checker)
                {
                    int max = Math.Max(red, Math.Max(green, blue));
                    int min = Math.Min(red, Math.Min(green, blue));
                    int brightness = (red + green + blue) / 3;
                    if (max - min < 26 && brightness >= 78 && brightness <= 246) alpha = 0;
                }
                tile.SetPixel(px, py, Color.FromArgb(alpha, red, green, blue));
            }
            tile.Save(destination, ImageFormat.Png);
        }
    }

    public static void Trim(string path, int padding)
    {
        using (var input = new Bitmap(path))
        {
            int minX = input.Width, minY = input.Height, maxX = -1, maxY = -1;
            for (int y = 0; y < input.Height; y++)
            for (int x = 0; x < input.Width; x++)
            {
                if (input.GetPixel(x, y).A <= 12) continue;
                minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
            if (maxX < minX || maxY < minY) return;
            minX = Math.Max(0, minX - padding); minY = Math.Max(0, minY - padding);
            maxX = Math.Min(input.Width - 1, maxX + padding); maxY = Math.Min(input.Height - 1, maxY + padding);
            using (var trimmed = input.Clone(new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1), PixelFormat.Format32bppArgb))
                trimmed.Save(path + ".trim.png", ImageFormat.Png);
        }
        System.IO.File.Delete(path);
        System.IO.File.Move(path + ".trim.png", path);
    }
}
'@

function Split-Atlas {
    param(
        [string]$Source,
        [int]$Columns,
        [int]$Rows,
        [string[]]$Names,
        [string[]]$Folders,
        [ValidateSet('Chroma','Checker')] [string]$Mode
    )
    for ($index = 0; $index -lt $Names.Count; $index++) {
        $destination = Join-Path $outputRoot (Join-Path $Folders[$index] ($Names[$index] + '.png'))
        [NeonSignalImageTools]::Extract($Source, $destination, $Columns, $Rows, $index, $Mode -eq 'Chroma', $Mode -eq 'Checker')
    }
}

$frameAtlas = Join-Path $sourceRoot 'exec-5a5218b0-0ad2-4db9-a794-24b846fc1012.png'
$controlAtlas = Join-Path $sourceRoot 'exec-3c422380-85eb-4312-ab6c-6378492408da.png'
$iconAtlas = Join-Path $sourceRoot 'exec-792a9ebe-9b7d-4353-8dbf-83c03f7977c4.png'
$targetAtlas = Join-Path $sourceRoot 'exec-a3fd9ddf-8757-4b6c-b5ce-4e8ab94bc35c.png'
$vfxAtlas = Join-Path $sourceRoot 'exec-c09b5aba-94b5-4f6a-85db-3d582a3de9d4.png'
$decorAtlas = Join-Path $sourceRoot 'exec-ad327052-c61e-429f-86b8-ff1f38dbf07e.png'
$progressAtlas = Join-Path $sourceRoot 'exec-8cd15bd9-3b41-46a8-9008-cee8c258d97f.png'

$frameSpecs = @(
    @('NineSlice/Panels/panel_popup.png',0,90,480,320),
    @('NineSlice/Panels/panel_medium.png',480,105,300,285),
    @('NineSlice/Panels/hud_card.png',770,110,290,275),
    @('NineSlice/Panels/header_strip.png',1020,165,428,170),
    @('NineSlice/Buttons/button_primary_normal.png',10,455,380,220),
    @('NineSlice/Buttons/button_primary_pressed.png',380,455,390,220),
    @('NineSlice/Buttons/button_primary_disabled.png',745,455,390,220),
    @('NineSlice/Buttons/button_compact.png',1110,455,338,220),
    @('NineSlice/Lists/list_row_normal.png',0,760,390,190),
    @('NineSlice/Lists/list_row_selected.png',365,760,400,190),
    @('NineSlice/Lists/list_row_top.png',735,760,410,190),
    @('Decorations/divider_diamond.png',1090,770,358,170)
)
foreach ($spec in $frameSpecs) {
    $destination = Join-Path $outputRoot $spec[0]
    Write-Output "Extracting $($spec[0])"
    [NeonSignalImageTools]::ExtractRect($frameAtlas, $destination, $spec[1], $spec[2], $spec[3], $spec[4], $false, $true)
}

Split-Atlas $controlAtlas 4 3 @(
    'checkbox_empty','checkbox_checked','toggle_off','toggle_on',
    'slider_track_empty','slider_track_fill','slider_knob','scrollbar_handle',
    'icon_button_normal','icon_button_pressed','icon_button_disabled','warning_badge_frame'
) @(
    'Controls','Controls','Controls','Controls',
    'Controls','Controls','Controls','Controls',
    'NineSlice/Buttons','NineSlice/Buttons','NineSlice/Buttons','Decorations'
) 'Chroma'

Split-Atlas $iconAtlas 4 4 @(
    'icon_back','icon_close','icon_settings','icon_ranking',
    'icon_sound_on','icon_sound_off','icon_remove_ads','icon_restore',
    'icon_privacy','icon_language','icon_vibration','icon_home',
    'icon_play','icon_retry','icon_info','icon_check'
) (,('Icons') * 16) 'Chroma'

Split-Atlas $targetAtlas 4 2 @(
    'target_normal','target_quick','target_time','target_precision',
    'target_moving','target_danger','target_overload','target_safe'
) (,('Targets') * 8) 'Chroma'

Split-Atlas $vfxAtlas 4 2 @(
    'vfx_hit','vfx_perfect','vfx_miss','vfx_combo',
    'vfx_time_bonus','vfx_fever','vfx_bomb','vfx_tap_ripple'
) (,('VFX') * 8) 'Chroma'

Split-Atlas $decorAtlas 4 2 @(
    'intro_energy_core','intro_orbit_ring','intro_platform_glow','tutorial_tap',
    'header_ornament','divider_long','fever_edge_flare','grade_badge_empty'
) (,('Decorations') * 8) 'Chroma'

$progressSpecs = @(
    @('energy_empty',10,85,305,155),@('energy_half',320,85,305,155),
    @('energy_full',625,85,305,155),@('energy_fever',935,85,309,155),
    @('combo_normal',25,285,280,290),@('combo_x2',330,285,285,290),
    @('combo_x3',640,285,285,290),@('combo_fever',950,285,290,290),
    @('grade_d',20,590,285,285),@('grade_c',330,590,285,285),
    @('grade_b',640,590,285,285),@('grade_a',950,590,290,285),
    @('grade_s',5,875,310,330),@('grade_s_plus',315,875,315,330),
    @('rank_crown_gold',625,875,315,330),@('rank_crown_silver',935,875,309,330)
)
foreach ($spec in $progressSpecs) {
    $destination = Join-Path $outputRoot ("Progress/" + $spec[0] + ".png")
    [NeonSignalImageTools]::ExtractRect($progressAtlas, $destination, $spec[1], $spec[2], $spec[3], $spec[4], $true, $false)
}

Get-ChildItem (Join-Path $outputRoot 'Controls') -Filter '*.png' | ForEach-Object {
    [NeonSignalImageTools]::Trim($_.FullName, 8)
}
Get-ChildItem (Join-Path $outputRoot 'Decorations') -Filter '*.png' | ForEach-Object {
    [NeonSignalImageTools]::Trim($_.FullName, 8)
}

Copy-Item (Join-Path $sourceRoot 'exec-29fdcfc4-3511-4171-aa9d-24f5342e3d9a.png') (Join-Path $outputRoot 'Backgrounds/background_intro.png') -Force
Copy-Item (Join-Path $sourceRoot 'exec-6dda6313-90a4-4283-8daa-076f29736a84.png') (Join-Path $outputRoot 'Backgrounds/background_game.png') -Force

Copy-Item (Join-Path $PSScriptRoot '../Assets/Resources/UI/Intro/title.png') (Join-Path $outputRoot 'Branding/title_neon_touch.png') -Force
Copy-Item (Join-Path $PSScriptRoot '../Assets/Resources/SecondWindGamesLogo.png') (Join-Path $outputRoot 'Branding/logo_secondwindgames.png') -Force

$conceptSource = Join-Path $PSScriptRoot '../Assets/Resources/References/NeonSignalConcept'
Copy-Item (Join-Path $conceptSource '*') (Join-Path $outputRoot 'Concepts') -Recurse -Force

Copy-Item $frameAtlas (Join-Path $outputRoot 'Sources/source_frames.png') -Force
Copy-Item $controlAtlas (Join-Path $outputRoot 'Sources/source_controls.png') -Force
Copy-Item $iconAtlas (Join-Path $outputRoot 'Sources/source_icons.png') -Force
Copy-Item $targetAtlas (Join-Path $outputRoot 'Sources/source_targets.png') -Force
Copy-Item $vfxAtlas (Join-Path $outputRoot 'Sources/source_vfx.png') -Force
Copy-Item $decorAtlas (Join-Path $outputRoot 'Sources/source_decorations.png') -Force
Copy-Item $progressAtlas (Join-Path $outputRoot 'Sources/source_progress.png') -Force

Write-Output "Neon Signal assets generated at $outputRoot"
