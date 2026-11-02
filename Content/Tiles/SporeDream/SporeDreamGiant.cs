using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Content.Tiles.SporeDream;

public sealed class SporeDreamGiant : ModTile
{
    private const string Root="OccidentMod/Assets/Sprites/Environment/SporeDreamForest/";
    // These are the actual opaque bounds, not equal atlas cells.
    public static readonly Rectangle[] CapFrames={new(2,12,96,62),new(100,20,76,54),new(186,2,52,72)};
    private static readonly int[] BranchAnchorY={28,30,38};
    public override string Texture => Root+"amanita_crema_tiles";
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type]=true; Main.tileAxe[Type]=true; Main.tileLavaDeath[Type]=true;
        Main.tileNoFail[Type]=false; Main.tileNoAttach[Type]=true;
        MineResist=4f;
        AddMapEntry(new Color(191,132,69)); DustType=DustID.WoodFurniture;
        RegisterItemDrop(ItemID.Mushroom);
    }
    public static bool TryGrow(int x,int ground)
    {
        int height=WorldGen.genRand.Next(6,14), type=ModContent.TileType<SporeDreamGiant>();
        Tile support=Main.tile[x,ground];
        if(!support.HasTile || support.TileType!=ModContent.TileType<SporeDreamGrass>() || support.IsHalfBlock || support.Slope!=0) return false;
        // Keep six tiles between trunks even when replenishing an existing forest.
        for(int i=x-5;i<=x+5;i++) for(int y=ground-40;y<=ground+4;y++)
            if(!WorldGen.InWorld(i,y,1) || (Main.tile[i,y].HasTile && Main.tile[i,y].TileType==type)) return false;
        int plants=ModContent.TileType<SporeDreamPlants>();
        for(int i=x-3;i<=x+3;i++) for(int j=ground-height-5;j<ground;j++) {
            if(!WorldGen.InWorld(i,j,1)) return false;
            Tile t=Main.tile[i,j];
            if((t.HasTile && t.TileType!=plants) || t.LiquidAmount>0 || Main.wallHouse[t.WallType]) return false;
        }
        int style=WorldGen.genRand.Next(3);
        for(int y=ground-height;y<ground;y++) {
            Tile t=Main.tile[x,y]; t.ClearTile(); t.HasTile=true; t.TileType=(ushort)type;
            t.TileFrameX=(short)(style*18); t.TileFrameY=(short)(y==ground-height ? 0 : 18);
        }
        return true;
    }
    private static uint BranchHash(int x,int y,int side,int worldId)
    {
        unchecked {
            uint h=(uint)x*73856093u ^ (uint)y*19349663u ^ (uint)(side+1)*83492791u ^ (uint)worldId;
            h^=h>>16; h*=0x7feb352du; h^=h>>15; h*=0x846ca68bu; return h^(h>>16);
        }
    }
    public static int BranchRow(int x,int y,int side,int worldId)
    {
        uint hash=BranchHash(x,y,side,worldId);
        // Independent left/right rolls, with a vertical gap on each side. No per-frame RNG.
        if(hash%100>=28 || BranchHash(x,y-1,side,worldId)%100<28) return -1;
        return (int)((hash>>8)%3);
    }
    public static bool CanDrawBranch(int x,int y,int side,int row)
    {
        if(row<0 || row>2 || side<0 || side>1 || !WorldGen.InWorld(x,y,5)) return false;
        int type=ModContent.TileType<SporeDreamGiant>();
        // Require a live trunk both above and below the attachment, with a bare lower stem.
        for(int dy=-1;dy<=3;dy++)
            if(!Main.tile[x,y+dy].HasTile || Main.tile[x,y+dy].TileType!=type) return false;
        // Entire branch footprint must be free; never paint across a hillside or another object.
        int left=x*16+(side==0 ? -30 : 4),top=y*16+16-BranchAnchorY[row];
        for(int i=left/16;i<=(left+41)/16;i++) for(int j=top/16;j<=(top+41)/16;j++) {
            if(i==x) continue;
            Tile t=Main.tile[i,j];
            if(t.HasTile) return false;
        }
        return true;
    }
    public override bool PreDraw(int i,int j,SpriteBatch spriteBatch)
    {
        Tile tile=Main.tile[i,j];
        Vector2 offset=Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
        Vector2 pos=new Vector2(i*16,j*16)-Main.screenPosition+offset;
        Color color=Lighting.GetColor(i,j);
        var stem=ModContent.Request<Texture2D>(Root+"amanita_crema_tiles").Value;
        bool baseTile=Main.tile[i,j+1].HasTile && Main.tile[i,j+1].TileType!=Type;
        spriteBatch.Draw(stem,pos,new Rectangle(0,(j%3)*18,16,16),color);
        if(baseTile && !Main.tile[i,j+1].IsHalfBlock && Main.tile[i,j+1].Slope==0)
            spriteBatch.Draw(stem,pos+new Vector2(0,16),new Rectangle(0,(j%3)*18+14,16,2),color);
        if(tile.TileFrameY==0) {
            var tops=ModContent.Request<Texture2D>(Root+"amanita_crema_tops").Value;
            int style=(tile.TileFrameX/18)%3;
            Rectangle frame=CapFrames[style];
            spriteBatch.Draw(tops,pos+new Vector2(8,16),frame,color,0,new Vector2(frame.Width/2f,frame.Height),1,SpriteEffects.None,0);
        } else {
            int top=j;
            while(top>1 && Main.tile[i,top-1].HasTile && Main.tile[i,top-1].TileType==Type) top--;
            // Keep the crown and ground readable; left and right can branch at different heights.
            if(j-top<3 || !Main.tile[i,j+1].HasTile || Main.tile[i,j+1].TileType!=Type) return false;
            for(int side=0;side<2;side++) {
                int row=BranchRow(i,j,side,Main.worldID);
                if(!CanDrawBranch(i,j,side,row)) continue;
                var branches=ModContent.Request<Texture2D>(Root+"little_mushroom").Value;
                spriteBatch.Draw(branches,pos+new Vector2(8,16),new Rectangle(side*42,row*42,42,42),color,0,
                    new Vector2(side==0 ? 38 : 4,BranchAnchorY[row]),1,SpriteEffects.None,0);
            }
        }
        return false;
    }
    public override bool TileFrame(int i,int j,ref bool resetFrame,ref bool noBreak)
    {
        Tile below=Main.tile[i,j+1];
        if(!below.HasTile || (below.TileType!=Type && !Main.tileSolid[below.TileType])) WorldGen.KillTile(i,j);
        return false;
    }
    public override void KillTile(int i,int j,ref bool fail,ref bool effectOnly,ref bool noItem)
    {
        if(fail || effectOnly) return;
        // Fell upward only, so a cut does not remove the lower stump or recurse into itself.
        for(int y=j-1;y>1;y--) {
            Tile t=Main.tile[i,y]; if(!t.HasTile || t.TileType!=Type) break;
            WorldGen.KillTile(i,y);
        }
    }
}
