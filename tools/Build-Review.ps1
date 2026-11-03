[CmdletBinding()]
param(
    [string]$StageRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OccidentMod/ReviewBuild'),
    [switch]$Install,
    [string]$ModsDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot 'OccidentMod.csproj'
$stagePath = [IO.Path]::GetFullPath($StageRoot)
if ($stagePath -match '(?i)[\\/]Mods([\\/]|$)' -or $stagePath -match '(?i)[\\/]workshop([\\/]|$)') {
    throw 'StageRoot must be outside game Mods and workshop directories.'
}
if ($Install -and [string]::IsNullOrWhiteSpace($ModsDirectory)) {
    throw 'Installation requires an explicit -ModsDirectory for the client being tested.'
}
New-Item -ItemType Directory -Force -Path $stagePath | Out-Null
# Let the existing project import select tModLoader and the target framework.
# Do not use -modpath: it did not redirect this installed builder.
$buildFlags = '-tmlsavedirectory "' + $stagePath + '"'
& dotnet build $project --no-restore -v minimal "-p:ExtraBuildModFlags=$buildFlags"
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE; installed mod was not changed." }
$package = Join-Path $stagePath 'Mods/OccidentMod.tmod'
if (!(Test-Path -LiteralPath $package)) { throw "Expected package was not produced: $package" }
Write-Output "Review package: $package"
if (!$Install) { return }

$running = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match '^(dotnet|tModLoader)(\.exe)?$' -and $_.CommandLine -match 'tModLoader(\.dll|\.exe)'
}
if ($running) { throw 'Review package is ready. Save and exit tModLoader before installing; no process was stopped.' }
$modsPath = [IO.Path]::GetFullPath($ModsDirectory)
if (!(Test-Path -LiteralPath $modsPath -PathType Container)) { throw "Mods directory does not exist: $modsPath" }
$destination = Join-Path $modsPath 'OccidentMod.tmod'
$conflicts = @(Get-ChildItem -LiteralPath $modsPath -Recurse -File -Filter '*.tmod' | Where-Object {
    $_.FullName -ne $destination -and $_.BaseName -match '(?i)Occident|ArkheCrowns'
})
if ($conflicts.Count -gt 0) { throw ('Review conflicting packages before installation: ' + ($conflicts.FullName -join ', ')) }
$backupRoot = Join-Path $stagePath 'Backups'
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
if (Test-Path -LiteralPath $destination) {
    Copy-Item -LiteralPath $destination -Destination (Join-Path $backupRoot ('OccidentMod-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.tmod'))
}
Copy-Item -LiteralPath $package -Destination $destination
$expected = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
$actual = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
if ($actual -ne $expected) { throw 'Installed package hash differs from review package.' }
Write-Output "Installed single package: $destination"
Write-Output "SHA256: $actual"
