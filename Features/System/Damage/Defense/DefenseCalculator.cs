using System;

namespace OccidentMod.Features.System.Damage.Defense;

public static class DefenseCalculator
{
    public const double OccidentMaximumReduction = 0.82;
    public const double OccidentDefenseScale = 134.0;

    /// <summary>Returns the reduction ratio contributed by effective defense.</summary>
    public static double GetOccidentReduction(double effectiveDefense) =>
        OccidentMaximumReduction * (1.0 - Math.Exp(-NormalizeDefense(effectiveDefense) / OccidentDefenseScale));

    /// <summary>Returns damage after defense, before final modifiers and integer rounding.</summary>
    /// <param name="incomingDamage">Damage entering the defense calculation.</param>
    /// <param name="effectiveDefense">Defense after any upstream adjustments.</param>
    /// <param name="formula">The defense formula to apply to this hit.</param>
    /// <param name="difficulty">The vanilla player defense multiplier to use.</param>
    public static double CalculateDamage(
        double incomingDamage,
        double effectiveDefense,
        DefenseFormula formula,
        DefenseDifficulty difficulty = DefenseDifficulty.Classic)
    {
        if (!double.IsFinite(incomingDamage) || incomingDamage <= 0.0)
            return 0.0;

        return formula switch
        {
            DefenseFormula.Occident => incomingDamage * (1.0 - GetOccidentReduction(effectiveDefense)),
            DefenseFormula.Vanilla => Math.Max(
                1.0,
                incomingDamage - NormalizeDefense(effectiveDefense) * GetVanillaEffectiveness(difficulty)),
            _ => throw new ArgumentOutOfRangeException(nameof(formula), formula, null)
        };
    }

    // Positive infinity saturates the curve. NaN and negative values contribute no defense.
    private static double NormalizeDefense(double defense) => defense > 0.0 ? defense : 0.0;

    private static double GetVanillaEffectiveness(DefenseDifficulty difficulty) => difficulty switch
    {
        DefenseDifficulty.Classic => 0.50,
        DefenseDifficulty.Expert => 0.75,
        DefenseDifficulty.Master => 1.00,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
    };
}
