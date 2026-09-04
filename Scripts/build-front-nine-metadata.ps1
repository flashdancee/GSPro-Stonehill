param(
    [Parameter(Mandatory = $true)][string]$ScenePath,
    [Parameter(Mandatory = $true)][string]$TemplateCourseDirectory,
    [Parameter(Mandatory = $true)][string]$BundlePath,
    [Parameter(Mandatory = $true)][string]$TopImagePath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$ReleaseManifestPath = (Join-Path $PSScriptRoot '..\CourseRelease.json')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReleaseManifestPath)) {
    throw "Release manifest not found: $ReleaseManifestPath"
}

$release = Get-Content -Raw -LiteralPath $ReleaseManifestPath | ConvertFrom-Json
foreach ($propertyName in @('courseFolder', 'courseName', 'version', 'updated')) {
    if ([string]::IsNullOrWhiteSpace([string]$release.$propertyName)) {
        throw "Release manifest is missing '$propertyName': $ReleaseManifestPath"
    }
}

$releaseUpdated = [DateTimeOffset]::Parse(
    [string]$release.updated,
    [Globalization.CultureInfo]::InvariantCulture,
    [Globalization.DateTimeStyles]::RoundtripKind)
$courseFolder = [string]$release.courseFolder
$courseName = [string]$release.courseName
$courseVersion = [string]$release.version

