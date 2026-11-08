#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.World.SporeDreamForest;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace OccidentMod.World.Biomes;

/// <summary>
///     孢夢之森專屬地表背景。
///     Spore Dream Forest surface background.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamSurfaceBackgroundStyle
    : ModSurfaceBackgroundStyle
{
    public const string TexturePath =
        "OccidentMod/Assets/Backgrounds/SporeDreamForest";

    private const int ExitFadeDistance = 48;
    private float anchorGroundY;

    private int[]? anchoredSurface;

    private Asset<Texture2D>? background;

    private Color lowerGroundColor;
    private bool sampledGroundColor;

    /// <summary>
    ///     交由原版背景系統處理圖層的過渡。
    ///     Let Terraria handle background layer transitions.
    /// </summary>
    public override void ModifyFarFades(
        float[] fades,
        float transitionSpeed)
    {
        for (var i = 0; i < fades.Length; i++)
            fades[i] = MathHelper.Clamp(
                fades[i] +
                (i == Slot
                    ? transitionSpeed
                    : -transitionSpeed),
                0f,
                1f
            );
    }

    /// <summary>
    ///     計算玩家與森林水平邊界的距離。
    ///     Calculate distance outside the forest boundaries.
    /// </summary>
    private static float BoundaryVisibility(
        float playerTileX,
        int left,
        int right)
    {
        var distance = 0f;

        if (playerTileX < left)
            distance = left - playerTileX;
        else if (playerTileX >= right) distance = playerTileX - right + 1f;

        var t = MathHelper.Clamp(
            distance / ExitFadeDistance,
            0f,
            1f
        );

        // 平滑衰減，不會在邊界突然閃爍。
        // Smooth fade without sudden boundary flickering.
        return 1f - t * t * (3f - 2f * t);
    }

    /// <summary>
    ///     計算背景的最終透明度。
    ///     Calculate final panorama opacity.
    /// </summary>
    private float CalculateOpacity(
        SporeDreamEnvironmentSystem forest)
    {
        if (Slot < 0 ||
            Slot >= Main.bgAlphaFrontLayer.Length)
            return 0f;

        var visuals =
            ModContent.GetInstance<SporeDreamVisuals>();

        // 修正重點：
        // 不可只依賴 Terraria 的背景圖層透明度。
        //
        // Important:
        // Vanilla background alpha alone does not indicate
        // whether the player is still in this biome.
        var vanillaAlpha =
            Main.bgAlphaFrontLayer[Slot];

        var biomeAlpha = visuals.Blend;

        var playerTileX =
            Main.LocalPlayer.Center.X / 16f;

        var boundaryAlpha = BoundaryVisibility(
            playerTileX,
            forest.Left,
            forest.Right
        );

        return MathHelper.Clamp(
            vanillaAlpha * biomeAlpha * boundaryAlpha,
            0f,
            1f
        );
    }

    /// <summary>
    ///     繪製森林全景及下方漸層。
    ///     Draw the forest panorama and lower gradient.
    /// </summary>
    public override bool PreDrawCloseBackground(
        SpriteBatch spriteBatch)
    {
        if (Main.gameMenu)
            return false;

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists ||
            !forest.Ready ||
            forest.Surface == null ||
            forest.Surface.Length == 0)
            return false;

        var opacity = CalculateOpacity(forest);

        // 透明度已完全消失，不再繪製森林背景。
        // Stop rendering once the forest has faded out.
        if (opacity <= 0.001f)
            return false;

        background ??=
            ModContent.Request<Texture2D>(TexturePath);

        var texture = background.Value;

        var column = Math.Clamp(
            (int)(
                Main.LocalPlayer.Center.X / 16f
            ) - forest.Left,
            0,
            forest.Surface.Length - 1
        );

        opacity *=
            SporeDreamBackgroundLayout.SurfaceVisibility(
                Main.LocalPlayer.Center.Y,
                forest.Surface[column] * 16f
            );

        if (opacity <= 0.001f)
            return false;

        // 森林地形資料變更時重新計算錨點。
        // Recalculate anchor when forest terrain changes.
        if (!ReferenceEquals(
                anchoredSurface,
                forest.Surface))
        {
            anchoredSurface = forest.Surface;

            anchorGroundY =
                SporeDreamBackgroundLayout
                    .RepresentativeGround(anchoredSurface);
        }

        var progress =
            (Main.LocalPlayer.Center.X / 16f -
             forest.Left) /
            forest.Surface.Length;

        var layout =
            SporeDreamBackgroundLayout.Place(
                texture.Width,
                texture.Height,
                Main.screenWidth,
                Main.screenHeight,
                anchorGroundY,
                Main.screenPosition.Y,
                progress
            );

        var tint =
            ModContent.GetInstance<SporeDreamVisuals>()
                .BackgroundTint(opacity);

        spriteBatch.Draw(
            texture,
            new Rectangle(
                layout.Left,
                layout.Top,
                layout.Width,
                layout.Height
            ),
            null,
            tint,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0f
        );

        DrawLowerGround(
            spriteBatch,
            texture,
            layout,
            tint,
            opacity
        );

        return false;
    }

    /// <summary>
    ///     繪製背景下方的漸層地色。
    ///     Draw the lower-ground color and gradient.
    /// </summary>
    private void DrawLowerGround(
        SpriteBatch batch,
        Texture2D texture,
        SporeDreamBackgroundLayout.Panorama layout,
        Color tint,
        float opacity)
    {
        if (!sampledGroundColor)
        {
            var height = Math.Max(
                1,
                texture.Height / 4
            );

            var pixels =
                new Color[texture.Width * height];

            texture.GetData(
                0,
                new Rectangle(
                    0,
                    texture.Height - height,
                    texture.Width,
                    height
                ),
                pixels,
                0,
                pixels.Length
            );

            Dictionary<uint, int> counts = new();

            var most = 0;

            foreach (var color in pixels)
            {
                if (color.A < 240)
                    continue;

                var key = color.PackedValue;

                counts.TryGetValue(
                    key,
                    out var count
                );

                counts[key] = ++count;

                if (count > most)
                {
                    most = count;

                    lowerGroundColor = new Color(
                        color.R,
                        color.G,
                        color.B
                    );
                }
            }

            if (most == 0)
                lowerGroundColor =
                    new Color(185, 157, 166);

            sampledGroundColor = true;
        }

        Color ground = new(
            lowerGroundColor.R * tint.R / 255,
            lowerGroundColor.G * tint.G / 255,
            lowerGroundColor.B * tint.B / 255
        );

        var fogHeight = Math.Max(
            1,
            layout.Bottom - layout.FogStart
        );

        var end = Math.Min(
            Main.screenHeight,
            layout.Bottom
        );

        for (var y = Math.Max(0, layout.FogStart);
             y < end;
             y += 8)
        {
            var height = Math.Min(8, end - y);

            var blend = MathHelper.SmoothStep(
                0f,
                1f,
                MathHelper.Clamp(
                    (y + height - layout.FogStart) /
                    (float)fogHeight,
                    0f,
                    1f
                )
            );

            batch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle(
                    0,
                    y,
                    Main.screenWidth,
                    height
                ),
                ground * (opacity * blend)
            );
        }

        var fillTop = Math.Max(
            0,
            layout.Bottom
        );

        if (fillTop < Main.screenHeight)
            batch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle(
                    0,
                    fillTop,
                    Main.screenWidth,
                    Main.screenHeight - fillTop
                ),
                ground * opacity
            );
    }

    public override void Unload()
    {
        background = null;
        anchoredSurface = null;

        sampledGroundColor = false;
        lowerGroundColor = default;
        anchorGroundY = 0f;
    }
}