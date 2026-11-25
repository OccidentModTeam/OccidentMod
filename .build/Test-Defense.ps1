param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../bin/Debug/net8.0/OccidentMod.dll')
)

$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$calculator = $assembly.GetType('OccidentMod.Features.System.Damage.Defense.DefenseCalculator', $true)
$formula = $assembly.GetType('OccidentMod.Features.System.Damage.Defense.DefenseFormula', $true)
$difficulty = $assembly.GetType('OccidentMod.Features.System.Damage.Defense.DefenseDifficulty', $true)

function Assert-Near([string]$Label, [double]$Actual, [double]$Expected) {
    if (-not [double]::IsFinite($Actual) -or [Math]::Abs($Actual - $Expected) -gt 0.00000001) {
        throw "${Label}: expected $Expected, got $Actual"
    }
}

# Reference values are reduction ratios, not percentages.
$cases = [ordered]@{
    '0' = 0.0
    '30' = 0.16448332384190964
    '100' = 0.43121142629255915
    '330' = 0.7501312995329003
    '500' = 0.8003522848013647
    '1000' = 0.8195292283993576
}

$previous = -1.0
foreach ($case in $cases.GetEnumerator()) {
    $defense = [double]$case.Key
    $actual = $calculator::GetOccidentReduction($defense)
    Assert-Near "Occident defense $defense" $actual $case.Value
    if ($actual -le $previous -or $actual -gt $calculator::OccidentMaximumReduction) {
        throw "Unexpected reduction at defense $defense"
    }
    $previous = $actual
}

foreach ($defense in -30.0, [double]::NaN, [double]::NegativeInfinity) {
    Assert-Near "Invalid or negative defense $defense" ($calculator::GetOccidentReduction($defense)) 0.0
}
foreach ($defense in 1000000.0, [double]::MaxValue, [double]::PositiveInfinity) {
    $actual = $calculator::GetOccidentReduction($defense)
    Assert-Near "Extreme defense $defense" $actual $calculator::OccidentMaximumReduction
    if ($actual -gt $calculator::OccidentMaximumReduction) { throw 'Reduction exceeded its cap.' }
}

Assert-Near 'Occident damage at 330 defense' ($calculator::CalculateDamage(100, 330, $formula::Occident)) (100 * (1 - $cases['330']))
Assert-Near 'Classic damage' ($calculator::CalculateDamage(100, 30, $formula::Vanilla, $difficulty::Classic)) 85
Assert-Near 'Expert damage before rounding' ($calculator::CalculateDamage(100, 30, $formula::Vanilla, $difficulty::Expert)) 77.5
Assert-Near 'Master damage' ($calculator::CalculateDamage(100, 30, $formula::Vanilla, $difficulty::Master)) 70
Assert-Near 'Vanilla minimum damage' ($calculator::CalculateDamage(100, 1000, $formula::Vanilla)) 1
Assert-Near 'Vanilla fractional hit floor' ($calculator::CalculateDamage(0.5, 0, $formula::Vanilla)) 1
Assert-Near 'Vanilla invalid defense' ($calculator::CalculateDamage(100, [double]::NaN, $formula::Vanilla)) 100

foreach ($damage in 0.0, -1.0, [double]::NaN, [double]::NegativeInfinity, [double]::PositiveInfinity) {
    foreach ($mode in $formula::Occident, $formula::Vanilla) {
        Assert-Near "Invalid damage $damage ($mode)" ($calculator::CalculateDamage($damage, 30, $mode)) 0
    }
}

foreach ($defense in 0.0, -30.0, [double]::NaN, [double]::NegativeInfinity, [double]::PositiveInfinity, [double]::MaxValue) {
    foreach ($mode in $formula::Occident, $formula::Vanilla) {
        $damage = $calculator::CalculateDamage([double]::MaxValue, $defense, $mode)
        if (-not [double]::IsFinite($damage) -or $damage -lt 0) {
            throw "Invalid damage at extreme defense $defense ($mode)"
        }
    }
}

'Defense math tests passed.'
