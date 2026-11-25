using VesperMod.Content.Mounts;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Content.Items.Mounts;

public sealed class PokaWhistle : ModItem
{
    public override string Texture => "VesperMod/Assets/Sprites/Items/Mounts/PokaWhistle";

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item79;
        Item.noMelee = true;
        Item.rare = ItemRarityID.Green;
        Item.mountType = ModContent.MountType<PokaMount>();
    }
}
