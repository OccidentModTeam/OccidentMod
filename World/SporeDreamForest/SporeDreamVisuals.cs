using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森環境視覺及日夜過渡。
///     Forest visual blending and day/night transitions.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamVisuals : ModSystem
{
    public const int TransitionTicks = 420;

    private bool? entryDay;
    private double entryTime;
    private int transitionProgress;
    private bool wasInside;

    public float Blend { get; private set; }

    public float BackgroundBrightness => 0.78f;

    public (Color Top, Color Horizon) SkyPalette =>
        entryDay ?? Main.dayTime
            ? (
                new Color(78, 12, 22),
                new Color(157, 37, 45)
            )
            : (
                new Color(159, 166, 170),
                new Color(222, 225, 224)
            );

    /// <summary>
    ///     全景圖的中性曝光與透明度。
    ///     Neutral panorama exposure and opacity.
    /// </summary>
    public Color BackgroundTint(float opacity)
    {
        var level = (int)Math.Round(
            255f * BackgroundBrightness
        );

        return new Color(level, level, level) *
               MathHelper.Clamp(opacity, 0f, 1f);
    }

    public override void Load()
    {
        On_Main.DrawStarsInBackground += DrawStars;
        On_Main.DrawSunAndMoon += DrawSunAndMoon;
    }

    public override void Unload()
    {
        On_Main.DrawStarsInBackground -= DrawStars;
        On_Main.DrawSunAndMoon -= DrawSunAndMoon;
    }

    public override void ClearWorld()
    {
        Blend = 0f;
        entryDay = null;
        entryTime = 0;
        wasInside = false;
        transitionProgress = 0;
    }

    public override void PostUpdateEverything()
    {
        if (Main.gameMenu)
        {
            ClearWorld();
            return;
        }

        var point =
            Main.LocalPlayer.Center.ToTileCoordinates();

        var inside =
            SporeDreamEnvironmentSystem.Instance.Contains(
                point.X,
                point.Y
            );

        // 進入生態域時記錄顯示時間。
        // Capture the visual clock when entering the biome.
        if (inside && !wasInside)
        {
            entryDay = Main.dayTime;
            entryTime = Main.time;
        }

        wasInside = inside;

        transitionProgress = Math.Clamp(
            transitionProgress + (inside ? 1 : -1),
            0,
            TransitionTicks
        );

        Blend = transitionProgress /
                (float)TransitionTicks;

        if (Blend <= 0f)
            entryDay = null;
    }

    /// <summary>
    ///     暫時計算反轉天空顯示時間。
    ///     Temporarily calculate the inverted visual clock.
    /// </summary>
    private void ApplySkyClock()
    {
        var frozen = DisplayClock(
            entryDay ?? Main.dayTime,
            entryDay.HasValue ? entryTime : Main.time,
            1f
        );

        var result = InterpolateClock(
            Main.dayTime,
            Main.time,
            frozen.Day,
            frozen.Time,
            Blend
        );

        Main.dayTime = result.Day;
        Main.time = result.Time;
    }

    private void DrawStars(
        On_Main.orig_DrawStarsInBackground orig,
        Main self,
        Main.SceneArea sceneArea,
        bool artificial)
    {
        if (Main.gameMenu || Blend <= 0f)
        {
            orig(self, sceneArea, artificial);
            return;
        }

        var day = Main.dayTime;
        var time = Main.time;

        try
        {
            ApplySkyClock();
            orig(self, sceneArea, artificial);
        }
        finally
        {
            Main.dayTime = day;
            Main.time = time;
        }
    }

    private void DrawSunAndMoon(
        On_Main.orig_DrawSunAndMoon orig,
        Main self,
        Main.SceneArea sceneArea,
        Color moonColor,
        Color sunColor,
        float mushroomInfluence)
    {
        if (Main.gameMenu || Blend <= 0f)
        {
            orig(
                self,
                sceneArea,
                moonColor,
                sunColor,
                mushroomInfluence
            );

            return;
        }

        var day = Main.dayTime;
        var time = Main.time;

        try
        {
            ApplySkyClock();

            orig(
                self,
                sceneArea,
                moonColor,
                sunColor,
                mushroomInfluence
            );
        }
        finally
        {
            Main.dayTime = day;
            Main.time = time;
        }
    }

    public static (bool Day, double Time) DisplayClock(
        bool day,
        double time,
        float blend)
    {
        var targetDay = !day;

        var targetTime = day
            ? time / Main.dayLength * Main.nightLength
            : time / Main.nightLength * Main.dayLength;

        return InterpolateClock(
            day,
            time,
            targetDay,
            targetTime,
            blend
        );
    }

    /// <summary>
    ///     在完整日夜循環上平滑插值。
    ///     Smoothly interpolate across the day/night cycle.
    /// </summary>
    public static (bool Day, double Time) InterpolateClock(
        bool day,
        double time,
        bool targetDay,
        double targetTime,
        float blend)
    {
        var cycle =
            Main.dayLength + Main.nightLength;

        var source = day
            ? time
            : Main.dayLength + time;

        var target = targetDay
            ? targetTime
            : Main.dayLength + targetTime;

        var delta = target - source;

        if (delta > cycle / 2)
            delta -= cycle;

        if (delta < -cycle / 2)
            delta += cycle;

        double eased = MathHelper.SmoothStep(
            0f,
            1f,
            MathHelper.Clamp(blend, 0f, 1f)
        );

        var clock =
            (source + delta * eased + cycle) % cycle;

        return clock < Main.dayLength
            ? (true, clock)
            : (false, clock - Main.dayLength);
    }

    public override void ModifySunLightColor(
        ref Color tileColor,
        ref Color backgroundColor)
    {
        if (Main.gameMenu || Blend <= 0f)
            return;

        // 天空色彩由獨立圖層處理。
        // The sky palette is handled separately.
        tileColor = Color.Lerp(
            tileColor,
            Color.White,
            Blend
        );

        backgroundColor = Color.Lerp(
            backgroundColor,
            Color.White,
            Blend
        );
    }
}