using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森連續霧帶路徑。
///     Continuous forest mist ribbon path.
/// </summary>
internal static class SporeDreamRibbonPath
{
    /// <summary>
    ///     建立霧帶的連續取樣點。
    ///     Generate continuous ribbon samples.
    /// </summary>
    internal static void Append(
        float left,
        float right,
        float phase,
        Func<float, float> ground,
        Func<Vector2, bool> touchesStem,
        List<Sample> output)
    {
        ArgumentNullException.ThrowIfNull(ground);
        ArgumentNullException.ThrowIfNull(touchesStem);
        ArgumentNullException.ThrowIfNull(output);

        if (right <= left)
            return;

        var steps = Math.Max(
            1,
            (int)Math.Ceiling(
                (right - left) / 4f
            )
        );

        var touching = false;
        var front = false;

        // 從整片森林開始追蹤前後層關係。
        // Track layering across the entire biome.
        for (var i = 0; i <= steps; i++)
        {
            var progress = i / (float)steps;

            var x =
                left + (right - left) * progress;

            var wave = x / 185f + phase;

            var y =
                ground(x) -
                102f +
                MathF.Sin(wave) * 42f +
                MathF.Sin(
                    x / 83f - phase * .63f
                ) * 15f;

            Vector2 position = new(x, y);

            var contact = touchesStem(position);

            if (contact && !touching)
                front = !front;

            touching = contact;

            // 疊加長短波形形成自然霧帶。
            // Combine long and short waves for thickness.
            var thickness =
                (MathF.Sin(
                    x / 137f - phase * .71f
                ) + 1f) * .5f;

            var detail =
                (MathF.Sin(
                    x / 51f + phase * .43f
                ) + 1f) * .5f;

            var radius =
                10f +
                38f * thickness * thickness +
                8f * detail;

            output.Add(
                new Sample(
                    position,
                    front,
                    progress,
                    radius
                )
            );
        }
    }

    internal readonly record struct Sample(
        Vector2 Position,
        bool Front,
        float Progress,
        float Radius
    );
}