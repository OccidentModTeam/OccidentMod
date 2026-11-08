#nullable enable

using System;
using System.Collections.Generic;
using OccidentMod.World.Generation;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森生成位置選擇器。
///     Spore Dream Forest generation site selector.
/// </summary>
internal static class SporeDreamSiteSelector
{
    // 每個搜尋階段最多保留的候選數量。
    // Maximum retained candidates per search stage.
    private const int MaximumCandidates = 256;

    // 森林與世界邊緣的最低距離。
    // Minimum distance from world edges.
    private const int WorldEdgeMargin = 180;

    /// <summary>
    ///     尋找適合孢夢之森的生成位置。
    ///     Find suitable generation sites for the forest.
    /// </summary>
    internal static Search Find(
        int worldWidth,
        int spawn,
        Func<int, WorldColumn> column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (worldWidth <= WorldEdgeMargin * 2 ||
            spawn < 0 ||
            spawn >= worldWidth)
            return new Search(
                new List<Site>(),
                "invalid-world"
            );

        var widths =
            SporeDreamGenerationScale.Widths(worldWidth);

        // 第一階段：標準選址。
        // Stage 1: standard site search.
        var sites = Scan(
            worldWidth,
            spawn,
            widths,
            column,
            false
        );

        if (sites.Count > 0)
            return new Search(
                sites,
                "standard"
            );

        // 第二階段：逐格搜尋，但維持標準森林尺寸。
        // Stage 2: detailed search at standard widths.
        sites = Scan(
            worldWidth,
            spawn,
            widths,
            column,
            true
        );

        if (sites.Count > 0)
            return new Search(
                sites,
                "detailed-safe-search"
            );

        // 第三階段：安全條件不變，逐步縮小森林。
        // Stage 3: reduce width while preserving safety.
        var rescueWidths =
            SporeDreamGenerationScale.RescueWidths(
                worldWidth
            );

        sites = Scan(
            worldWidth,
            spawn,
            rescueWidths,
            column,
            true,
            true
        );

        return new Search(
            sites,
            "compact-safe-search"
        );
    }

    /// <summary>
    ///     執行單一階段的候選位置搜尋。
    ///     Scan candidate locations for one search stage.
    /// </summary>
    private static List<Site> Scan(
        int worldWidth,
        int spawn,
        int[] widths,
        Func<int, WorldColumn> column,
        bool detailed,
        bool compact = false)
    {
        List<Site> sites = new();

        if (widths.Length == 0)
            return sites;

        var preferred =
            SporeDreamGenerationScale.Widths(
                worldWidth
            )[0];

        var searchDistance =
            SporeDreamGenerationScale.Distance(
                1100,
                worldWidth
            );

        var evilSearchDistance =
            SporeDreamGenerationScale.Distance(
                1400,
                worldWidth
            );

        var gap =
            SporeDreamGenerationScale.Distance(
                compact ? 16 : 48,
                worldWidth
            );

        var step = detailed
            ? 1
            : Math.Max(
                1,
                SporeDreamGenerationScale.Distance(
                    12,
                    worldWidth
                )
            );

        // 暫存地形欄位，減少反覆取樣。
        // Cache terrain columns to avoid repeated sampling.
        Dictionary<int, WorldColumn> cache = new();

        WorldColumn ReadColumn(int x)
        {
            if (!cache.TryGetValue(
                    x,
                    out var result))
            {
                result = column(x);
                cache[x] = result;
            }

            return result;
        }

        foreach (var direction in new[] { -1, 1 })
        {
            var evilDistance = worldWidth;

            // 逐格尋找邪惡地形的第一道邊界。
            // Find the first evil terrain boundary.
            var evilLimit = Math.Min(
                evilSearchDistance,
                worldWidth / 3
            );

            for (var distance = 80;
                 distance < evilLimit;
                 distance++)
            {
                var x =
                    spawn + direction * distance;

                if (x < WorldEdgeMargin ||
                    x >= worldWidth - WorldEdgeMargin)
                    break;

                if (ReadColumn(x).Evil)
                {
                    evilDistance = distance;
                    break;
                }
            }

            foreach (var width in widths)
            {
                if (width <= 0)
                    continue;

                var minimum = compact
                    ? width / 2 + gap
                    : detailed
                        ? Math.Max(
                            SporeDreamGenerationScale.Distance(
                                120,
                                worldWidth
                            ),
                            width / 2 + gap
                        )
                        : Math.Max(
                            SporeDreamGenerationScale.Distance(
                                220,
                                worldWidth
                            ),
                            width / 2 + gap
                        );

                // 不允許森林越過邪惡地形邊界。
                // Prevent crossing into evil terrain.
                var limit = Math.Min(
                    searchDistance,
                    evilDistance - width / 2 - gap
                );

                if (limit <= minimum)
                    continue;

                for (var distance = minimum;
                     distance < limit;
                     distance += step)
                {
                    var left =
                        spawn +
                        direction * distance -
                        width / 2;

                    if (left < WorldEdgeMargin ||
                        left + width >=
                        worldWidth - WorldEdgeMargin)
                        continue;

                    if (!TryEvaluateSite(
                            left,
                            width,
                            distance,
                            preferred,
                            ReadColumn,
                            out var site))
                        continue;

                    if (site is not null)
                        AddCandidate(
                            sites,
                            site
                        );
                }
            }
        }

        // 優先較寬的森林，其次地形分數。
        // Prefer wider sites, then better terrain scores.
        sites.Sort(CompareSites);

        return sites;
    }

