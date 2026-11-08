using Microsoft.Xna.Framework;
using OccidentMod.Content.Items.Materials;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace OccidentMod.Content.Tiles.SporeDream;

/// <summary>
///     孢夢之森地被植物。
///     Spore Dream Forest ground vegetation.
/// </summary>
public sealed class SporeDreamPlants : ModTile
{
    public const int StyleCount = 30;

    public override string Texture =>
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/mushroom_glass";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileCut[Type] = true;
        Main.tileNoFail[Type] = true;

        TileID.Sets.DisableSmartCursor[Type] = true;

        TileObjectData.newTile.CopyFrom(
            TileObjectData.Style1x1
        );

        TileObjectData.newTile.CoordinateHeights =
            new[] { 20 };

        TileObjectData.newTile.DrawYOffset = -4;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.RandomStyleRange = StyleCount;

        TileObjectData.newTile.AnchorBottom =
            new AnchorData(AnchorType.SolidTile, 1, 0);

        TileObjectData.newTile.AnchorValidTiles =
            new[]
            {
                ModContent.TileType<SporeDreamGrass>()
            };

        TileObjectData.addTile(Type);

        AddMapEntry(new Color(232, 164, 69));
        DustType = DustID.Grass;

        RegisterItemDrop(ItemID.MudBlock);
    }
}

/// <summary>
///     致幻果植株。
///     Pandora fruit plant.
/// </summary>
public sealed class PandoraPlant : ModTile
{
    public override string Texture =>
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/pangora_berry";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;

        TileObjectData.newTile.CopyFrom(
            TileObjectData.Style2x2
        );

        TileObjectData.newTile.Origin =
            new Point16(0, 1);

        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.RandomStyleRange = 3;

        TileObjectData.newTile.AnchorValidTiles =
            new[]
            {
                ModContent.TileType<SporeDreamGrass>()
            };

        TileObjectData.addTile(Type);

        AddMapEntry(
            new Color(145, 86, 182),
            Language.GetOrRegister(
                "Mods.OccidentMod.Items.PandorasFruit.DisplayName"
            )
        );

        RegisterItemDrop(
            ModContent.ItemType<PandorasFruit>()
        );

        DustType = DustID.PurpleMoss;
    }

    public override bool RightClick(int i, int j)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(
                MessageID.TileManipulation,
                -1,
                -1,
                null,
                0,
                i,
                j
            );
        else
            WorldGen.KillTile(i, j);

        return true;
    }

    public override void MouseOver(int i, int j)
    {
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;

        Main.LocalPlayer.cursorItemIconID =
            ModContent.ItemType<PandorasFruit>();
    }
}