function Get-MarkerPosition {
    param([string]$SceneText, [string]$Name)
    $escaped = [regex]::Escape($Name)
    $pattern = "(?s)m_Name: $escaped\r?\n.*?m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}"
    $match = [regex]::Match($SceneText, $pattern)
    if (-not $match.Success) { throw "Marker not found in scene: $Name" }
    return [ordered]@{
        x = [double]::Parse($match.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
        y = [double]::Parse($match.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture)
        z = [double]::Parse($match.Groups[3].Value, [Globalization.CultureInfo]::InvariantCulture)
    }
}

function New-Position {
    param([double]$X, [double]$Y, [double]$Z)
    return [ordered]@{ x = $X; y = $Y; z = $Z }
}

function New-TeeRecord {
    param([string]$Type, [bool]$Enabled, [double]$Distance, $Position)
    return [ordered]@{ TeeType = $Type; Enabled = $Enabled; Distance = $Distance; Position = $Position }
}

function New-Hazard {
    param([object[]]$Points)
    return [ordered]@{
        pointCount = $Points.Count
        coords = $Points
        DZpos = New-Position 0 0 0
        freeDrop = $false
        innerOOB = $false
        noAIL = $false
        hasDZ = $false
        islandGreen = $false
    }
}

function Convert-ImagePolygonToHazard {
    param([double[]]$Values, [double]$Height)
    $points = @()
    for ($i = 0; $i -lt $Values.Length; $i += 2) {
        $points += ,(New-Position $Values[$i] $Height (2048.0 - $Values[$i + 1]))
    }
    return New-Hazard $points
}

$scene = Get-Content -Raw -LiteralPath $ScenePath
$templateGkdPath = Join-Path $TemplateCourseDirectory 'Stonehill_2H.GKD'
$templateDetailsPath = Join-Path $TemplateCourseDirectory 'coursedetails.txt'
$template = Get-Content -Raw -LiteralPath $templateGkdPath | ConvertFrom-Json

$pars = @(5, 4, 4, 4, 3, 3, 4, 3, 4)
$indexes = @(1, 3, 15, 7, 11, 9, 13, 17, 5)
$whiteYards = @(471, 344, 287, 318, 156, 151, 290, 161, 310)
$redYards = @(446, 308, 271, 293, 144, 132, 227, 128, 300)
$pinDays = @('Thursday', 'Friday', 'Saturday', 'Sunday')

$aims = @(
    [ordered]@{ A1 = @(625, 1374); A2 = @(485, 1362) },
    [ordered]@{ A1 = @(610, 1273); A2 = $null },
    [ordered]@{ A1 = @(535, 1198); A2 = $null },
    [ordered]@{ A1 = @(545, 1163); A2 = $null },
    [ordered]@{ A1 = @(784, 1190); A2 = $null },
    [ordered]@{ A1 = @(744, 1072); A2 = $null },
    [ordered]@{ A1 = @(610, 997); A2 = $null },
    [ordered]@{ A1 = @(820, 1010); A2 = $null },
    [ordered]@{ A1 = @(960, 1159); A2 = @(1015, 1233) }
)

$holes = @()
for ($hole = 1; $hole -le 18; $hole++) {
    if ($hole -gt 9) {
        $holes += ,[ordered]@{ Enabled = $false; HoleNumber = $hole; Par = 4; Index = 0; Tees = @(); Pins = @() }
        continue
    }

    $suffix = $hole.ToString('00')
    $white = Get-MarkerPosition $scene "GK_H${suffix}_Tee_White_$($whiteYards[$hole - 1])yd"
    $red = Get-MarkerPosition $scene "GK_H${suffix}_Tee_Red_$($redYards[$hole - 1])yd"
    $pins = @()
    foreach ($day in $pinDays) {
        $pins += ,[ordered]@{ Day = $day; Position = Get-MarkerPosition $scene "GK_H${suffix}_Pin_$day" }
    }
    $center = New-Position `
        ((($pins | ForEach-Object { $_.Position.x } | Measure-Object -Average).Average) - 0.1) `
        (($pins | ForEach-Object { $_.Position.y } | Measure-Object -Average).Average) `
        ((($pins | ForEach-Object { $_.Position.z } | Measure-Object -Average).Average) + 0.3)

    $tees = @(
        (New-TeeRecord 'Black' $true 0 $null),
        (New-TeeRecord 'White' $true ($whiteYards[$hole - 1] * 0.9144) $white),
        (New-TeeRecord 'Green' $true 0 $null),
        (New-TeeRecord 'Blue' $true 0 $null),
        (New-TeeRecord 'Yellow' $true 0 $null),
        (New-TeeRecord 'Red' $true ($redYards[$hole - 1] * 0.9144) $red),
        (New-TeeRecord 'Junior' $true 0 $null),
        (New-TeeRecord 'Par3' $true 0 $null)
    )
    $holeAims = $aims[$hole - 1]
    $aim1 = New-Position $holeAims.A1[0] $center.y $holeAims.A1[1]
    $aim2 = if ($null -ne $holeAims.A2) { New-Position $holeAims.A2[0] $center.y $holeAims.A2[1] } else { $null }
    $tees += ,(New-TeeRecord 'AimPoint1' $true 0 $aim1)
    $tees += ,(New-TeeRecord 'AimPoint2' $true 0 $aim2)
    $tees += ,(New-TeeRecord 'GreenCenterPoint' $false 0 $center)

    $holes += ,[ordered]@{
        Enabled = $true
        HoleNumber = $hole
        Par = $pars[$hole - 1]
        Index = $indexes[$hole - 1]
        Tees = $tees
        Pins = $pins
    }
}

$hazardPolygons = @()
$hazardPolygons += ,@(535,713, 550,708, 575,716, 610,710, 650,703, 690,700, 720,711, 735,725,
    729,742, 700,746, 668,731, 640,736, 620,754, 600,742, 575,724, 558,742, 537,732)
$hazardPolygons += ,@(586,906, 610,899, 647,901, 685,908, 716,919, 704,933, 675,941, 635,942, 602,933, 586,920)
$hazardPolygons += ,@(970,827, 991,821, 1014,827, 1022,840, 1014,852, 991,855, 971,847)
$hazardPolygons += ,@(873,936, 893,928, 911,936, 920,951, 914,968, 899,980, 883,970, 876,953)
$hazardPolygons += ,@(823,972, 840,966, 855,975, 857,991, 846,1004, 830,1000, 820,987)
$hazardPolygons += ,@(728,1078, 752,1068, 783,1066, 817,1074, 839,1088, 829,1102, 800,1110, 762,1108, 735,1097)
$hazardPolygons += ,@(911,660, 929,654, 944,663, 949,685, 946,710, 935,729, 920,721, 912,700)
$hazards = @()
foreach ($polygon in $hazardPolygons) {
    $hazards += ,(Convert-ImagePolygonToHazard $polygon 43)
}

$gkd = [ordered]@{}
foreach ($property in $template.PSObject.Properties) { $gkd[$property.Name] = $property.Value }
$gkd.SceneFolderName = $courseFolder
$gkd.CourseName = $courseName
$gkd.Designer = 'Stonehill / Codex GIS-first beta'
$gkd.DescriptionTxtFileName = ''
$gkd.CoursePar = 34
$gkd.par = 34
$gkd.hazardCount = $hazards.Count
$gkd.teeTypeCount = 2
$gkd.pOOB = [ordered]@{
    pointCount = 4
    coords = @(
        (New-Position 225 45 875), (New-Position 1130 45 875),
        (New-Position 1130 45 1490), (New-Position 225 45 1490)
    )
}
$gkd.CourseInfo = 'Front-nine rebuild using the annotated Stonehill panorama, 2023-24 DTM, 2021 orthophoto, official 2025 scorecard, and existing BaseProject assets.'
$gkd.Holes = $holes
$gkd.Hazards = $hazards
$gkd.TeeTypeTotalDistance = @(
    (New-TeeRecord 'Black' $true 0 $null),
    (New-TeeRecord 'White' $true (($whiteYards | Measure-Object -Sum).Sum * 0.9144) $null),
    (New-TeeRecord 'Green' $true 0 $null),
    (New-TeeRecord 'Blue' $true 0 $null),
    (New-TeeRecord 'Yellow' $true 0 $null),
    (New-TeeRecord 'Red' $true (($redYards | Measure-Object -Sum).Sum * 0.9144) $null),
    (New-TeeRecord 'Junior' $true 0 $null),
    (New-TeeRecord 'Par3' $true 0 $null)
)

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$gkd | ConvertTo-Json -Depth 20 -Compress | Set-Content -LiteralPath (Join-Path $OutputDirectory "$courseFolder.GKD") -NoNewline -Encoding UTF8

$details = (Get-Content -Raw -LiteralPath $templateDetailsPath).Trim().Split('|')
$details[0] = $courseName
$details[1] = 'Stonehill / Codex GIS-first beta'
$details[3] = 'A rebuilt front-nine beta with annotated routing, official yardages, smoothed greens and tees, and mapped water hazards.'
$details[7] = '34'
for ($i = 0; $i -lt 18; $i++) {
    $details[8 + $i] = if ($i -lt 9) { [string]$pars[$i] } else { '0' }
    $details[26 + $i] = if ($i -lt 9) { [string]$indexes[$i] } else { '0' }
}
$details[82] = [string](($whiteYards | Measure-Object -Sum).Sum)
$details[86] = [string](($redYards | Measure-Object -Sum).Sum)
($details -join '|') | Set-Content -LiteralPath (Join-Path $OutputDirectory 'coursedetails.txt') -NoNewline -Encoding UTF8

Copy-Item -LiteralPath $BundlePath -Destination (Join-Path $OutputDirectory "$courseFolder.gspcrse") -Force
Copy-Item -LiteralPath $ReleaseManifestPath -Destination (Join-Path $OutputDirectory 'stonehill-release.json') -Force

Add-Type -AssemblyName System.Drawing
$sourceImage = [System.Drawing.Image]::FromFile($TopImagePath)
try {
    $bitmap = [System.Drawing.Bitmap]::new(480, 270)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $cropHeight = [int]($sourceImage.Width * 9 / 16)
        $cropY = [int](($sourceImage.Height - $cropHeight) / 2)
        $graphics.DrawImage($sourceImage, [System.Drawing.Rectangle]::new(0,0,480,270),
            [System.Drawing.Rectangle]::new(0,$cropY,$sourceImage.Width,$cropHeight), [System.Drawing.GraphicsUnit]::Pixel)
        $overlay = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(165, 8, 18, 20))
        $graphics.FillRectangle($overlay, 0, 216, 480, 54)
        $font = [System.Drawing.Font]::new('Arial', 17, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $small = [System.Drawing.Font]::new('Arial', 10, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
        $graphics.DrawString('STONEHILL GOLF CLUB', $font, $whiteBrush, 14, 223)
        $releaseLabel = "Front Nine Beta • v$courseVersion • $($releaseUpdated.ToString('yyyy-MM-dd'))"
        $graphics.DrawString($releaseLabel, $small, $whiteBrush, 16, 247)
        $bitmap.Save((Join-Path $OutputDirectory 'splash template.jpg'), [System.Drawing.Imaging.ImageFormat]::Jpeg)
        $bitmap.Save((Join-Path $OutputDirectory 'image_altered_480_270splash template.jpg'), [System.Drawing.Imaging.ImageFormat]::Jpeg)
        $overlay.Dispose(); $font.Dispose(); $small.Dispose(); $whiteBrush.Dispose()
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
finally { $sourceImage.Dispose() }

Write-Output "METADATA_READY version=$courseVersion updated=$($releaseUpdated.ToString('yyyy-MM-dd')) output=$OutputDirectory"
