using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Content.Items.Materials;

public sealed class PandorasFruit : ModItem
{
    public override string Texture => "Occident/Assets/Sprites/Environment/SporeDreamForest/pangora_berry_item";
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Type,new DrawAnimationVertical(8,6));
        ItemID.Sets.AnimatesAsSoul[Type]=true;
    }
    public override void SetDefaults()
    {
        Item.width=26; Item.height=26; Item.maxStack=Item.CommonMaxStack;
        Item.rare=ItemRarityID.Blue;
        // Deliberately no use style, permanent buffs, healing or story unlocks.
    }
}
