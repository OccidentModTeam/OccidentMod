using System;

namespace OccidentMod.Features.System.Damage.Blocking;

public static class ShieldBlockCalculator
{
    /// <summary>Passes through valid damage when no block occurs.</summary>
    public static BlockResult NoBlock(double finalDamage) =>
        ApplyReduction(finalDamage, 0.0, isPerfectBlock: false);

    /// <summary>Applies a normal block to damage after defense and passive reductions.</summary>
    /// <remarks>Finite ratios are clamped to [0, 1]. Non-finite ratios grant no reduction.</remarks>
    public static BlockResult NormalBlock(double finalDamage, double blockRatio) =>
        ApplyReduction(finalDamage, double.IsFinite(blockRatio) ? Math.Clamp(blockRatio, 0.0, 1.0) : 0.0,
            isPerfectBlock: false);

    /// <summary>Resolves a successful perfect block after the caller checks attack eligibility.</summary>
    public static BlockResult PerfectBlock(double finalDamage) =>
        ApplyReduction(finalDamage, 1.0, isPerfectBlock: true);

    private static BlockResult ApplyReduction(double finalDamage, double reduction, bool isPerfectBlock)
    {
        double inputDamage = double.IsFinite(finalDamage) && finalDamage > 0.0 ? finalDamage : 0.0;
        double damageTaken = inputDamage * (1.0 - reduction);
        return new BlockResult(inputDamage, inputDamage - damageTaken, damageTaken, isPerfectBlock);
    }
}
