Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Filled rounded-square background (dark green)
    $r = [single]($size * 0.18)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [single]($r * 2)
    $rect = New-Object System.Drawing.RectangleF 1, 1, ($size - 2), ($size - 2)
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $bgBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45, 140, 80))
    $g.FillPath($bgBrush, $path)

    # Thin border for crispness at small sizes
    $borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(80, 0, 0, 0)), 1
    $g.DrawPath($borderPen, $path)

    # Checkmark
    $penWidth = [single][Math]::Max(2.0, $size / 8.0)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $penWidth
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $x1 = [single]($size * 0.24); $y1 = [single]($size * 0.54)
    $x2 = [single]($size * 0.44); $y2 = [single]($size * 0.72)
    $x3 = [single]($size * 0.78); $y3 = [single]($size * 0.32)
    $g.DrawLine($pen, $x1, $y1, $x2, $y2)
    $g.DrawLine($pen, $x2, $y2, $x3, $y3)

    $pen.Dispose(); $borderPen.Dispose(); $bgBrush.Dispose(); $path.Dispose(); $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,@{ Size = $s; Bytes = $ms.ToArray() }
    $bmp.Dispose(); $ms.Dispose()
}

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$pngs.Count)

$entryOffset = 6 + (16 * $pngs.Count)
$dataOffsets = @(); $curOffset = $entryOffset
foreach ($p in $pngs) { $dataOffsets += $curOffset; $curOffset += $p.Bytes.Length }

for ($i = 0; $i -lt $pngs.Count; $i++) {
    $s = $pngs[$i].Size; $bytes = $pngs[$i].Bytes
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim)
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$bytes.Length); $bw.Write([uint32]$dataOffsets[$i])
}
foreach ($p in $pngs) { $bw.Write($p.Bytes) }
$bw.Flush()

[System.IO.File]::WriteAllBytes("C:\Users\teddy\Desktop\Unblocker\Unblocker.ico", $out.ToArray())
$bw.Close()
Write-Output ("Wrote Unblocker.ico ({0} bytes, {1} sizes)" -f (Get-Item "C:\Users\teddy\Desktop\Unblocker\Unblocker.ico").Length, $pngs.Count)
