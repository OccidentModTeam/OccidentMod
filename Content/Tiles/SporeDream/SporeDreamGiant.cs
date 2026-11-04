// 相容既有直柱巨菇物塊，負責素材繪製、截斷與剩餘菇柄框架。
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Occident.Common.Worlds;

namespace Occident.Content.Tiles.SporeDream;

public sealed class SporeDreamGiant : ModTile
{
    private const string Root="OccidentMod/Assets/Sprites/Environment/SporeDreamForest/";
    // 舊單欄存檔的框架 X 為 0、18、36；雙欄版使用不同標記，無須重寫世界即可辨識舊植株。
    private const int SplitFrameBase=108;
    private const int SingleFrameBase=216;
    private static bool felling;
    public static readonly Rectangle[] CapFrames={SporeDreamGiantLayout.CapFrame(0),SporeDreamGiantLayout.CapFrame(1),SporeDreamGiantLayout.CapFrame(2)};
    public override string Texture => Root+"amanita_crema_tiles_02";
    private static bool Split(Tile t) => t.TileFrameX>=SplitFrameBase && t.TileFrameX<SplitFrameBase+108;
    private static int Half(Tile t) => Split(t)?((t.TileFrameX-SplitFrameBase)/18)%2:0;
    private static int PrimaryX(int x,int y) => x-Half(Main.tile[x,y]);
    public static bool IsPrimaryTile(int x,int y) => Half(Main.tile[x,y])==0;
    public static bool IsPairedTile(int x,int y) => Split(Main.tile[x,y]);
    public static float StemCenterX(int x,int y)=>PrimaryX(x,y)*16+(Split(Main.tile[x,y])?16:8);
    public static int CapStyle(int x,int y)
    {
        Tile t=Main.tile[x,y];
        return t.TileFrameX>=SingleFrameBase?(t.TileFrameX-SingleFrameBase)/18:
            Split(t)?(t.TileFrameX-SplitFrameBase)/36:(t.TileFrameX/18)%3;
    }
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type]=true;Main.tileAxe[Type]=true;Main.tileHammer[Type]=false;Main.tileLavaDeath[Type]=true;
        Main.tileNoFail[Type]=false;Main.tileNoAttach[Type]=true;
        // 巨菇柄比照樹幹使用斧頭砍伐。
        MinPick=0;MineResist=1f;
        AddMapEntry(new Color(156,83,83),Language.GetText("Mods.OccidentMod.SporeDream.GiantMushroom"));DustType=DustID.WoodFurniture;
        RegisterItemDrop(ItemID.Mushroom);
    }
    public static bool TryGrow(int x,int ground)
        => SporeDreamCurvedSystem.Instance.TryFloor(x,ground);
    public static void GetStemBounds(int x,int y,out int top,out int bottom,out int bodyTop)
    {
        x=PrimaryX(x,y);
        int type=ModContent.TileType<SporeDreamGiant>();
        top=y;bottom=y+1;
        while(top>1 && Main.tile[x,top-1].HasTile && Main.tile[x,top-1].TileType==type) top--;
        while(bottom<Main.maxTilesY-1 && Main.tile[x,bottom].HasTile && Main.tile[x,bottom].TileType==type) bottom++;
        bodyTop=top+(Main.tile[x,top].TileFrameY==0?1:0);
    }
    public static bool HasDecoratedStemAt(int x,int y)
    {
        x=PrimaryX(x,y);
        GetStemBounds(x,y,out int top,out int bottom,out int bodyTop);
        int pixel=(y-bodyTop)*16;
        if(pixel<0) return false;
        int pixels=(bottom-bodyTop)*16;
        int first=pixel/SporeDreamGiantLayout.StemHeight,last=(pixel+15)/SporeDreamGiantLayout.StemHeight;
        return SporeDreamGiantLayout.Decorated(x,top,pixels,first,Main.worldID)
            || SporeDreamGiantLayout.Decorated(x,top,pixels,last,Main.worldID);
    }
    public static void DrawStemTile(int i,int j,SpriteBatch spriteBatch,Vector2 offset)
    {
        Tile tile=Main.tile[i,j];
        if(!IsPrimaryTile(i,j))return;
        int x=PrimaryX(i,j);
        GetStemBounds(x,j,out int top,out int bottom,out int bodyTop);
        int startPixel=(j-bodyTop)*16;
        if(startPixel<0) return;
        int bodyPixels=(bottom-bodyTop)*16;
        var texture=ModContent.Request<Texture2D>(Root+"amanita_crema_tiles_02").Value;
        bool cutTop=Main.tile[x,top].TileFrameY!=0;
        // 截斷後保留剩餘物塊資料，由現有菇柄判定斷頂，確保存檔與霧層重繪使用同一端點。
        var cutTexture=cutTop && startPixel<SporeDreamGiantLayout.StemHeight
            ?ModContent.Request<Texture2D>(Root+"amanita_crema_tiles_top").Value:null;
        int cutStyle=SporeDreamGiantLayout.Style(x,top,1,Main.worldID);
        Color color=Lighting.GetColor(i,j);
        var slots=SporeDreamGiantLayout.DecorationSlots(x,top,bodyPixels,Main.worldID);
        {
            int pixel=startPixel,remaining=16,destinationY=0;
            Vector2 position=new(StemCenterX(i,j)-8,j*16);
            if(cutTop && j==top) {
                Rectangle tip=SporeDreamGiantLayout.CutStemFrame(cutStyle,0,SporeDreamGiantLayout.CutTopExtension);
                spriteBatch.Draw(cutTexture,position-new Vector2(0,SporeDreamGiantLayout.CutTopExtension)-Main.screenPosition+offset,tip,color);
            }
            while(remaining>0) {
                int segment=pixel/SporeDreamGiantLayout.StemHeight;
                int height=SporeDreamGiantLayout.SliceHeight(pixel,remaining);
                int style=SporeDreamGiantLayout.Style(x,top,segment,Main.worldID);
                bool decorated=segment==slots.First || segment==slots.Second;
                Rectangle frame=SporeDreamGiantLayout.StemFrame(style,decorated,pixel%SporeDreamGiantLayout.StemHeight,height);
                bool capped=cutTop && segment==0;
                if(capped) frame=SporeDreamGiantLayout.CutStemFrame(cutStyle,pixel+SporeDreamGiantLayout.CutTopExtension,height);
                spriteBatch.Draw(capped?cutTexture:texture,position+new Vector2(capped?0:-2,destinationY)-Main.screenPosition+offset,frame,color);
                pixel+=height;destinationY+=height;remaining-=height;
            }
        }
    }
    public override bool PreDraw(int i,int j,SpriteBatch spriteBatch)
    {
        Tile tile=Main.tile[i,j];
        Vector2 offset=Main.drawToScreen?Vector2.Zero:new Vector2(Main.offScreenRange);
        if(tile.TileFrameY==0) {
            if(!IsPrimaryTile(i,j)) return false;
            Rectangle frame=CapFrames[CapStyle(i,j)];
            var cap=ModContent.Request<Texture2D>(Root+"fleshy_mushroom_caps").Value;
            Vector2 position=new(i*16+(Split(tile)?16:8)-SporeDreamGiantLayout.CapAnchorX,j*16+16-frame.Height);
            spriteBatch.Draw(cap,position-Main.screenPosition+offset,frame,Lighting.GetColor(i,j));
        } else DrawStemTile(i,j,spriteBatch,offset);
        return false;
    }
    public override bool TileFrame(int i,int j,ref bool resetFrame,ref bool noBreak)
    {
        if(felling) return false;
        Tile tile=Main.tile[i,j],below=Main.tile[i,j+1];
        bool supported=below.HasTile && (below.TileType==Type || Main.tileSolid[below.TileType]);
        if(Split(tile)) {
            Tile partner=Main.tile[i+(Half(tile)==0?1:-1),j];
            supported&=partner.HasTile && partner.TileType==Type && Split(partner)
                && Half(partner)!=Half(tile) && CapStyle(i,j)==CapStyle(i+(Half(tile)==0?1:-1),j)
                && partner.TileFrameY==tile.TileFrameY;
        }
        if(!supported) WorldGen.KillTile(i,j);
        return false;
    }
    // 先尊重原版挖掘成敗與效果判定，再處理相連菇柄，避免重複砍除。
    public override void KillTile(int i,int j,ref bool fail,ref bool effectOnly,ref bool noItem)
    {
        if(fail || effectOnly || felling) return;
        Tile current=Main.tile[i,j];int x=PrimaryX(i,j),width=Split(current)?2:1;
        GetStemBounds(i,j,out int top,out _,out _);
        felling=true;
        try {
            // 清除切點另一半與上方雙欄，保留有支撐的下方殘柄，不以遞迴逐格往上砍除。
            for(int y=j;y>=top;y--)for(int side=0;side<width;side++) {
                if(x+side==i && y==j) continue;
                Tile t=Main.tile[x+side,y];
                if(t.HasTile && t.TileType==Type) WorldGen.KillTile(x+side,y);
            }
        }
        finally {felling=false;}
    }
}
