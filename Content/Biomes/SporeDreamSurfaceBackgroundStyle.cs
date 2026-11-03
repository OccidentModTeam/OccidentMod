using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using Occident.Common.Worlds;

namespace Occident.Content.Biomes;

[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamSurfaceBackgroundStyle : ModSurfaceBackgroundStyle
{
    public const string TexturePath = "OccidentMod/Assets/Backgrounds/SporeDreamForest/SporeDreamForest";
    private const float Parallax = 0.2f;
    private Asset<Texture2D> background;
    private int[] anchoredSurface;
    private float anchorGroundY;
    private Color lowerGroundColor;
    private bool sampledGroundColor;

    public override void ModifyFarFades(float[] fades, float transitionSpeed)
    {
        for (int i = 0; i < fades.Length; i++)
            fades[i] = MathHelper.Clamp(fades[i] + (i == Slot ? transitionSpeed : -transitionSpeed), 0f, 1f);
    }

    public override bool PreDrawCloseBackground(SpriteBatch spriteBatch)
    {
        if (Main.gameMenu)
            return false;

        float opacity = Main.bgAlphaFrontLayer[Slot];
        if (opacity <= 0f)
            return false;

        background ??= ModContent.Request<Texture2D>(TexturePath);
        Texture2D texture = background.Value;
        var forest = SporeDreamForestSystem.Instance;
        if (!forest.Exists)
            return false;

        int column = Math.Clamp((int)(Main.LocalPlayer.Center.X / 16f) - forest.Left, 0, forest.Surface.Length - 1);
        opacity *= SporeDreamBackgroundLayout.SurfaceVisibility(Main.LocalPlayer.Center.Y,forest.Surface[column]*16f);
        if(opacity<=0f) return false;

        // A fixed biome-wide anchor prevents a deep pit under the player from
        // pulling the panorama up or down. Refresh only when world data changes.
        if(!ReferenceEquals(anchoredSurface,forest.Surface)) {
            anchoredSurface=forest.Surface;
            anchorGroundY=SporeDreamBackgroundLayout.RepresentativeGround(anchoredSurface);
        }
        float progress=(Main.LocalPlayer.Center.X/16f-forest.Left)/forest.Surface.Length;
        var layout=SporeDreamBackgroundLayout.Place(texture.Width,texture.Height,
            Main.screenWidth,Main.screenHeight,anchorGroundY,Main.screenPosition.Y,progress);
        Color tint = ModContent.GetInstance<SporeDreamVisuals>().BackgroundTint(opacity);

        SkyManager.Instance.DrawToDepth(spriteBatch, 1f / Parallax);
        spriteBatch.Draw(texture, new Rectangle(layout.Left,layout.Top,layout.Width,layout.Height),
            null, tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        DrawLowerGround(spriteBatch,texture,layout,tint,opacity);

        return false;
    }

    private void DrawLowerGround(SpriteBatch batch,Texture2D texture,
        SporeDreamBackgroundLayout.Panorama layout,Color tint,float opacity)
    {
        if(!sampledGroundColor) {
            // Pick the dominant opaque ground color from the original lower
            // quarter once. Do not stretch a skull or repeat the bottom row.
            int height=Math.Max(1,texture.Height/4);
            var pixels=new Color[texture.Width*height];
            texture.GetData(0,new Rectangle(0,texture.Height-height,texture.Width,height),pixels,0,pixels.Length);
            var counts=new System.Collections.Generic.Dictionary<uint,int>();
            int most=0;
            foreach(Color color in pixels) {
                if(color.A<240) continue;
                uint key=color.PackedValue;
                counts.TryGetValue(key,out int count);
                counts[key]=++count;
                if(count>most) { most=count; lowerGroundColor=new Color(color.R,color.G,color.B); }
            }
            if(most==0) lowerGroundColor=new Color(185,157,166);
            sampledGroundColor=true;
        }
        Color ground=new Color(lowerGroundColor.R*tint.R/255,
            lowerGroundColor.G*tint.G/255,lowerGroundColor.B*tint.B/255);
        int fogHeight=Math.Max(1,layout.Bottom-layout.FogStart);
        int end=Math.Min(Main.screenHeight,layout.Bottom);
        for(int y=Math.Max(0,layout.FogStart);y<end;y+=8) {
            int height=Math.Min(8,end-y);
            float blend=MathHelper.SmoothStep(0f,1f,
                MathHelper.Clamp((y+height-layout.FogStart)/(float)fogHeight,0f,1f));
            batch.Draw(TextureAssets.MagicPixel.Value,new Rectangle(0,y,Main.screenWidth,height),ground*(opacity*blend));
        }
        int fillTop=Math.Max(0,layout.Bottom);
        if(fillTop<Main.screenHeight)
            batch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0,fillTop,Main.screenWidth,Main.screenHeight-fillTop),ground*opacity);
    }

    public override void Unload()
    {
        background=null;
        anchoredSurface=null;
        sampledGroundColor=false;
    }
}
