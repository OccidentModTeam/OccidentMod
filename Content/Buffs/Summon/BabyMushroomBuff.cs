// 維持菇寶寶召喚增益，沒有對應召喚物時移除增益。
using Terraria;
using Terraria.ModLoader;
using Occident.Content.Projectiles.Minions;

namespace Occident.Content.Buffs.Summon;

public sealed class BabyMushroomBuff : ModBuff
{
    public override string Texture => "OccidentMod/Assets/Sprites/Item/Furniture/mushroom_in_a_bottle";

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<BabyMushroomMinion>()] > 0)
            player.buffTime[buffIndex] = 18000;
        else {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}
