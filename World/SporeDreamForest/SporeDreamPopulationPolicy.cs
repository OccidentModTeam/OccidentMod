#nullable enable

using System;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森彎曲巨菇的生成數量政策。
///     Population policy for curved mushrooms.
/// </summary>
internal static class SporeDreamPopulationPolicy
{
    // 大型世界森林 480 格對應天然生成上限 32 株。
    // A 480-tile forest permits up to 32 natural plants.
    private const int ReferenceForestWidth = 480;
    private const int ReferenceNaturalLimit = 32;

    // 存檔與多人同步包含玩家種植的矮菇，因此保留 256 上限。
    // Saved plants also include player-placed dwarfs.
    internal const int MaximumStoredCurvedPlants = 256;

    /// <summary>
    ///     按實際森林寬度計算天然巨菇上限。
    ///     Scale the natural population cap with forest width.
    /// </summary>
    internal static int GetCurvedPlantLimit(int forestWidth)
    {
        if (forestWidth <= 0)
            return 0;

        var scaled = ReferenceNaturalLimit *
            forestWidth / (double)ReferenceForestWidth;

        return Math.Clamp(
            (int)Math.Ceiling(scaled),
            16,
            MaximumStoredCurvedPlants);
    }

    /// <summary>
    ///     計算森林側面生成預算，沿用原有密度公式。
    ///     Preserve the previous side-growth budget formula.
    /// </summary>
    internal static int GetCurvedSideBudget(int forestWidth)
    {
        if (forestWidth <= 0)
            return 0;

        return Math.Max(
            1,
            (int)(forestWidth / 32f * 0.25f));
    }
}