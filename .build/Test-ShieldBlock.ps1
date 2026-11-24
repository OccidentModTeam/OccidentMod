param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../bin/Debug/net8.0/OccidentMod.dll')
)

$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$shield = $assembly.GetType('OccidentMod.Features.System.Damage.Blocking.ShieldBlockCalculator', $true)
$defense = $assembly.GetType('OccidentMod.Features.System.Damage.Defense.DefenseCalculator', $true)
$formula = $assembly.GetType('OccidentMod.Features.System.Damage.Defense.DefenseFormula', $true)

function Assert-BlockResult(
    [string]$Label,
    $Result,
    [double]$InputDamage,
    [double]$BlockedDamage,
    [double]$DamageTaken,
    [bool]$IsPerfectBlock = $false
) {
    $expected = @{
        InputDamage = $InputDamage
        BlockedDamage = $BlockedDamage
        DamageTaken = $DamageTaken
    }

    foreach ($field in $expected.Keys) {
        $actual = $Result.$field
        if (-not [double]::IsFinite($actual) -or $actual -lt 0 -or $actual -ne $expected[$field]) {
            throw "${Label}: expected $field = $($expected[$field]), got $actual"
        }
    }

    if ($Result.IsPerfectBlock -ne $IsPerfectBlock) {
        throw "${Label}: unexpected perfect block flag"
    }
}

Assert-BlockResult 'No block' ($shield::NoBlock(200)) 200 0 200
Assert-BlockResult '50% block' ($shield::NormalBlock(200, 0.5)) 200 100 100
Assert-BlockResult 'Perfect block' ($shield::PerfectBlock(200)) 200 200 0 $true
Assert-BlockResult 'Negative ratio' ($shield::NormalBlock(200, -0.2)) 200 0 200
Assert-BlockResult 'Above-max ratio' ($shield::NormalBlock(200, 1.2)) 200 200 0

foreach ($damage in 0.0, -1.0, [double]::NaN, [double]::NegativeInfinity, [double]::PositiveInfinity) {
    Assert-BlockResult "No block with damage $damage" ($shield::NoBlock($damage)) 0 0 0
    Assert-BlockResult "Normal block with damage $damage" ($shield::NormalBlock($damage, 0.5)) 0 0 0
    Assert-BlockResult "Perfect block with damage $damage" ($shield::PerfectBlock($damage)) 0 0 0 $true
}
foreach ($ratio in [double]::NaN, [double]::NegativeInfinity, [double]::PositiveInfinity) {
    Assert-BlockResult "Non-finite ratio $ratio" ($shield::NormalBlock(200, $ratio)) 200 0 200
}
$max = [double]::MaxValue
Assert-BlockResult 'Extreme damage' ($shield::NormalBlock($max, 0.5)) $max ($max / 2) ($max / 2)

$afterDefense = $defense::CalculateDamage(300, 200, $formula::Vanilla)
$afterPassiveReduction = $afterDefense * 0.75
Assert-BlockResult 'Block after defense and passive reduction' ($shield::NormalBlock($afterPassiveReduction, 0.25)) 150 37.5 112.5

'Shield block math tests passed.'
