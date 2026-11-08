using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Tiles.SporeDream;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森動態迷霧系統。
///     Dynamic Spore Dream Forest mist system.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamMistSystem : ModSystem
{
    private readonly HashSet<Point> restoreStems = new();

    private readonly List<SporeDreamRibbonPath.Sample>
        samples = new(2048);

    private readonly List<Stamp> stamps = new(4096);
    private double elapsed;

    private Texture2D mistTexture;

    public override void ClearWorld()
    {
        elapsed = 0;
        stamps.Clear();
        samples.Clear();
        restoreStems.Clear();
    }

    public override void PostUpdateEverything()
    {
        if (Main.gameMenu)
        {
            ClearWorld();
            return;
        }

        if (Main.gamePaused)
            return;

        elapsed += 1.0 / 60.0;

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        stamps.Clear();

        if (!forest.Exists ||
            !forest.Ready ||
            Main.screenPosition.X + Main.screenWidth <
            forest.Left * 16f - 128 ||
            Main.screenPosition.X >
            forest.Right * 16f + 128)
            return;

        samples.Clear();

        var phase = (float)(elapsed * .14);

        SporeDreamRibbonPath.Append(
            forest.Left * 16f,
            (forest.Right - 1) * 16f,
            phase,
            x => SmoothGround(forest, x),
            TouchesStem,
            samples
        );

        // 整片森林共用一個霧氣強度週期。
        // Use one mist intensity cycle for the entire biome.
        var cycle =
            (float)(elapsed % 48.0) / 48f;

        var envelope = cycle >= .88f
            ? 0f
            : MathF.Sin(
                cycle / .88f * MathHelper.Pi
            );

        envelope *= envelope;

        foreach (var sample in samples)
        {
            var p = sample.Position;

            if (p.X < Main.screenPosition.X - 128 ||
                p.X > Main.screenPosition.X +
                Main.screenWidth + 128 ||
                p.Y < Main.screenPosition.Y - 128 ||
                p.Y > Main.screenPosition.Y +
                Main.screenHeight + 128)
                continue;

            var taper = Math.Clamp(
                Math.Min(
                    sample.Progress,
                    1f - sample.Progress
                ) * 18f,
                0f,
                1f
            );

            var density =
                7f / MathF.Sqrt(sample.Radius * 14f);

            stamps.Add(
                new Stamp(
                    p,
                    sample.Radius,
                    envelope * taper * density,
                    sample.Front
                )
            );
        }
    }

    /// <summary>
    ///     平滑取樣森林地表。
    ///     Smoothly sample the forest surface.
    /// </summary>
    private static float SmoothGround(
        SporeDreamEnvironmentSystem forest,
        float x)
    {
        var column = x / 16f - forest.Left;

        var center = (int)MathF.Floor(column);
        var fraction = column - center;

        var a = 0f;
        var b = 0f;

        for (var offset = -8; offset <= 8; offset++)
        {
            a += forest.Surface[
                Math.Clamp(
                    center + offset,
                    0,
                    forest.Surface.Length - 1
                )
            ];

            b += forest.Surface[
                Math.Clamp(
                    center + offset + 1,
                    0,
                    forest.Surface.Length - 1
                )
            ];
        }

        return (a + (b - a) * fraction) * 16f / 17f;
    }

    private static bool TouchesStem(Vector2 position)
    {
        var x = (int)MathF.Floor(position.X / 16f);
        var y = (int)MathF.Floor(position.Y / 16f);

        if (!WorldGen.InWorld(x, y, 1))
            return false;

        var tile = Main.tile[x, y];

        return tile.HasTile &&
               (tile.TileType ==
                ModContent.TileType<SporeDreamGiant>() ||
                tile.TileType ==
                ModContent.TileType<SporeDreamCurvedGiant>());
    }

    /// <summary>
    ///     建立一次性柔邊霧點貼圖。
    ///     Create the reusable soft mist texture.
    /// </summary>
    private void EnsureTexture()
    {
        if (mistTexture != null)
            return;

        const int size = 32;

        var pixels = new Color[size * size];

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var nx = (x + .5f) / size * 2f - 1f;
            var ny = (y + .5f) / size * 2f - 1f;

            var falloff = Math.Max(
                0f,
                1f - nx * nx - ny * ny
            );

            pixels[y * size + x] =
                Color.White * (falloff * falloff);
        }

        mistTexture = new Texture2D(
            Main.instance.GraphicsDevice,
            size,
            size
        );

        mistTexture.SetData(pixels);
    }

    public override void PostDrawTiles()
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (Main.gameMenu ||
            !forest.Exists ||
            !forest.Ready ||
            stamps.Count == 0)
            return;

        var column = Math.Clamp(
            (int)(Main.LocalPlayer.Center.X / 16f)
            - forest.Left,
            0,
            forest.Surface.Length - 1
        );

        var blend =
            SporeDreamBackgroundLayout.SurfaceVisibility(
                Main.LocalPlayer.Center.Y,
                forest.Surface[column] * 16f
            );

        blend *=
            ModContent.GetInstance<SporeDreamVisuals>().Blend;

        if (blend <= 0f)
            return;

        EnsureTexture();
        restoreStems.Clear();

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        try
        {
            // 先繪製位於菇柄後方的霧。
            // Draw mist behind mushroom stems first.
            foreach (var stamp in stamps)
            {
                if (stamp.Front)
                    continue;

                DrawStamp(stamp, blend * .76f);

                var radius =
                    (int)Math.Ceiling(
                        stamp.Radius * 1.9f
                    ) + 18;

                var left = (int)(
                    (stamp.Position.X - radius) / 16f
                );

                var right = (int)(
                    (stamp.Position.X + radius) / 16f
                );

                var top = (int)(
                    (stamp.Position.Y - radius) / 16f
                );

                var bottom = (int)(
                    (stamp.Position.Y + radius) / 16f
                );

                for (var x = left; x <= right; x++)
                for (var y = top; y <= bottom; y++)
                {
                    if (!WorldGen.InWorld(x, y, 1))
                        continue;

                    var tile = Main.tile[x, y];

                    if (tile.HasTile &&
                        ((tile.TileType ==
                          ModContent.TileType<SporeDreamGiant>() &&
                          tile.TileFrameY != 0) ||
                         tile.TileType ==
                         ModContent.TileType<SporeDreamCurvedGiant>()))
                        restoreStems.Add(
                            new Point(x, y)
                        );
                }
            }

            // 重新繪製菇柄，使霧氣呈現前後遮擋。
            // Redraw stems between the mist layers.
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            foreach (var point in restoreStems)
                if (Main.tile[point.X, point.Y].TileType ==
                    ModContent.TileType<SporeDreamCurvedGiant>())
                    SporeDreamCurvedSystem.Instance.DrawAt(
                        point.X,
                        point.Y,
                        Main.spriteBatch,
                        Vector2.Zero,
                        true
                    );
                else
                    SporeDreamGiant.DrawStemTile(
                        point.X,
                        point.Y,
                        Main.spriteBatch,
                        Vector2.Zero
                    );

            Main.spriteBatch.End();

            // 最後繪製位於菇柄前方的霧。
            // Finally draw the foreground mist.
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            foreach (var stamp in stamps)
                if (stamp.Front)
                    DrawStamp(stamp, blend);
        }
        finally
        {
            Main.spriteBatch.End();
        }
    }

    private void DrawStamp(Stamp stamp, float blend)
    {
        var position =
            stamp.Position - Main.screenPosition;

        if (position.X < -128 ||
            position.Y < -128 ||
            position.X > Main.screenWidth + 128 ||
            position.Y > Main.screenHeight + 128)
            return;

        Color color = new(231, 199, 239);

        Main.spriteBatch.Draw(
            mistTexture,
            position,
            null,
            color * (stamp.Opacity * blend * .06f),
            0f,
            new Vector2(16f),
            stamp.Radius * 3.8f / 32f,
            SpriteEffects.None,
            0f
        );

        Main.spriteBatch.Draw(
            mistTexture,
            position,
            null,
            color * (stamp.Opacity * blend * .24f),
            0f,
            new Vector2(16f),
            stamp.Radius * 2f / 32f,
            SpriteEffects.None,
            0f
        );
    }

    public override void Unload()
    {
        var texture = mistTexture;

        mistTexture = null;

        ClearWorld();

        if (texture != null)
            Main.QueueMainThreadAction(() => texture.Dispose()
            );
    }

    private readonly record struct Stamp(
        Vector2 Position,
        float Radius,
        float Opacity,
        bool Front
    );
}