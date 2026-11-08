using System;
using System.Collections.Generic;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森地形起伏計算。
///     Smooth terrain profile generation.
/// </summary>
internal static class SporeDreamTerrainProfile
{
    internal const int MaxOffset = 8;
    internal const int EdgeMargin = 16;

    /// <summary>
    ///     產生平滑的地形偏移量。
    ///     Generate smooth terrain offsets.
    /// </summary>
    internal static int[] CreateOffsets(
        int width,
        Func<int, int, int> next)
    {
        if (width < 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        ArgumentNullException.ThrowIfNull(next);

        var result = new int[width];

        if (width <= EdgeMargin * 2 + 1)
            return result;

        List<(int X, int Height)> points = new()
        {
            (EdgeMargin, 0)
        };

        var end = width - EdgeMargin - 1;

        for (var x = EdgeMargin + next(26, 47);
             x < end - 25;
             x += next(26, 47))
            points.Add((
                x,
                next(-MaxOffset, MaxOffset + 1)
            ));

        points.Add((end, 0));

        // 使用 Smoothstep 插值避免鋸齒。
        // Apply Smoothstep interpolation.
        for (var i = 1; i < points.Count; i++)
        {
            var from = points[i - 1];
            var to = points[i];

            for (var x = from.X; x <= to.X; x++)
            {
                var t =
                    (x - from.X) /
                    (double)(to.X - from.X);

                t = t * t * (3 - 2 * t);

                result[x] = (int)Math.Round(
                    from.Height +
                    (to.Height - from.Height) * t
                );
            }
        }

        return result;
    }
}