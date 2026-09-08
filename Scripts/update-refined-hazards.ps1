param(
    [Parameter(Mandatory=$true)][string]$GkdPath,
    [string]$BoundaryPath=(Join-Path $PSScriptRoot '..\Reviews\water-boundaries.json')
)
$ErrorActionPreference='Stop'
$gkd=Get-Content -LiteralPath $GkdPath -Raw | ConvertFrom-Json
$boundaries=Get-Content -LiteralPath $BoundaryPath -Raw | ConvertFrom-Json
if($boundaries.waters.Count -ne 7 -or $gkd.Hazards.Count -ne 7){throw 'Expected seven matching water features'}
$holesBefore=$gkd.Holes | ConvertTo-Json -Depth 30 -Compress
for($i=0;$i -lt 7;$i++){
    $points=@($boundaries.waters[$i].coords)
    if($points.Count -lt 3){throw 'Invalid shoreline'}
    $gkd.Hazards[$i].coords=$points
    $gkd.Hazards[$i].pointCount=$points.Count
}
$gkd.hazardCount=7
if(($gkd.Holes | ConvertTo-Json -Depth 30 -Compress) -cne $holesBefore){throw 'Hole metadata changed unexpectedly'}
$gkd | ConvertTo-Json -Depth 30 -Compress | Set-Content -LiteralPath $GkdPath -NoNewline -Encoding UTF8
$check=Get-Content -LiteralPath $GkdPath -Raw | ConvertFrom-Json
for($i=0;$i -lt 7;$i++){
    if(($check.Hazards[$i].coords | ConvertTo-Json -Compress) -cne ($boundaries.waters[$i].coords | ConvertTo-Json -Compress)){throw 'Saved shoreline mismatch'}
}
Write-Output 'PASS: seven GKD shorelines match Unity export; hole, tee, pin and aiming data preserved.'
