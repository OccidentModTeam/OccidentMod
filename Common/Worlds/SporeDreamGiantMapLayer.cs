// 以素材透明輪廓補畫已探索的巨菇地圖圖形。
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using Occident.Content.Tiles.SporeDream;
using G=Occident.Common.Worlds.SporeDreamGrowthPath;

namespace Occident.Common.Worlds;

[Autoload(Side=ModSide.Client)]
public sealed class SporeDreamGiantMapLayer : ModMapLayer
{
    private const string Root="OccidentMod/Assets/Sprites/Environment/SporeDreamForest/";
    private readonly Dictionary<Point,bool> ink=new();
    private readonly Dictionary<string,(Color[] Pixels,int Width)> sources=new();
    private int revision=-1,world=-1;
    private ulong refreshed;
    private Texture2D mapPixel;
    public override Position GetDefaultPosition()=>BeforeFirstVanillaLayer;
    private (Color[] Pixels,int Width) Source(string name)
    {
        if(sources.TryGetValue(name,out var cached))return cached;
        var texture=ModContent.Request<Texture2D>(Root+name).Value;
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        return sources[name]=(pixels,texture.Width);
    }
    private void Stamp(G.Piece p)
    {
        string name;Rectangle frame;
        switch(p.Kind) {
            case G.Crown:name="fleshy_mushroom_caps";frame=SporeDreamGiantLayout.CapFrame(p.Style);break;
            case G.Vertical:name="amanita_crema_tiles_02";frame=SporeDreamGiantLayout.StemFrame(p.Style,p.Extra==1,0,20,p.Direction==G.Down);break;
            case G.Horizontal:name="amanita_crema_tiles";frame=SporeDreamGiantLayout.HorizontalFrame(p.Style,p.Extra==1,p.Direction==G.Right);break;
            case G.Turn:name="amanita_crema_tiles_corner";frame=new(G.CornerIndex(p.Extra,p.Direction)*22,p.Style%3*22,20,20);break;
            default:name="amanita_crema_tiles_top";frame=SporeDreamGiantLayout.CutStemFrame(p.Style,0,24);break;
        }
        var source=Source(name);
        for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++) {
            if(source.Pixels[(frame.Y+y)*source.Width+frame.X+x].A==0)continue;
            var pixel=SporeDreamMapGeometry.Pixel(p,x,y);var tile=SporeDreamMapGeometry.Tile(pixel.X,pixel.Y);
            Point point=new(tile.X,tile.Y);
            if(!WorldGen.InWorld(point.X,point.Y,1))continue;
            ink.TryGetValue(point,out bool cap);ink[point]=cap || p.Kind==G.Crown;
        }
    }
    // 幾何或世界變動時重建輪廓快取，避免每格每幀重讀貼圖。
    private void Refresh()
    {
        ink.Clear();
        var curved=SporeDreamCurvedSystem.Instance;
        foreach(var p in curved.MapPieces())Stamp(p);
        // 舊直柱菇柄已占有實體地圖格，只需補上向外延伸的菌蓋輪廓。
        var forest=SporeDreamForestSystem.Instance;
        if(forest.Exists)for(int x=forest.Left;x<forest.Right;x++)
            for(int y=Math.Max(1,forest.Surface[x-forest.Left]-64);y<forest.Surface[x-forest.Left];y++) {
                Tile tile=Main.tile[x,y];
                if(tile.HasTile && tile.TileType==ModContent.TileType<SporeDreamGiant>() && tile.TileFrameY==0 && SporeDreamGiant.IsPrimaryTile(x,y))
                    Stamp(new(G.Crown,(int)SporeDreamGiant.StemCenterX(x,y),(y+1)*16,G.Up,SporeDreamGiant.CapStyle(x,y)));
            }
        revision=curved.MapRevision;world=Main.worldID;refreshed=Main.GameUpdateCount;
    }
    public override void Draw(ref MapOverlayDrawContext context,ref string text)
    {
        if(Main.gameMenu || Main.Map is null)return;
        if(world!=Main.worldID || revision!=SporeDreamCurvedSystem.Instance.MapRevision || Main.GameUpdateCount-refreshed>120)Refresh();
        // 原版 MagicPixel 為 1×1000；直接繪製整張會把地圖格拉成長線，因此另建 1×1 貼圖。
        if(mapPixel is null) {
            mapPixel=new Texture2D(Main.instance.GraphicsDevice,1,1);
            mapPixel.SetData(new[]{Color.White});
        }
        var texture=mapPixel;
        float scale=context.MapScale/context.DrawScale;
        foreach(var entry in ink) {
            Point p=entry.Key;byte light=Main.Map[p.X,p.Y].Light;
            if(light==0)continue; // 只繪製已探索區域，避免提前洩露菌蓋與群系位置。
            Vector2 tile=new(p.X,p.Y);
            Vector2 screen=(tile-context.MapPosition)*context.MapScale+context.MapOffset;
            if(context.ClippingRectangle is Rectangle clip && !clip.Contains(screen.ToPoint()))continue;
            Color color=(entry.Value?new Color(164,98,103):new Color(142,108,106))*(light/255f);
            var result=context.Draw(texture,tile,color,new SpriteFrame(1,1),scale,scale,Alignment.TopLeft);
            if(result.IsMouseOver)text=Language.GetTextValue("Mods.OccidentMod.SporeDream.GiantMushroom");
        }
    }
    public override void Unload()
    {
        ink.Clear();sources.Clear();
        var oldPixel=mapPixel;mapPixel=null;
        if(oldPixel is not null)Main.QueueMainThreadAction(()=>oldPixel.Dispose());
    }
}
