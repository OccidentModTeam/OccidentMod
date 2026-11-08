using System;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森全景背景佈局。
///     Spore Dream Forest panorama layout.
/// </summary>
internal static class SporeDreamBackgroundLayout
{
    /// <summary>
    ///     使用地表中位數定位背景。
    ///     Anchor the background to median terrain height.
    /// </summary>
    internal static float RepresentativeGround(int[] surface)
    {
        if (surface.Length == 0)
            return 0f;

        var heights = (int[])surface.Clone();
        Array.Sort(heights);

        return heights[heights.Length / 2] * 16f;
    }

    /// <summary>
    ///     計算全景的位置與縮放。
    ///     Calculate panorama position and scale.
    /// </summary>
    internal static Panorama Place(
        int imageWidth,
        int imageHeight,
        int screenWidth,
        int screenHeight,
        float groundY,
        float cameraY,
        float horizontalProgress)
    {
        var scale = Math.Max(
            2,
            (int)Math.Ceiling(
                screenWidth / (float)imageWidth
            )
        );

        var width = imageWidth * scale;
        var height = imageHeight * scale;

        var bottom =
            screenHeight * 0.92f +
            (groundY - cameraY - screenHeight * 0.65f) * 0.2f;

        var top = (int)Math.Round(bottom - height);

        var left = -(int)Math.Round(
            (width - screenWidth) *
            Math.Clamp(horizontalProgress, 0f, 1f)
        );

        var fogStart =
            top + height - Math.Max(64, height / 6);

        return new Panorama(
            left,
            top,
            width,
            height,
            fogStart
        );
    }

    /// <summary>
    ///     根據玩家深入地下的程度淡出背景。
    ///     Fade the background as the player goes underground.
    /// </summary>
    internal static float SurfaceVisibility(
        float playerY,
        float localGroundY)
    {
        var depth = (playerY - localGroundY) / 16f;

        var t = Math.Clamp(
            (depth - 8f) / 24f,
            0f,
            1f
        );

        return 1f - t * t * (3f - 2f * t);
    }

    internal readonly record struct Panorama(
        int Left,
        int Top,
        int Width,
        int Height,
        int FogStart)
    {
        public int Bottom => Top + Height;
    }
}