using System;
using System.Collections.Generic;

namespace Occident.Common.Worlds;

internal static class SporeDreamTerrainProfile
{
    internal const int MaxOffset = 8;
    internal const int EdgeMargin = 16;

    // Long, irregular slopes layered over the original terrain; never white noise
    // per tile. The caller supplies world-generation randomness for seed stability.
    internal static int[] CreateOffsets(int width, Func<int, int, int> next)
    {
        var result = new int[width];
        if (width <= EdgeMargin * 2 + 1)
            return result;
        var points = new List<(int X, int Height)> { (EdgeMargin, 0) };
        int end = width - EdgeMargin - 1;
        for (int x = EdgeMargin + next(26, 47); x < end - 25; x += next(26, 47))
            points.Add((x, next(-MaxOffset, MaxOffset + 1)));
        points.Add((end, 0));
        for (int i = 1; i < points.Count; i++)
        {
            var from = points[i - 1];
            var to = points[i];
            for (int x = from.X; x <= to.X; x++)
            {
                double t = (x - from.X) / (double)(to.X - from.X);
                t = t * t * (3 - 2 * t);
                result[x] = (int)Math.Round(from.Height + (to.Height - from.Height) * t);
            }
        }
        return result;
    }
}
