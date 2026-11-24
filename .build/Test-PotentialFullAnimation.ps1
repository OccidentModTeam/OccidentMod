param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../bin/Debug/net8.0/OccidentMod.dll')
)

$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$type = $assembly.GetType('OccidentMod.Presentation.UI.PotentialFullAnimation', $true)
$animation = [Activator]::CreateInstance($type)
function Assert-Frame($Expected) {
    if ($Expected -ne $animation.FrameIndex) {
        throw "Expected frame [$Expected], got [$($animation.FrameIndex)]"
    }
}

$animation.Update($false, $false, 0.1)
Assert-Frame $null
$animation.Update($true, $false, 0.1)
Assert-Frame 0
$animation.Update($true, $false, 0.72)
Assert-Frame 13
$animation.Update($true, $false, 0.06)
Assert-Frame 0
$animation.Update($true, $true, 0.01)
Assert-Frame 0
$animation.Update($false, $true, 0.72)
Assert-Frame 13
$animation.Update($false, $true, 1.0)
Assert-Frame $null
$animation.Update($false, $true, 1.0)
Assert-Frame $null
$animation.Update($false, $false, 0.1)
$animation.Update($true, $false, 0.1)
Assert-Frame 0
$animation.Reset()
Assert-Frame $null

'Potential full animation tests passed.'
