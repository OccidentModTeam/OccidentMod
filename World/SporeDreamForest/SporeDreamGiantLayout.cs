using System;
using Microsoft.Xna.Framework;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     巨型蘑菇素材裁切及幾何規則。
///     Giant mushroom sprite layout and geometry.
/// </summary>
internal static class SporeDreamGiantLayout
{
    // 菌蓋素材尺寸。
    // Mushroom cap sprite dimensions.
    public const int CapCount = 3;
    public const int CapWidth = 126;
    public const int CapHeight = 72;
    public const int CapStride = 130;
    public const int CapAnchorX = 62;

    // 菇柄素材尺寸。
    // Mushroom stem sprite dimensions.
    public const int StemWidth = 20;
    public const int StemHeight = 20;
    public const int StemRows = 5;
    public const int StemRowStride = 22;
    public const int StemColumnStride = 22;

    public const int MinimumTrunkSpacing = 9;
    public const int MinimumHeight = 9;
    public const int MaximumHeight = 16;

    public const int CutTopExtension = 4;
    public const int CutStemHeight = 24;

    /// <summary>
    ///     檢查並計算巨菇基座的地形輪廓。
    ///     Validate and calculate the foundation profile.
    /// </summary>
    public static bool TryFoundationProfile(
        ReadOnlySpan<int> original,
        int ground,
        Span<int> result,
        int stemWidth = 1)
    {
        if (stemWidth is < 1 or > 2 ||
            original.Length != stemWidth + 6 ||
            result.Length < original.Length)
            return false;

        for (var index = 0; index < original.Length; index++)
        {
            var dx = index - 3;
            var old = original[index];

            var distance = dx < 0
                ? -dx
                : dx - stemWidth + 1;

            var target = Math.Min(
                old,
                ground + Math.Max(0, distance - 1)
            );

            if (dx >= 0 && dx < stemWidth)
                target = ground;

            if (target > old ||
                old - target > 2 ||
                Math.Abs(old - ground) > 4)
                return false;

            result[index] = target;
        }

        return true;
    }

    /// <summary>
    ///     取得菌蓋素材裁切範圍。
    ///     Get mushroom cap sprite frame.
    /// </summary>
    public static Rectangle CapFrame(int style)
    {
        return new Rectangle(
            Math.Clamp(style, 0, CapCount - 1) * CapStride,
            0,
            CapWidth,
            CapHeight
        );
    }

    /// <summary>
    ///     取得直立菇柄裁切範圍。
    ///     Get vertical stem sprite frame.
    /// </summary>
    public static Rectangle StemFrame(
        int style,
        bool decorated,
        int row,
        int height,
        bool down = false)
    {
        return new Rectangle(
            Math.Clamp(style, 0, StemRows - 1)
            * StemColumnStride,
            ((down ? 2 : 0) + (decorated ? 1 : 0))
            * StemRowStride + row,
            StemWidth,
            height
        );
    }

    public static Rectangle CutStemFrame(
        int style,
        int row,
        int height)
    {
        return new Rectangle(
            Math.Clamp(style, 0, StemRows - 1) * 18,
            row,
            16,
            height
        );
    }

    public static Rectangle HorizontalFrame(
        int style,
        bool decorated,
        bool right)
    {
        return new Rectangle(
            Math.Clamp(style, 0, 4) * 22,
            (right ? 46 : 0) + (decorated ? 22 : 0),
            20,
            20
        );
    }

    /// <summary>
    ///     根據世界 ID 和位置產生穩定亂數。
    ///     Generate a stable hash from world ID and position.
    /// </summary>
    public static uint Hash(
        int x,
        int top,
        int value,
        int worldId)
    {
        unchecked
        {
            var h =
                ((uint)x * 73856093u) ^
                ((uint)top * 19349663u) ^
                ((uint)(value + 1) * 83492791u) ^
                (uint)worldId;

            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;

            return h ^ (h >> 16);
        }
    }

    public static int Style(
        int x,
        int top,
        int segment,
        int worldId)
    {
        return segment == 0
            ? 0
            : (int)(Hash(x, top, segment, worldId) % StemRows);
    }

    public static int SegmentCount(int bodyPixels)
    {
        return (Math.Max(0, bodyPixels) + StemHeight - 1)
               / StemHeight;
    }

    /// <summary>
    ///     為整株巨菇分配裝飾位置。
    ///     Allocate decoration slots for the whole mushroom.
    /// </summary>
    public static (int First, int Second) DecorationSlots(
        int x,
        int top,
        int bodyPixels,
        int worldId)
    {
        var count = SegmentCount(bodyPixels);
        var eligible = count - 4;

        if (eligible <= 0)
            return (-1, -1);

        var seed = Hash(x, top, 7919, worldId);

        if (seed % 10 < 2)
            return (-1, -1);

        var first = 2 +
                    (int)((seed >> 8) % (uint)eligible);

        if (seed % 10 != 9)
            return (first, -1);

        var available = 0;

        for (var i = 2; i <= count - 3; i++)
            if (Math.Abs(i - first) >= 3)
                available++;

        if (available == 0)
            return (first, -1);

        var choice =
            (int)((seed >> 16) % (uint)available);

        for (var i = 2; i <= count - 3; i++)
            if (Math.Abs(i - first) >= 3 && choice-- == 0)
                return (first, i);

        return (first, -1);
    }

    public static bool Decorated(
        int x,
        int top,
        int bodyPixels,
        int segment,
        int worldId)
    {
        var slots = DecorationSlots(
            x,
            top,
            bodyPixels,
            worldId
        );

        return segment >= 0 &&
               (segment == slots.First ||
                segment == slots.Second);
    }

    /// <summary>
    ///     依原始 20 像素素材切割菇柄。
    ///     Slice stems without stretching source sprites.
    /// </summary>
    public static int SliceHeight(
        int bodyPixel,
        int remaining)
    {
        return Math.Min(
            remaining,
            StemHeight - bodyPixel % StemHeight
        );
    }
}