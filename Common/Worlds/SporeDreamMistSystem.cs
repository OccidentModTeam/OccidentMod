// 建立流動霧帶，分前後層繪製並讓菇柄遮住後方霧氣。
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Occident.Content.Tiles.SporeDream;

namespace Occident.Common.Worlds;

[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamMistSystem : ModSystem
{
    private readonly record struct Stamp(Vector2 Position,float Radius,float Opacity,bool Front);
    private readonly List<Stamp> stamps=new(4096);
    private readonly List<SporeDreamRibbonPath.Sample> samples=new(2048);
    private readonly HashSet<Point> restoreStems=new();
    private Texture2D mistTexture;
    private double elapsed;

    public override void ClearWorld()
    {
        elapsed=0;stamps.Clear();samples.Clear();restoreStems.Clear();
    }
    public override void PostUpdateEverything()
    {
        if(Main.gameMenu) {ClearWorld();return;}
        if(Main.gamePaused) return;
        elapsed+=1.0/60.0;
        var forest=SporeDreamForestSystem.Instance;
        stamps.Clear();
        if(!forest.Exists || Main.screenPosition.X+Main.screenWidth<forest.Left*16f-128 ||
           Main.screenPosition.X>forest.Right*16f+128) return;
        samples.Clear();
        float phase=(float)(elapsed*.14);
        SporeDreamRibbonPath.Append(forest.Left*16f,(forest.Right-1)*16f,phase,
            x=>SmoothGround(forest,x),TouchesStem,samples);
        // 整片群系共用一道移動的霧氣強度曲線，不為每株菇建立發射器。
        float cycle=(float)(elapsed%48.0)/48f;
        float envelope=cycle>=.88f?0f:MathF.Sin(cycle/.88f*MathHelper.Pi);
        envelope*=envelope;
        foreach(var sample in samples) {
            Vector2 p=sample.Position;
            if(p.X<Main.screenPosition.X-128 || p.X>Main.screenPosition.X+Main.screenWidth+128 ||
               p.Y<Main.screenPosition.Y-128 || p.Y>Main.screenPosition.Y+Main.screenHeight+128) continue;
            float taper=Math.Clamp(Math.Min(sample.Progress,1f-sample.Progress)*18f,0f,1f);
            // 寬霧段降低密度，維持霧氣的柔軟感，避免成為不透明管狀。
            float density=7f/MathF.Sqrt(sample.Radius*14f);
            stamps.Add(new(p,sample.Radius,envelope*taper*density,sample.Front));
        }
    }
    private static float SmoothGround(SporeDreamForestSystem forest,float x)
    {
        float column=x/16f-forest.Left;
        int center=(int)MathF.Floor(column);
        float fraction=column-center;
        float a=0,b=0;
        for(int offset=-8;offset<=8;offset++) {
            a+=forest.Surface[Math.Clamp(center+offset,0,forest.Surface.Length-1)];
            b+=forest.Surface[Math.Clamp(center+offset+1,0,forest.Surface.Length-1)];
        }
        return (a+(b-a)*fraction)*16f/17f;
    }
    private static bool TouchesStem(Vector2 position)
    {
        int x=(int)MathF.Floor(position.X/16),y=(int)MathF.Floor(position.Y/16);
        if(!WorldGen.InWorld(x,y,1)) return false;
        Tile tile=Main.tile[x,y];
        return tile.HasTile && (tile.TileType==ModContent.TileType<SporeDreamGiant>()
            || tile.TileType==ModContent.TileType<SporeDreamCurvedGiant>());
    }
    // 只建立一次柔邊霧點貼圖，後續以位置、尺寸與透明度重複繪製。
    private void EnsureTexture()
    {
        if(mistTexture!=null) return;
        const int size=32;
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            float nx=(x+.5f)/size*2-1,ny=(y+.5f)/size*2-1;
            float falloff=Math.Max(0f,1-nx*nx-ny*ny);
            pixels[y*size+x]=Color.White*(falloff*falloff);
        }
        mistTexture=new Texture2D(Main.instance.GraphicsDevice,size,size);
        mistTexture.SetData(pixels);
    }
    public override void PostDrawTiles()
    {
        var forest=SporeDreamForestSystem.Instance;
        float blend=1f;
        if(Main.gameMenu || !forest.Exists || blend<=0 || stamps.Count==0) return;
        int column=Math.Clamp((int)(Main.LocalPlayer.Center.X/16)-forest.Left,0,forest.Surface.Length-1);
        blend*=SporeDreamBackgroundLayout.SurfaceVisibility(Main.LocalPlayer.Center.Y,forest.Surface[column]*16f);
        if(blend<=0) return;
        EnsureTexture();restoreStems.Clear();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
            DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        try {
            foreach(Stamp stamp in stamps)if(!stamp.Front) {
                DrawStamp(stamp,blend*.76f);
                int radius=(int)Math.Ceiling(stamp.Radius*1.9f)+18;
                for(int x=(int)((stamp.Position.X-radius)/16);x<=(int)((stamp.Position.X+radius)/16);x++)
                    for(int y=(int)((stamp.Position.Y-radius)/16);y<=(int)((stamp.Position.Y+radius)/16);y++) {
                        if(!WorldGen.InWorld(x,y,1)) continue;
                        Tile tile=Main.tile[x,y];
                        if(tile.HasTile && ((tile.TileType==ModContent.TileType<SporeDreamGiant>() && tile.TileFrameY!=0)
                            || tile.TileType==ModContent.TileType<SporeDreamCurvedGiant>()))
                            restoreStems.Add(new Point(x,y));
                    }
            }
            // 前後霧層之間重繪原菇柄以形成遮擋；動態霧不寫入物塊快取或來源圖片。
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,
                DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
            foreach(Point tile in restoreStems) {
                if(Main.tile[tile.X,tile.Y].TileType==ModContent.TileType<SporeDreamCurvedGiant>())
                    SporeDreamCurvedSystem.Instance.DrawAt(tile.X,tile.Y,Main.spriteBatch,Vector2.Zero,true);
                else SporeDreamGiant.DrawStemTile(tile.X,tile.Y,Main.spriteBatch,Vector2.Zero);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
            foreach(Stamp stamp in stamps)if(stamp.Front) DrawStamp(stamp,blend);
        }
        finally {Main.spriteBatch.End();}
    }
    private void DrawStamp(Stamp stamp,float blend)
    {
        Vector2 position=stamp.Position-Main.screenPosition;
        if(position.X< -128 || position.Y< -128 || position.X>Main.screenWidth+128 || position.Y>Main.screenHeight+128) return;
        Color color=new(231,199,239);
        Main.spriteBatch.Draw(mistTexture,position,null,color*(stamp.Opacity*blend*.06f),0,new Vector2(16),stamp.Radius*3.8f/32,SpriteEffects.None,0);
        Main.spriteBatch.Draw(mistTexture,position,null,color*(stamp.Opacity*blend*.24f),0,new Vector2(16),stamp.Radius*2/32,SpriteEffects.None,0);
    }
    public override void Unload()
    {
        Texture2D texture=mistTexture;mistTexture=null;ClearWorld();
        if(texture!=null) Main.QueueMainThreadAction(()=>texture.Dispose());
    }
}
