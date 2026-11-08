using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Items.Materials;

/// <summary>
///     孢夢之森的致幻果材料。
///     Pandora fruit material.
/// </summary>
public sealed class PandorasFruit : ModItem
{
    public override string Texture =>
        "OccidentMod/Assets/Sprites/Items/Furniture/pangora_berry_item";

    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(
            Type,
            new DrawAnimationVertical(8, 6)
        );

        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 26;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.Blue;

        // 暫不提供使用效果。
        // No active effects are implemented yet.
    }
}