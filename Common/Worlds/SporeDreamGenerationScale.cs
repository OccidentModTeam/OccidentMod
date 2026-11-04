// 按世界寬度換算群系尺寸與搜尋距離，集中生成比例參數。
using System;

namespace Occident.Common.Worlds;

internal static class SporeDreamGenerationScale
{
    internal static int Distance(int smallWorldDistance,int worldWidth) =>
        (int)Math.Round(smallWorldDistance*Math.Max(1f,worldWidth/4200f));

    internal static int[] Widths(int worldWidth) => new[] {
        Distance(240,worldWidth),Distance(208,worldWidth),Distance(176,worldWidth)
    };
    internal static int MinimumWidth(int worldWidth) => Distance(64,worldWidth);
    internal const int GiantCandidatePercent=37;
}
