Add-Type -AssemblyName System.Drawing
$out = $PSScriptRoot
$W = 800; $H = 600

function New-Canvas($bg) {
	$bmp = New-Object System.Drawing.Bitmap $W, $H
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.SmoothingMode = 'AntiAlias'
	$g.Clear([System.Drawing.ColorTranslator]::FromHtml($bg))
	return @{ Bmp = $bmp; G = $g }
}
function Br($c) { New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($c)) }
function Pn($c, $w = 3) { New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($c)), $w }
function Txt($g, $s, $x, $y, $size = 28, $c = '#ffffff') {
	$f = New-Object System.Drawing.Font 'Arial', $size, ([System.Drawing.FontStyle]::Bold)
	$g.DrawString($s, $f, (Br $c), [single]$x, [single]$y)
}
function Save($cv, $name) {
	$cv.Bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
	$cv.G.Dispose(); $cv.Bmp.Dispose()
}
function Gradient($g, $c1, $c2) {
	$r = New-Object System.Drawing.Rectangle 0, 0, $W, $H
	$b = New-Object System.Drawing.Drawing2D.LinearGradientBrush $r, ([System.Drawing.ColorTranslator]::FromHtml($c1)), ([System.Drawing.ColorTranslator]::FromHtml($c2)), 90
	$g.FillRectangle($b, $r)
}
function Pt($x, $y) { New-Object System.Drawing.Point ([int]$x), ([int]$y) }
function Car($g, $x, $y, $color, $sporty = $false) {
	$top = if ($sporty) { 60 } else { 90 }
	$g.FillPolygon((Br $color), @((Pt ($x + 40) ($y + 100)), (Pt ($x + 120) ($y + 100 - $top)), (Pt ($x + 300) ($y + 100 - $top)), (Pt ($x + 380) ($y + 100))))
	$g.FillRectangle((Br $color), $x, ($y + 100), 440, 50)
	$g.FillPolygon((Br '#cfe8ff'), @((Pt ($x + 100) ($y + 95)), (Pt ($x + 140) ($y + 110 - $top)), (Pt ($x + 280) ($y + 110 - $top)), (Pt ($x + 330) ($y + 95))))
	foreach ($wx in 80, 300) {
		$g.FillEllipse((Br '#222222'), $wx + $x, $y + 120, 70, 70)
		$g.FillEllipse((Br '#aaaaaa'), $wx + $x + 18, $y + 138, 34, 34)
	}
	$g.FillRectangle((Br '#ffee88'), $x + 425, $y + 110, 18, 12)
}
function Person($g, $cx, $cy, $hair, $cloth, $s = 1.0) {
	$g.FillEllipse((Br '#ffdcc0'), [single]($cx - 40 * $s), [single]($cy - 120 * $s), [single](80 * $s), [single](90 * $s))
	$g.FillPie((Br $hair), [single]($cx - 48 * $s), [single]($cy - 132 * $s), [single](96 * $s), [single](90 * $s), 180, 180)
	$g.FillRectangle((Br $hair), [single]($cx - 48 * $s), [single]($cy - 90 * $s), [single](12 * $s), [single](70 * $s))
	$g.FillRectangle((Br $hair), [single]($cx + 36 * $s), [single]($cy - 90 * $s), [single](12 * $s), [single](70 * $s))
	$g.FillEllipse((Br '#222222'), [single]($cx - 22 * $s), [single]($cy - 80 * $s), [single](10 * $s), [single](14 * $s))
	$g.FillEllipse((Br '#222222'), [single]($cx + 12 * $s), [single]($cy - 80 * $s), [single](10 * $s), [single](14 * $s))
	$g.DrawArc((Pn '#cc4444' 2), [single]($cx - 12 * $s), [single]($cy - 62 * $s), [single](24 * $s), [single](14 * $s), 0, 180)
	$g.FillRectangle((Br $cloth), [single]($cx - 50 * $s), [single]($cy - 30 * $s), [single](100 * $s), [single](160 * $s))
}
function Panel($g, $x, $y, $w, $h) {
	$g.FillRectangle((Br '#ffffff'), $x, $y, $w, $h)
	$g.DrawRectangle((Pn '#000000' 4), $x, $y, $w, $h)
}
function Bubble($g, $x, $y, $w, $h, $s) {
	$g.FillEllipse((Br '#ffffff'), $x, $y, $w, $h)
	$g.DrawEllipse((Pn '#000000' 2), $x, $y, $w, $h)
	Txt $g $s ($x + 15) ($y + $h / 3) 14 '#000000'
}

