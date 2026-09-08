param(
 [Parameter(Mandatory=$true)][string]$BundlePath,
 [Parameter(Mandatory=$true)][string]$BackupDirectory,
 [string]$Repository=(Split-Path $PSScriptRoot -Parent),
 [string]$Destination='C:\GSProV1\Core\GSP\Courses\Stonehill_9H'
)
$ErrorActionPreference='Stop'
$release=Get-Content -Raw (Join-Path $Repository 'CourseRelease.json') | ConvertFrom-Json
$sourceGkd=Join-Path $Repository 'GSPro\Stonehill_9H\Stonehill_9H.GKD'
$installedBundle=Join-Path $Destination 'Stonehill_9H.gspcrse'
$installedGkd=Join-Path $Destination 'Stonehill_9H.GKD'
if(!(Test-Path $BundlePath) -or !(Test-Path $sourceGkd)){throw 'Missing built package or metadata'}
if((Get-Item $BundlePath).Length -lt 1000000){throw 'Package unexpectedly small'}
if(Test-Path $BackupDirectory){throw 'Choose a new backup directory; never overwrite a rollback checkpoint'}
New-Item -ItemType Directory -Path $BackupDirectory | Out-Null
foreach($p in @($installedBundle,$installedGkd,(Join-Path $Destination 'stonehill-release.json'))){if(Test-Path $p){Copy-Item -LiteralPath $p -Destination $BackupDirectory}}
$sourceCommit=git -C $Repository rev-parse HEAD
if($LASTEXITCODE -ne 0){throw 'Cannot identify source commit'}
$buildHash=(Get-FileHash -LiteralPath $BundlePath -Algorithm SHA256).Hash
$gkdHash=(Get-FileHash -LiteralPath $sourceGkd -Algorithm SHA256).Hash
try{
 Copy-Item -LiteralPath $BundlePath -Destination $installedBundle -Force
 Copy-Item -LiteralPath $sourceGkd -Destination $installedGkd -Force
 Copy-Item -LiteralPath (Join-Path $Repository 'CourseRelease.json') -Destination (Join-Path $Destination 'stonehill-release.json') -Force
 if((Get-FileHash $installedBundle).Hash -ne $buildHash -or (Get-FileHash $installedGkd).Hash -ne $gkdHash){throw 'Installed hashes differ from source'}
}catch{
 foreach($name in @('Stonehill_9H.gspcrse','Stonehill_9H.GKD','stonehill-release.json')){if(Test-Path (Join-Path $BackupDirectory $name)){Copy-Item (Join-Path $BackupDirectory $name) $Destination -Force}}
 throw
}
$receipt=[ordered]@{version=$release.version;sourceCommit=$sourceCommit;installedUtc=[DateTime]::UtcNow.ToString('o');packageSha256=$buildHash;gkdSha256=$gkdHash;backupDirectory=$BackupDirectory;destination=$Destination;gsproPlaytest='Awaiting user';runtimeInspection='Pending'}
$receipt | ConvertTo-Json | Set-Content (Join-Path $Repository 'Reviews\installation.json')
$receipt | ConvertTo-Json
