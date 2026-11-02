using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Content.Items.Furniture;

public sealed class MushroomInABottle : ModItem
{
    public override string Texture => "Occident/Assets/Sprites/Item/Furniture/mushroom_in_a_bottle";

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.Furniture.MushroomInABottleTile>());
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
