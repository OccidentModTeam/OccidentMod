#nullable enable

using System;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森的世界尺寸縮放政策。
///     World-size scaling policy for Spore Dream Forest.
/// </summary>
internal static class SporeDreamForestScalePolicy
{
    private const int SmallWorldWidth = 4200;
    private const int LargeWorldWidth = 8400;
    private const int ExtraLargeWorldWidth = 12000;

    private const int SmallForestWidth = 240;
    private const int LargeForestWidth = 480;
    private const int ExtraLargeForestWidth = 900;

    private const int MinimumForestWidth = 160;

    /// <summary>
    ///     根據實際世界寬度計算森林目標寬度。
    ///     Calculate target forest width from actual world width.
    /// </summary>
    internal static int GetForestWidth(int worldWidth)
    {
        if (worldWidth <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(worldWidth));

        double target;

        if (worldWidth <= LargeWorldWidth)
            // 小至大型世界採用平緩線性縮放。
            // Scale smoothly from small to large worlds.
            target = Interpolate(
                SmallWorldWidth,
                SmallForestWidth,
                LargeWorldWidth,
                LargeForestWidth,
                worldWidth);
        else
            // 特大型世界提高森林的相對占比。
            // Increase forest presence on extra-large worlds.
            target = Interpolate(
                LargeWorldWidth,
                LargeForestWidth,
                ExtraLargeWorldWidth,
                ExtraLargeForestWidth,
                worldWidth);

        // 防止極端自訂地圖造成超界目標。
        // Limit target width for unusual custom world sizes.
        var maximum = Math.Max(
            MinimumForestWidth,
            worldWidth / 8);

        return Math.Clamp(
            (int)Math.Round(
                target,
                MidpointRounding.AwayFromZero),
            MinimumForestWidth,
            maximum);
    }

    /// <summary>
    ///     根據森林寬度計算候選生成預算。
    ///     Calculate the terrain candidate search budget.
    /// </summary>
    internal static int GetSiteAttemptBudget(int forestWidth)
    {
        return Math.Clamp(
            (int)Math.Ceiling(forestWidth / 12.0),
            24,
            128);
    }

    /// <summary>
    ///     計算地形與森林邊界的最低緩衝距離。
    ///     Calculate a minimum terrain boundary margin.
    /// </summary>
    internal static int GetBoundaryMargin(int forestWidth)
    {
        return Math.Clamp(
            (int)Math.Ceiling(forestWidth * 0.025),
            8,
            32);
    }

    /// <summary>
    ///     以兩組設定點進行線性插值。
    ///     Linearly interpolate between two reference points.
    /// </summary>
    private static double Interpolate(
        int fromWorldWidth,
        int fromForestWidth,
        int toWorldWidth,
        int toForestWidth,
        int actualWorldWidth)
    {
        var progress =
            (actualWorldWidth - fromWorldWidth) /
            (double)(toWorldWidth - fromWorldWidth);

        return fromForestWidth +
               (toForestWidth - fromForestWidth) * progress;
    }
}