using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森天空漸層。
///     Spore Dream Forest sky gradient.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamSkyBackground : ModSystem
{
    public override void Load()
    {
        On_Main.DrawStarsInBackground += DrawSky;
    }

    public override void Unload()
    {
        On_Main.DrawStarsInBackground -= DrawSky;
    }

    /// <summary>
    ///     在原版天空與星星之間疊加森林色彩。
    ///     Overlay the forest palette before drawing stars.
    /// </summary>
    private static void DrawSky(
        On_Main.orig_DrawStarsInBackground orig,
        Main self,
        Main.SceneArea sceneArea,
        bool artificial)
    {
        var visuals =
            ModContent.GetInstance<SporeDreamVisuals>();

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        var visibility = 0f;

        if (forest.Exists && forest.Ready)
        {
            var column = Math.Clamp(
                (int)(Main.LocalPlayer.Center.X / 16f)
                - forest.Left,
                0,
                forest.Surface.Length - 1
            );

            visibility =
                SporeDreamBackgroundLayout.SurfaceVisibility(
                    Main.LocalPlayer.Center.Y,
                    forest.Surface[column] * 16f
                );
        }

        if (!Main.gameMenu &&
            visuals.Blend > 0f &&
            visibility > 0f &&
            Main.screenPosition.Y <
            Main.worldSurface * 16.0 + 16.0)
        {
            var (top, horizon) = visuals.SkyPalette;

            const int stripHeight = 8;

            var opacity =
                visuals.Blend * visibility;

            for (var y = 0;
                 y < Main.screenHeight;
                 y += stripHeight)
            {
                var amount = y /
                             (float)Math.Max(
                                 1,
                                 Main.screenHeight - 1
                             );

                var color =
                    Color.Lerp(
                        top,
                        horizon,
                        amount
                    ) * opacity;

                Main.spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle(
                        0,
                        y,
                        Main.screenWidth,
                        Math.Min(
                            stripHeight,
                            Main.screenHeight - y
                        )
                    ),
                    color
                );
            }
        }

        orig(self, sceneArea, artificial);
    }
}