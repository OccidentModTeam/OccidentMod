using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Occident.Content.Tiles.Furniture;

public sealed class MushroomInABottleTile : ModTile
{
    public override string Texture => "OccidentMod/Assets/Sprites/Item/Furniture/mushroom_in_a_bottle";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        // The supplied 32x32 artwork has no gutters between its four tile cells.
        TileObjectData.newTile.CoordinatePadding = 0;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
        TileObjectData.newTile.HookPostPlaceMyPlayer =
            ModContent.GetInstance<MushroomBottleEntity>().Generic_HookPostPlaceMyPlayer;
        TileObjectData.addTile(Type);

        RegisterItemDrop(ModContent.ItemType<Items.Furniture.MushroomInABottle>());
        AddMapEntry(new Color(120, 139, 110),
            Terraria.Localization.Language.GetOrRegister("Mods.OccidentMod.Items.MushroomInABottle.DisplayName"));
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        ModContent.GetInstance<MushroomBottleEntity>().Kill(i, j);
    }
}
