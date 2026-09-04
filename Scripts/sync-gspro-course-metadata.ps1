param(
    [string]$ReleaseManifestPath = (Join-Path $PSScriptRoot '..\CourseRelease.json'),
    [string]$GsproRoot = 'C:\GSProV1\Core\GSP',
    [string]$DatabasePath = (Join-Path $env:USERPROFILE 'AppData\LocalLow\GSPro\GSPro\GSPro.db')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReleaseManifestPath)) {
    throw "Release manifest not found: $ReleaseManifestPath"
}
if (-not (Test-Path -LiteralPath $DatabasePath)) {
    throw "GSPro database not found: $DatabasePath"
}

$release = Get-Content -Raw -LiteralPath $ReleaseManifestPath | ConvertFrom-Json
foreach ($propertyName in @('courseFolder', 'courseName', 'version', 'updated')) {
    if ([string]::IsNullOrWhiteSpace([string]$release.$propertyName)) {
        throw "Release manifest is missing '$propertyName': $ReleaseManifestPath"
    }
}

$updated = [DateTimeOffset]::Parse(
    [string]$release.updated,
    [Globalization.CultureInfo]::InvariantCulture,
    [Globalization.DateTimeStyles]::RoundtripKind)
$databaseDate = $updated.ToString('yyyy-MM-dd HH:mm:ss')

$managedDirectory = Join-Path $GsproRoot 'GSPro_Data\Managed'
$nativeDirectory = Join-Path $GsproRoot 'GSPro_Data\Plugins'
$firstPassAssembly = Join-Path $managedDirectory 'Assembly-CSharp-firstpass.dll'
if (-not (Test-Path -LiteralPath $firstPassAssembly)) {
    throw "GSPro managed assembly not found: $firstPassAssembly"
}

$env:PATH = "$nativeDirectory;$env:PATH"
Get-ChildItem -LiteralPath $managedDirectory -Filter '*.dll' | ForEach-Object {
    try { [void][Reflection.Assembly]::LoadFrom($_.FullName) } catch {}
}

$connection = [SQLite4Unity3d.SQLiteConnection]::new(
    $DatabasePath,
    ([SQLite4Unity3d.SQLiteOpenFlags]::ReadWrite -bor [SQLite4Unity3d.SQLiteOpenFlags]::FullMutex),
    $false)
try {
    $sql = @'
UPDATE CourseRepoCourse
SET RemoteVersion = ?,
    LocalVersion = ?,
    LastModified = ?,
    LastUpdated = ?
WHERE lower(CourseFolder) = lower(?)
  AND Name = ?
'@
    $arguments = [object[]]@(
        [string]$release.version,
        [string]$release.version,
        $databaseDate,
        $databaseDate,
        [string]$release.courseFolder,
        [string]$release.courseName)
    $changed = $connection.Execute($sql, $arguments)
    if ($changed -ne 1) {
        throw "Expected one GSPro course row for '$($release.courseName)', but updated $changed. Launch GSPro once so it discovers the installed course, then rerun this script."
    }
}
finally {
    $connection.Close()
}

Write-Output "GSPRO_METADATA_SYNCED course=$($release.courseFolder) version=$($release.version) updated=$($updated.ToString('yyyy-MM-dd')) database=$DatabasePath"