    /// <summary>
    ///     驗證森林候選地形並計算分數。
    ///     Validate terrain and score a forest candidate.
    /// </summary>
    private static bool TryEvaluateSite(
        int left,
        int width,
        int distance,
        int preferred,
        Func<int, WorldColumn> column,
        out Site? site)
    {
        site = null;

        var surface = new int[width];

        var grassCount = 0;
        var minimumGround = int.MaxValue;
        var maximumGround = int.MinValue;

        long waterDepth = 0;

        for (var k = 0; k < width; k++)
        {
            var current =
                column(left + k);

            // 排除不安全地形與邪惡環境。
            // Exclude unsafe and evil terrain.
            if (!current.Safe || current.Evil) return false;

            surface[k] = current.Ground;

            if (current.Grass)
                grassCount++;

            waterDepth += Math.Max(
                0,
                current.WaterDepth
            );

            minimumGround = Math.Min(
                minimumGround,
                current.Ground
            );

            maximumGround = Math.Max(
                maximumGround,
                current.Ground
            );
        }

        // 至少四分之一地面必須是草地。
        // Require grass on at least one quarter of columns.
        if (grassCount < width / 4)
            return false;

        // 擴大森林後容許更自然的地表高低差。
        // Allow additional terrain relief for larger forests.
        var maximumRelief = Math.Clamp(
            (int)Math.Round(width * 0.12),
            80,
            108
        );

        var relief =
            maximumGround - minimumGround;

        if (relief > maximumRelief)
            return false;

        var score =
            distance +
            relief * 2L +
            (preferred - width) * 3L +
            waterDepth * 2L;

        site = new Site(
            left,
            (int)Math.Clamp(
                score,
                int.MinValue,
                int.MaxValue
            ),
            surface
        );

        return true;
    }

    /// <summary>
    ///     將候選位置限制在固定數量內。
    ///     Keep candidate storage within a fixed limit.
    /// </summary>
    private static void AddCandidate(
        List<Site> sites,
        Site candidate)
    {
        if (sites.Count < MaximumCandidates)
        {
            sites.Add(candidate);
            return;
        }

        var worstIndex = 0;

        for (var i = 1;
             i < sites.Count;
             i++)
            if (CompareSites(
                    sites[i],
                    sites[worstIndex]) > 0)
                worstIndex = i;

        if (CompareSites(
                candidate,
                sites[worstIndex]) < 0)
            sites[worstIndex] = candidate;
    }

    /// <summary>
    ///     依森林寬度與地形品質排序。
    ///     Compare sites by width and terrain quality.
    /// </summary>
    private static int CompareSites(
        Site a,
        Site b)
    {
        var widthComparison =
            b.Surface.Length.CompareTo(
                a.Surface.Length
            );

        if (widthComparison != 0)
            return widthComparison;

        var scoreComparison =
            a.Score.CompareTo(b.Score);

        if (scoreComparison != 0)
            return scoreComparison;

        return a.Left.CompareTo(b.Left);
    }

    /// <summary>
    ///     單個候選森林位置。
    ///     One candidate forest location.
    /// </summary>
    internal sealed record Site(
        int Left,
        int Score,
        int[] Surface
    );

    /// <summary>
    ///     選址結果與搜尋階段。
    ///     Site selection result and search stage.
    /// </summary>
    internal sealed record Search(
        List<Site> Sites,
        string Mode
    );
}