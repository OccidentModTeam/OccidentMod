using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Occident.Common.Worlds;

namespace Occident.Content.Tiles.SporeDream;

public sealed class SporeDreamCurvedGiant : ModTile
{
    public override string Texture => "OccidentMod/Assets/Sprites/Environment/SporeDreamForest/amanita_crema_tiles";
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type]=true;Main.tileNoAttach[Type]=true;
        Main.tileAxe[Type]=true;Main.tileHammer[Type]=false;Main.tileNoFail[Type]=false;
        MinPick=0;MineResist=1f;DustType=DustID.WoodFurniture;
        AddMapEntry(new Color(156,83,83),Language.GetText("Mods.OccidentMod.SporeDream.GiantMushroom"));RegisterItemDrop(ItemID.Mushroom);
    }
    public override bool PreDraw(int i,int j,SpriteBatch spriteBatch)
    {
        Vector2 offset=Main.drawToScreen?Vector2.Zero:new Vector2(Main.offScreenRange);
        SporeDreamCurvedSystem.Instance.DrawAt(i,j,spriteBatch,offset,false);return false;
    }
    public override bool TileFrame(int i,int j,ref bool resetFrame,ref bool noBreak)=>false;
    public override void KillTile(int i,int j,ref bool fail,ref bool effectOnly,ref bool noItem)
    {
        if(!fail && !effectOnly) SporeDreamCurvedSystem.Instance.CutAt(i,j);
    }
}
