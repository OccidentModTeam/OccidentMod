using Microsoft.Xna.Framework;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OccidentMod.Content.Tiles.SporeDream;

/// <summary>
///     孢夢之森特殊草皮。
///     Spore Dream Forest grass tile.
/// </summary>
public sealed class SporeDreamGrass : ModTile
{
    public override string Texture =>
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/liegrass_tiles2";

    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
        Main.tileBrick[Type] = true;
        Main.tileMergeDirt[Type] = false;

        Main.tileMerge[Type][TileID.Mud] = true;
        Main.tileMerge[TileID.Mud][Type] = true;
        Main.tileMerge[Type][TileID.Grass] = true;
        Main.tileMerge[TileID.Grass][Type] = true;

        TileID.Sets.Grass[Type] = true;
        TileID.Sets.NeedsGrassFraming[Type] = true;
        TileID.Sets.NeedsGrassFramingDirt[Type] = TileID.Mud;
        TileID.Sets.ChecksForMerge[Type] = true;

        AddMapEntry(
            new Color(30, 119, 117),
            Language.GetOrRegister(
                "Mods.OccidentMod.SporeDream.Grass"
            )
        );

        RegisterItemDrop(ItemID.MudBlock);

        DustType = DustID.Grass;
        VanillaFallbackOnModDeletion = TileID.Mud;
    }

    /// <summary>
    ///     自然生長孢夢植物。
    ///     Naturally grow Spore Dream vegetation.
    /// </summary>
    public override void RandomUpdate(int i, int j)
    {
        if (!WorldGen.InWorld(i, j, 2))
            return;

        var forest = SporeDreamEnvironmentSystem.Instance;

        if (!forest.Contains(i, j))
            return;

        var above = Main.tile[i, j - 1];

        if (above.HasTile || above.LiquidAmount > 0)
            return;

        if (WorldGen.genRand.NextBool(2000) &&
            SporeDreamVegetationRules.CanPlaceFruit(i, j))
        {
            var type = ModContent.TileType<PandoraPlant>();

            var placed = WorldGen.PlaceObject(
                i,
                j - 1,
                type,
                true,
                WorldGen.genRand.Next(3)
            );

            if (placed && Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(
                    -1, i, j - 1, 4
                );
        }
        else if (WorldGen.genRand.NextBool(30))
        {
            var type = ModContent.TileType<SporeDreamPlants>();

            var placed = WorldGen.PlaceObject(
                i,
                j - 1,
                type,
                true,
                WorldGen.genRand.Next(
                    SporeDreamPlants.StyleCount
                )
            );

            if (placed && Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(
                    -1, i, j - 1, 3
                );
        }
    }
}