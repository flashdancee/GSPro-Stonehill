param(
    [Parameter(Mandatory=$true)][string]$BundlePath,
    [Parameter(Mandatory=$true)][string]$BackupDirectory,
    [string]$Repository=(Split-Path $PSScriptRoot -Parent),
    [string]$Destination='C:\GSProV1\Core\GSP\Courses\Stonehill_9H',
    [string]$DatabasePath=(Join-Path $env:USERPROFILE 'AppData\LocalLow\GSPro\GSPro\GSPro.db')
)

$ErrorActionPreference='Stop'
$release=Get-Content -Raw -LiteralPath (Join-Path $Repository 'CourseRelease.json') | ConvertFrom-Json
$sourceCourse=Join-Path $Repository 'GSPro\Stonehill_9H'
$sources=[ordered]@{
    'Stonehill_9H.gspcrse'=$BundlePath
    'Stonehill_9H.GKD'=(Join-Path $sourceCourse 'Stonehill_9H.GKD')
    'stonehill-release.json'=(Join-Path $Repository 'CourseRelease.json')
    'coursedetails.txt'=(Join-Path $sourceCourse 'coursedetails.txt')
    'splash template.jpg'=(Join-Path $sourceCourse 'splash template.jpg')
}
$thumbnail=Join-Path $sourceCourse 'image_altered_480_270splash template.jpg'
if(Test-Path -LiteralPath $thumbnail){$sources['image_altered_480_270splash template.jpg']=$thumbnail}

foreach($source in $sources.Values){if(!(Test-Path -LiteralPath $source)){throw "Missing install source: $source"}}
if((Get-Item -LiteralPath $BundlePath).Length -lt 1000000){throw 'Package unexpectedly small'}
if(!(Test-Path -LiteralPath $Destination)){throw "GSPro course destination missing: $Destination"}
if(Test-Path -LiteralPath $BackupDirectory){throw 'Choose a new backup directory; never overwrite a rollback checkpoint'}
if(Get-Process -Name GSPro -ErrorAction SilentlyContinue){throw 'Close GSPro before installing the course'}

$sourceCommit=git -C $Repository rev-parse HEAD
if($LASTEXITCODE -ne 0){throw 'Cannot identify source commit'}
New-Item -ItemType Directory -Path $BackupDirectory | Out-Null
$backedUp=@()
foreach($name in $sources.Keys){
    $installed=Join-Path $Destination $name
    if(Test-Path -LiteralPath $installed){
        Copy-Item -LiteralPath $installed -Destination (Join-Path $BackupDirectory $name)
        $backedUp+=$name
    }
}
if(Test-Path -LiteralPath $DatabasePath){
    Copy-Item -LiteralPath $DatabasePath -Destination (Join-Path $BackupDirectory 'GSPro.db')
}

try{
    foreach($name in $sources.Keys){
        Copy-Item -LiteralPath $sources[$name] -Destination (Join-Path $Destination $name) -Force
    }
    foreach($name in $sources.Keys){
        $sourceHash=(Get-FileHash -LiteralPath $sources[$name] -Algorithm SHA256).Hash
        $installedHash=(Get-FileHash -LiteralPath (Join-Path $Destination $name) -Algorithm SHA256).Hash
        if($sourceHash -ne $installedHash){throw "Installed hash differs from source: $name"}
    }
}catch{
    foreach($name in $sources.Keys){
        $installed=Join-Path $Destination $name
        if($name -in $backedUp){
            Copy-Item -LiteralPath (Join-Path $BackupDirectory $name) -Destination $installed -Force
        }elseif(Test-Path -LiteralPath $installed){
            Remove-Item -LiteralPath $installed -Force
        }
    }
    throw
}

$hashes=@(foreach($name in $sources.Keys){
    [ordered]@{file=$name;sha256=(Get-FileHash -LiteralPath (Join-Path $Destination $name) -Algorithm SHA256).Hash}
})
$receipt=[ordered]@{
    version=$release.version
    sourceCommit=$sourceCommit
    installedUtc=[DateTime]::UtcNow.ToString('o')
    packageSha256=($hashes | Where-Object file -eq 'Stonehill_9H.gspcrse').sha256
    gkdSha256=($hashes | Where-Object file -eq 'Stonehill_9H.GKD').sha256
    backupDirectory=$BackupDirectory
    databaseBackup=(Join-Path $BackupDirectory 'GSPro.db')
    destination=$Destination
    installedHashes=$hashes
    gsproPlaytest='Awaiting user'
    runtimeInspection='Pending'
}
$receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Repository 'Reviews\installation.json')
$receipt | ConvertTo-Json -Depth 5
