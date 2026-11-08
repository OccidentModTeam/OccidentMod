#nullable enable

using System;
using System.Collections.Generic;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森生成尺寸及搜尋距離換算。
///     Scaling of biome dimensions and search distances.
/// </summary>
internal static class SporeDreamGenerationScale
{
    private const int ReferenceWorldWidth = 4200;
    private const int MinimumSearchDistance = 1;

    // 每個候選巨菇位置的生成機率。
    // Chance for each giant mushroom candidate.
    internal const int GiantCandidatePercent = 37;

    /// <summary>
    ///     按世界寬度縮放一般搜尋距離。
    ///     Scale general search distances by world width.
    /// </summary>
    internal static int Distance(
        int smallWorldDistance,
        int worldWidth)
    {
        if (smallWorldDistance <= 0)
            return 0;

        if (worldWidth <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(worldWidth));

        var scale = Math.Max(
            1.0,
            worldWidth / (double)ReferenceWorldWidth);

        return Math.Max(
            MinimumSearchDistance,
            (int)Math.Round(
                smallWorldDistance * scale,
                MidpointRounding.AwayFromZero));
    }

    /// <summary>
    ///     取得標準、大幅縮小及次級候選森林寬度。
    ///     Get preferred and fallback forest widths.
    /// </summary>
    internal static int[] Widths(int worldWidth)
    {
        var preferred =
            SporeDreamForestScalePolicy.GetForestWidth(
                worldWidth);

        // 保留舊版 240 / 208 / 176 的相對比例。
        // Preserve the original candidate width ratios.
        var medium = (int)Math.Round(
            preferred * 208.0 / 240.0,
            MidpointRounding.AwayFromZero);

        var compact = (int)Math.Round(
            preferred * 176.0 / 240.0,
            MidpointRounding.AwayFromZero);

        return DistinctDescending(
            preferred,
            medium,
            compact);
    }

    /// <summary>
    ///     取得保底搜尋允許的最小森林寬度。
    ///     Get the minimum fallback forest width.
    /// </summary>
    internal static int MinimumWidth(int worldWidth)
    {
        var preferred =
            SporeDreamForestScalePolicy.GetForestWidth(
                worldWidth);

        return Math.Max(
            64,
            (int)Math.Round(
                preferred * 64.0 / 240.0,
                MidpointRounding.AwayFromZero));
    }

    /// <summary>
    ///     取得保底搜尋的候選森林寬度。
    ///     Build fallback width candidates for rescue search.
    /// </summary>
    internal static int[] RescueWidths(int worldWidth)
    {
        var preferred =
            SporeDreamForestScalePolicy.GetForestWidth(
                worldWidth);

        var minimum = MinimumWidth(worldWidth);

        List<int> widths = new();

        foreach (var width in Widths(worldWidth))
            widths.Add(width);

        // 逐步減小，最後才縮至最小保底寬度。
        // Reduce gradually before reaching the minimum.
        for (var percent = 67;
             percent >= 27;
             percent -= 8)
            widths.Add(Math.Max(
                minimum,
                (int)Math.Round(
                    preferred * percent / 100.0,
                    MidpointRounding.AwayFromZero)));

        widths.Add(minimum);

        return DistinctDescending(widths.ToArray());
    }

    /// <summary>
    ///     移除重複尺寸並按寬度由大至小排序。
    ///     Deduplicate and sort candidate widths descending.
    /// </summary>
    private static int[] DistinctDescending(
        params int[] values)
    {
        HashSet<int> seen = new();
        List<int> widths = new();

        foreach (var width in values)
            if (width > 0 && seen.Add(width))
                widths.Add(width);

        widths.Sort((a, b) => b.CompareTo(a));

        return widths.ToArray();
    }
}