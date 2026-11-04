// 致幻果物品的外觀、動畫與基本數值，沒有永久能力效果。
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Content.Items.Materials;

public sealed class PandorasFruit : ModItem
{
    public override string Texture => "OccidentMod/Assets/Sprites/Item/pangora_berry_item";
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Type,new DrawAnimationVertical(8,6));
        ItemID.Sets.AnimatesAsSoul[Type]=true;
    }
    public override void SetDefaults()
    {
        Item.width=26; Item.height=26; Item.maxStack=Item.CommonMaxStack;
        Item.rare=ItemRarityID.Blue;
        // 此物品刻意不提供使用動作、永久增益、治療或劇情解鎖效果。
    }
}
