using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Occident.Common.Worlds;

[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamSkyBackground : ModSystem
{
    public override void Load() => On_Main.DrawStarsInBackground += DrawSky;
    public override void Unload() => On_Main.DrawStarsInBackground -= DrawSky;

    private static void DrawSky(On_Main.orig_DrawStarsInBackground orig, Main self,
        Main.SceneArea sceneArea, bool artificial)
    {
        var visuals = ModContent.GetInstance<SporeDreamVisuals>();
        var forest=SporeDreamForestSystem.Instance;
        float surfaceVisibility=0f;
        if(forest.Exists) {
            int column=System.Math.Clamp((int)(Main.LocalPlayer.Center.X/16f)-forest.Left,0,forest.Surface.Length-1);
            surfaceVisibility=SporeDreamBackgroundLayout.SurfaceVisibility(Main.LocalPlayer.Center.Y,forest.Surface[column]*16f);
        }
        if (!Main.gameMenu && visuals.Blend > 0f
            && surfaceVisibility>0f
            && Main.screenPosition.Y < Main.worldSurface * 16.0 + 16.0)
        {
            // Vanilla's blue gradient has already been drawn here. Replace its
            // color before stars, sun/moon, clouds, biome art and terrain draw.
            var (top, horizon) = visuals.SkyPalette;
            const int stripHeight = 8;
            for (int y = 0; y < Main.screenHeight; y += stripHeight)
            {
                float amount = y / (float)System.Math.Max(1, Main.screenHeight - 1);
                Color color = Color.Lerp(top, horizon, amount) * (visuals.Blend*surfaceVisibility);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(0, y, Main.screenWidth, System.Math.Min(stripHeight, Main.screenHeight - y)), color);
            }
        }
        orig(self, sceneArea, artificial);
    }
}
