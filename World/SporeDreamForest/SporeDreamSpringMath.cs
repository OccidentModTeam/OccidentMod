using System;
using System.Collections.Generic;
using G = OccidentMod.World.SporeDreamForest.SporeDreamGrowthPath;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     巨菇彈跳表面及壓縮動畫計算。
///     Mushroom spring surface and compression calculations.
/// </summary>
internal static class SporeDreamSpringMath
{
    internal const int PressTicks = 6;
    internal const int AnimationTicks = 24;
    internal const int CooldownTicks = 18;

    internal const float PressDepth = 4f;
    internal const float LaunchSpeed = 12f;

    // 三種菌蓋的實際上緣輪廓。
    // Actual upper silhouettes for the three cap styles.
    private static readonly byte[][] Tops =
    {
        new byte[]
        {
            44, 44, 44, 44, 42, 42, 34, 34, 32, 32, 32, 32,
            32, 32, 30, 30, 28, 28, 28, 28, 28, 28, 26, 26,
            24, 24, 24, 24, 20, 20, 16, 16, 16, 16, 14, 14,
            14, 14, 12, 12, 12, 12, 10, 10, 10, 10, 8, 8,
            8, 8, 8, 8, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2,
            4, 4, 4, 4, 6, 6, 8, 8, 10, 10, 8, 8,
            8, 8, 8, 8, 10, 10, 12, 12, 12, 12, 12, 12,
            14, 14, 14, 14, 18, 18, 20, 20, 20, 20, 20, 20,
            20, 20, 20, 20, 22, 22, 28, 28, 32, 32, 32, 32,
            32, 32, 34, 34, 36, 36, 40, 40, 42, 42, 44, 44,
            46, 46
        },
        new byte[]
        {
            44, 44, 44, 44, 42, 42, 36, 36, 32, 32, 32, 32,
            30, 30, 28, 28, 24, 24, 18, 18, 14, 14, 12, 12,
            10, 10, 10, 10, 10, 10, 6, 6, 4, 4, 4, 4,
            4, 4, 4, 4, 6, 6, 8, 8, 10, 10, 8, 8,
            8, 8, 10, 10, 12, 12, 14, 14, 18, 18, 22, 22,
            22, 22, 22, 22, 20, 20, 20, 20, 18, 18, 16, 16,
            12, 12, 12, 12, 10, 10, 8, 8, 6, 6, 4, 4,
            4, 4, 6, 6, 6, 6, 8, 8, 8, 8, 8, 8,
            10, 10, 10, 10, 14, 14, 24, 24, 26, 26, 26, 26,
            28, 28, 32, 32, 34, 34, 40, 40, 40, 40, 42, 42,
            44, 44, 44, 44
        },
        new byte[]
        {
            44, 44, 44, 44, 42, 42, 40, 40, 38, 38, 38, 38,
            38, 38, 38, 38, 38, 38, 38, 38, 36, 36, 34, 34,
            32, 32, 30, 30, 26, 26, 22, 22, 20, 20, 20, 20,
            20, 20, 18, 18, 16, 16, 16, 16, 12, 12, 10, 10,
            6, 6, 4, 4, 4, 4, 4, 4, 4, 4, 0, 0,
            0, 0, 2, 2, 4, 4, 6, 6, 6, 6, 8, 8,
            10, 10, 10, 10, 14, 14, 16, 16, 16, 16, 16, 16,
            18, 18, 18, 18, 20, 20, 20, 20, 28, 28, 28, 28,
            32, 32, 32, 32, 36, 36, 38, 38, 38, 38, 38, 38,
            38, 38, 38, 38, 38, 38, 40, 40, 40, 40, 42, 42,
            44, 44, 46, 46
        }
    };

    /// <summary>
    ///     判斷是否為支援彈跳的菌蓋。
    ///     Determine whether the path supports spring jumping.
    /// </summary>
    internal static bool IsSpring(IReadOnlyList<SporeDreamGrowthPath.Piece> pieces)
    {
        if (pieces.Count == 0 ||
            pieces[^1].Kind != G.Crown ||
            pieces[^1].Direction != G.Up)
            return false;

        var vertical = 0;

        foreach (var piece in pieces)
            if (piece.Kind == G.Vertical && ++vertical > 1)
                return false;

        return true;
    }

    /// <summary>
    ///     根據菌蓋透明輪廓計算角色的接觸高度。
    ///     Calculate contact height using the cap silhouette.
    /// </summary>
    internal static bool TryTop(
        SporeDreamGrowthPath.Piece crown,
        float left,
        float width,
        out float top)
    {
        top = 0f;

        if (crown.Kind != G.Crown ||
            crown.Direction != G.Up ||
            crown.Style < 0 ||
            crown.Style >= Tops.Length ||
            width <= 0f)
            return false;

        var start = crown.X - 62f;

        var overlapLeft = Math.Max(
            left,
            start + 10f
        );

        var overlapRight = Math.Min(
            left + width,
            start + 116f
        );

        if (overlapRight - overlapLeft < 8f)
            return false;

        var from = Math.Clamp(
            (int)MathF.Floor(overlapLeft - start),
            10,
            115
        );

        var to = Math.Clamp(
            (int)MathF.Ceiling(overlapRight - start) - 1,
            10,
            115
        );

        var height = 72;

        for (var x = from; x <= to; x++)
            height = Math.Min(height, Tops[crown.Style][x]);

        if (height >= 72)
            return false;

        top = crown.Y - 72 + height;

        return true;
    }

    /// <summary>
    ///     判斷角色是否由上而下接觸菌蓋。
    ///     Detect downward crossing of the mushroom surface.
    /// </summary>
    internal static bool Crosses(
        float before,
        float after,
        float top,
        float velocity)
    {
        return velocity > 0f &&
               before <= top + 2f &&
               after >= top &&
               after >= before;
    }

    /// <summary>
    ///     菌蓋下壓、回彈與回復的動畫。
    ///     Cap compression, rebound and recovery animation.
    /// </summary>
    internal static float Offset(int age)
    {
        if (age < 0 || age >= AnimationTicks)
            return 0f;

        if (age <= PressTicks)
            return PressDepth * MathF.Sin(
                age / (float)PressTicks *
                MathF.PI / 2f
            );

        if (age <= 12)
            return PressDepth * MathF.Cos(
                (age - PressTicks) / 6f *
                MathF.PI / 2f
            );

        if (age < 20)
            return -1.5f * MathF.Sin(
                (age - 12) / 8f *
                MathF.PI
            );

        return 0f;
    }
}