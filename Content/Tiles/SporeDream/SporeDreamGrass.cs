using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Occident.Common.Worlds;

namespace Occident.Content.Tiles.SporeDream;

public sealed class SporeDreamGrass : ModTile
{
    public override string Texture => "OccidentMod/Assets/Sprites/Environment/SporeDreamForest/liegrass_tiles2";
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type]=true; Main.tileBlockLight[Type]=true; Main.tileMergeDirt[Type]=false;
        Main.tileBrick[Type]=true;
        Main.tileMerge[Type][TileID.Mud]=true;
        TileID.Sets.Grass[Type]=true;
        TileID.Sets.NeedsGrassFraming[Type]=true;
        TileID.Sets.NeedsGrassFramingDirt[Type]=TileID.Mud;
        TileID.Sets.ChecksForMerge[Type]=true;
        Main.tileMerge[TileID.Mud][Type]=true;
        Main.tileMerge[TileID.Grass][Type]=true;
        Main.tileMerge[Type][TileID.Grass]=true;
        AddMapEntry(new Color(30,119,117),Language.GetOrRegister("Mods.OccidentMod.SporeDream.Grass"));
        RegisterItemDrop(ItemID.MudBlock);
        DustType=DustID.Grass;
        VanillaFallbackOnModDeletion=TileID.Mud;
    }
    public override void RandomUpdate(int i,int j)
    {
        var forest=SporeDreamForestSystem.Instance;
        if(!forest.Contains(i,j) || Main.tile[i,j-1].HasTile || Main.tile[i,j-1].LiquidAmount>0) return;
        if(WorldGen.genRand.NextBool(2000) && forest.CanPlaceFruit(i,j)) {
            bool placed=WorldGen.PlaceObject(i,j-1,ModContent.TileType<PandoraPlant>(),true,WorldGen.genRand.Next(3));
            if(placed && Main.netMode==NetmodeID.Server) NetMessage.SendObjectPlacement(-1,i,j-1,ModContent.TileType<PandoraPlant>(),Main.tile[i,j-1].TileFrameX/36,0,-1,-1);
        } else if(WorldGen.genRand.NextBool(30)) {
            WorldGen.PlaceObject(i,j-1,ModContent.TileType<SporeDreamPlants>(),true,WorldGen.genRand.Next(SporeDreamPlants.StyleCount));
            if(Main.netMode==NetmodeID.Server) NetMessage.SendTileSquare(-1,i,j-1,3);
        }
    }
}
