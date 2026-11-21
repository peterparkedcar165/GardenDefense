# crops every plant's icon sprite out of its texture (rects from icon_jobs.json, written by
# extract.py) and saves a 128 by 128 portrait in icons/. small pixel art is scaled up with
# nearest neighbour so it stays crisp, big painted icons are scaled down smoothly
Add-Type -AssemblyName System.Drawing
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent (Split-Path -Parent $here)
$jobs = Get-Content -Raw -Encoding UTF8 (Join-Path $here 'icon_jobs.json') | ConvertFrom-Json
$outDir = Join-Path $here 'icons'
New-Item -ItemType Directory -Force $outDir | Out-Null
$size = 128
foreach ($j in $jobs) {
    $src = [System.Drawing.Image]::FromFile((Join-Path $root $j.src))
    $dst = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($dst)
    $g.Clear([System.Drawing.Color]::Transparent)
    if ($j.w -le $size) {
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    } else {
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    }
    # keep the sprite's shape: fit the longer side, centre the shorter one
    $scale = [Math]::Min($size / $j.w, $size / $j.h)
    $w = [int][Math]::Round($j.w * $scale); $h = [int][Math]::Round($j.h * $scale)
    $dx = [int](($size - $w) / 2); $dy = [int](($size - $h) / 2)
    $destRect = New-Object System.Drawing.Rectangle $dx, $dy, $w, $h
    $srcRect = New-Object System.Drawing.Rectangle $j.x, $j.y, $j.w, $j.h
    $g.DrawImage($src, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $dst.Save((Join-Path $outDir ($j.cls + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
    $dst.Dispose(); $src.Dispose()
}
"icons: " + $jobs.Count
