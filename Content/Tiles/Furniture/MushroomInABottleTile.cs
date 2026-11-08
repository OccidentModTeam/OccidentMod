#nullable enable

using Microsoft.Xna.Framework;
using OccidentMod.Content.Items.Furniture;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace OccidentMod.Content.Tiles.Furniture;

/// <summary>
///     瓶中蘑菇家具地塊。
///     Mushroom-in-a-bottle furniture tile.
/// </summary>
public sealed class MushroomInABottleTile : ModTile
{
    public override string Texture =>
        "OccidentMod/Assets/Sprites/Tiles/Furniture/mushroom_in_a_bottle";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;

        // 設定 2×2 家具的放置資料。
        // Configure placement data for 2×2 furniture.
        TileObjectData.newTile.CopyFrom(
            TileObjectData.Style2x2);

        TileObjectData.newTile.Origin =
            new Point16(0, 1);

        // 原始貼圖沒有地塊間隔。
        // The original texture has no padding between tiles.
        TileObjectData.newTile.CoordinatePadding = 0;

        TileObjectData.newTile.CoordinateHeights =
            new[] { 16, 16 };

        // 放置完成後建立對應的 TileEntity。
        // Create the tile entity after placement.
        TileObjectData.newTile.HookPostPlaceMyPlayer =
            ModContent.GetInstance<MushroomBottleEntity>()
                .Generic_HookPostPlaceMyPlayer;

        TileObjectData.addTile(Type);

        // 破壞家具後掉落瓶中蘑菇。
        // Drop the furniture item when destroyed.
        RegisterItemDrop(
            ModContent.ItemType<MushroomInABottle>());

        AddMapEntry(
            new Color(120, 139, 110),
            Language.GetOrRegister(
                "Mods.OccidentMod.Items.MushroomInABottle.DisplayName")
        );
    }

    /// <summary>
    ///     家具被完整移除時清除 TileEntity。
    ///     Remove the tile entity when the furniture is destroyed.
    /// </summary>
    public override void KillMultiTile(
        int i,
        int j,
        int frameX,
        int frameY)
    {
        ModContent.GetInstance<MushroomBottleEntity>()
            .Kill(i, j);
    }
}