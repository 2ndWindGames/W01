param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @(
    [System.Drawing.Bitmap].Assembly.Location,
    [System.Drawing.Rectangle].Assembly.Location
) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

public static class NeonButtonNormalizer
{
    private static Rectangle VisibleBounds(Bitmap image, byte threshold)
    {
        int minX = image.Width, minY = image.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
        {
            if (image.GetPixel(x, y).A <= threshold) continue;
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
        return maxX < minX ? Rectangle.Empty : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    public static void Normalize(string[] paths, int padding)
    {
        var bounds = new Rectangle[paths.Length];
        int contentWidth = 0, contentHeight = 0;
        for (int i = 0; i < paths.Length; i++)
        using (var image = new Bitmap(paths[i]))
        {
            bounds[i] = VisibleBounds(image, 8);
            contentWidth = Math.Max(contentWidth, bounds[i].Width);
            contentHeight = Math.Max(contentHeight, bounds[i].Height);
        }

        int width = contentWidth + padding * 2;
        int height = contentHeight + padding * 2;
        for (int i = 0; i < paths.Length; i++)
        using (var input = new Bitmap(paths[i]))
        using (var output = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(output))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            int x = (width - bounds[i].Width) / 2;
            int y = (height - bounds[i].Height) / 2;
            graphics.DrawImage(input, new Rectangle(x, y, bounds[i].Width, bounds[i].Height), bounds[i], GraphicsUnit.Pixel);
            output.Save(paths[i] + ".normalized.png", ImageFormat.Png);
        }

        for (int i = 0; i < paths.Length; i++)
        {
            System.IO.File.Delete(paths[i]);
            System.IO.File.Move(paths[i] + ".normalized.png", paths[i]);
        }
    }

    public static void AdjustState(string path, float brightness, float saturation, float alphaMultiplier)
    {
        using (var input = new Bitmap(path))
        using (var output = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < input.Height; y++)
            for (int x = 0; x < input.Width; x++)
            {
                Color c = input.GetPixel(x, y);
                float gray = (c.R + c.G + c.B) / 3f;
                int r = Math.Min(255, Math.Max(0, (int)((gray + (c.R - gray) * saturation) * brightness)));
                int g = Math.Min(255, Math.Max(0, (int)((gray + (c.G - gray) * saturation) * brightness)));
                int b = Math.Min(255, Math.Max(0, (int)((gray + (c.B - gray) * saturation) * brightness)));
                int a = Math.Min(255, Math.Max(0, (int)(c.A * alphaMultiplier)));
                output.SetPixel(x, y, Color.FromArgb(a, r, g, b));
            }
            output.Save(path + ".state.png", ImageFormat.Png);
        }
        System.IO.File.Delete(path);
        System.IO.File.Move(path + ".state.png", path);
    }

    public static void Resize(string path, int width, int height)
    {
        using (var input = new Bitmap(path))
        using (var output = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(output))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(input, new Rectangle(0, 0, width, height));
            output.Save(path + ".resized.png", ImageFormat.Png);
        }
        System.IO.File.Delete(path);
        System.IO.File.Move(path + ".resized.png", path);
    }
}
'@

$buttonRoot = Join-Path $ProjectRoot 'Assets\Resources\UI\NeonSignalPack\NineSlice\Buttons'
$iconSource = Join-Path $ProjectRoot 'Assets\ArtSource\NeonSignal\icon_button_closed.png'
$iconNormal = Join-Path $buttonRoot 'icon_button_normal.png'
$iconPressed = Join-Path $buttonRoot 'icon_button_pressed.png'
$iconDisabled = Join-Path $buttonRoot 'icon_button_disabled.png'
if (Test-Path -LiteralPath $iconSource) {
    Copy-Item -LiteralPath $iconSource -Destination $iconNormal -Force
    Copy-Item -LiteralPath $iconSource -Destination $iconPressed -Force
    Copy-Item -LiteralPath $iconSource -Destination $iconDisabled -Force
}
[NeonButtonNormalizer]::Normalize(@(
    (Join-Path $buttonRoot 'button_primary_normal.png'),
    (Join-Path $buttonRoot 'button_primary_pressed.png'),
    (Join-Path $buttonRoot 'button_primary_disabled.png')
), 8)
[NeonButtonNormalizer]::Normalize(@((Join-Path $buttonRoot 'button_compact.png')), 8)
[NeonButtonNormalizer]::Normalize(@(
    $iconNormal,
    $iconPressed,
    $iconDisabled
), 8)
[NeonButtonNormalizer]::AdjustState($iconPressed, 1.18, 1.08, 1.0)
[NeonButtonNormalizer]::AdjustState($iconDisabled, 0.58, 0.35, 0.72)

$titlePath = Join-Path $ProjectRoot 'Assets\Resources\UI\NeonSignalPack\Branding\title_neon_touch.png'
[NeonButtonNormalizer]::Normalize(@($titlePath), 8)

$hudSource = Join-Path $ProjectRoot 'Assets\ArtSource\NeonSignal\hud_card_clean.png'
$hudPath = Join-Path $ProjectRoot 'Assets\Resources\UI\NeonSignalPack\NineSlice\Panels\hud_card.png'
if (Test-Path -LiteralPath $hudSource) {
    Copy-Item -LiteralPath $hudSource -Destination $hudPath -Force
    [NeonButtonNormalizer]::Normalize(@($hudPath), 10)
    [NeonButtonNormalizer]::Resize($hudPath, 320, 200)
}

Write-Output 'Normalized Neon Signal buttons, title, and HUD card.'
