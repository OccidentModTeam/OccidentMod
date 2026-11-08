#nullable enable

using OccidentMod.Content.Tiles.Furniture;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Items.Furniture;

/// <summary>
///     瓶中蘑菇家具物品。
///     Placeable mushroom-in-a-bottle furniture item.
/// </summary>
public sealed class MushroomInABottle : ModItem
{
    public override string Texture =>
        "OccidentMod/Assets/Sprites/Tiles/Furniture/mushroom_in_a_bottle";

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(
            ModContent.TileType<MushroomInABottleTile>());

        Item.width = 32;
        Item.height = 32;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.White;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Bottle)
            .AddIngredient(ItemID.Mushroom)
            .AddTile(TileID.Tables)
            .Register();
    }
}