$c = New-Canvas '#fff4e0'; $g = $c.G
Gradient $g '#ffd6e8' '#bfe3ff'
$g.FillEllipse((Br '#ffffaa'), 560, 60, 120, 120)
Person $g 400 300 '#5a3a8a' '#ff7aa8' 2.0
Txt $g 'Illustration' 20 20 28 '#7a4a8a'
Save $c 'illust_01.png'

foreach ($n in 1, 2) {
	$c = New-Canvas '#ffffff'; $g = $c.G
	Panel $g 30 30 360 260; Panel $g 410 30 360 260
	Panel $g 30 310 740 260
	Person $g 210 250 '#222222' '#dddddd' 1.1
	Bubble $g 250 45 130 60 'Hello!'
	Person $g 590 250 '#442200' '#aaaaaa' 1.1
	Bubble $g 420 45 130 60 'Eh?!'
	for ($i = 0; $i -lt 40; $i++) { $g.DrawLine((Pn '#000000' 1), 400, 440, ((Get-Random -Max 740) + 30), (Get-Random -Min 320 -Max 560)) }
	Txt $g "MANGA p.$n" 300 520 22 '#000000'
	Save $c "manga_0$n.png"
}

$c = New-Canvas '#2b3a55'; $g = $c.G
$g.FillRectangle((Br '#f5ecd5'), 200, 60, 400, 480)
$g.DrawRectangle((Pn '#6b4f2a' 6), 200, 60, 400, 480)
$g.FillEllipse((Br '#2b3a55'), 330, 120, 140, 140)
$g.FillEllipse((Br '#f5ecd5'), 360, 110, 130, 130)
Txt $g 'NOVEL' 300 330 40 '#6b4f2a'
Txt $g 'Moonlit Story' 270 400 22 '#6b4f2a'
Save $c 'novel_01.png'

$c = New-Canvas '#87ceeb'; $g = $c.G
Gradient $g '#6ab7ff' '#fff0f5'
$g.FillRectangle((Br '#6aa84f'), 0, 450, $W, 150)
$g.FillEllipse((Br '#ffffff'), 100, 80, 200, 70); $g.FillEllipse((Br '#ffffff'), 500, 120, 220, 70)
Person $g 400 330 '#2255aa' '#ffffff' 1.8
Txt $g 'ANIME  EP.1' 20 540 24 '#ffffff'
Save $c 'anime_01.png'

$c = New-Canvas '#101030'; $g = $c.G
for ($i = 0; $i -lt 60; $i++) { $g.FillRectangle((Br '#ffffff'), (Get-Random -Max 800), (Get-Random -Max 350), 3, 3) }
for ($x = 0; $x -lt $W; $x += 50) { $g.FillRectangle((Br '#8b5a2b'), $x, 500, 48, 48); $g.FillRectangle((Br '#3cb043'), $x, 500, 48, 12) }
$g.FillRectangle((Br '#ff3333'), 150, 440, 40, 40); $g.FillRectangle((Br '#ffcc99'), 155, 420, 30, 20)
$g.FillRectangle((Br '#ffd700'), 450, 360, 40, 40); $g.FillRectangle((Br '#ffd700'), 540, 300, 40, 40)
$g.FillRectangle((Br '#aa33aa'), 600, 450, 50, 50)
Txt $g 'SCORE 001200' 20 15 22 '#ffffff'
Save $c 'game_01.png'

$c = New-Canvas '#dddddd'; $g = $c.G
Gradient $g '#b0b0c0' '#e8e8f0'
$g.FillRectangle((Br '#555566'), 0, 480, $W, 120)
Person $g 400 330 '#ff66cc' '#3355aa' 2.0
$g.FillPolygon((Br '#ffd700'), @((Pt 400 70), (Pt 420 120), (Pt 380 120)))
Txt $g 'COSPLAY PHOTO' 20 20 22 '#333333'
Save $c 'cosplay_01.png'

$c = New-Canvas '#1a0033'; $g = $c.G
Gradient $g '#1a0033' '#ff4488'
for ($i = 0; $i -lt 24; $i++) { $h = Get-Random -Min 40 -Max 300; $g.FillRectangle((Br '#00ffcc'), (40 + $i * 31), (520 - $h), 22, $h) }
Txt $g 'MUSIC' 320 60 48 '#ffffff'
$g.FillEllipse((Br '#ffffff'), 340, 220, 60, 45); $g.FillRectangle((Br '#ffffff'), 390, 120, 8, 120)
Save $c 'music_01.png'

$c = New-Canvas '#220033'; $g = $c.G
foreach ($lx in 100, 300, 500, 700) { $g.FillPolygon((Br '#ffee88'), @((Pt $lx 0), (Pt ($lx - 120) 520), (Pt ($lx + 120) 520))) }
$g.FillRectangle((Br '#553366'), 0, 500, $W, 100)
Person $g 250 350 '#ff99cc' '#ff55aa' 1.6
Person $g 550 350 '#66ccff' '#33aaff' 1.6
Txt $g 'IDOL LIVE' 270 20 36 '#ffffff'
Save $c 'idol_01.png'

$cars = @(
	@('toyota', '#cc1111', 'TOYOTA', $false), @('nissan', '#222266', 'NISSAN', $true), @('honda', '#eeeeee', 'HONDA', $false),
	@('mazda', '#aa0022', 'MAZDA', $true), @('subaru', '#1144cc', 'SUBARU', $true), @('suzuki', '#ffcc00', 'SUZUKI', $false),
	@('daihatsu', '#33aa66', 'DAIHATSU', $false), @('mercedes', '#bbbbbb', 'MERCEDES', $true), @('porsche', '#ff8800', 'PORSCHE', $true)
)
foreach ($car in $cars) {
	$c = New-Canvas '#cfe8ff'; $g = $c.G
	Gradient $g '#9fd0ff' '#eef6ff'
	$g.FillRectangle((Br '#666666'), 0, 420, $W, 180)
	for ($x = 0; $x -lt $W; $x += 100) { $g.FillRectangle((Br '#ffffff'), $x, 500, 50, 8) }
	Car $g 180 250 $car[1] $car[3]
	Txt $g $car[2] 20 20 30 '#223355'
	Save $c ("car_" + $car[0] + ".png")
}

$c = New-Canvas '#0d1117'; $g = $c.G
$g.FillRectangle((Br '#161b22'), 60, 60, 680, 420); $g.DrawRectangle((Pn '#30363d' 3), 60, 60, 680, 420)
$cols = '#ff7b72', '#79c0ff', '#a5d6ff', '#7ee787'
for ($i = 0; $i -lt 12; $i++) { $g.FillRectangle((Br $cols[$i % 4]), (90 + ($i % 3) * 30), (90 + $i * 32), (Get-Random -Min 100 -Max 400), 14) }
Txt $g 'IT  { code }' 300 500 24 '#79c0ff'
Save $c 'it_01.png'

$c = New-Canvas '#dfe6ee'; $g = $c.G
foreach ($i in 0..7) { $h = Get-Random -Min 150 -Max 420; $g.FillRectangle((Br '#7a8aa0'), (20 + $i * 95), (520 - $h), 80, $h) }
$g.FillRectangle((Br '#444444'), 0, 520, $W, 80)
Person $g 300 520 '#222222' '#336699' 0.9
Person $g 450 520 '#552200' '#993333' 0.9
Txt $g 'SOCIETY' 20 20 28 '#223355'
Save $c 'society_01.png'

$c = New-Canvas '#ffffff'; $g = $c.G
Gradient $g '#ffe08a' '#ff8aa8'
for ($i = 0; $i -lt 30; $i++) { $g.FillEllipse((Br ('#{0:x6}' -f (Get-Random -Max 16777215))), (Get-Random -Max 750), (Get-Random -Max 550), 60, 60) }
Txt $g 'ANYTHING' 260 250 44 '#ffffff'
Save $c 'anything_01.png'
