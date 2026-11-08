using OccidentMod.Content.Projectiles.Minions;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.Content.Buffs.Summon;

/// <summary>
///     維持菇寶寶召喚物的增益，召喚物消失時自動移除。
///     Maintains the baby mushroom minion and removes itself when no minion remains.
/// </summary>
public sealed class BabyMushroomBuff : ModBuff
{
    /// <summary>
    ///     取得召喚增益的圖示路徑。
    ///     Gets the summoning buff icon path.
    /// </summary>

    public override string Texture => "OccidentMod/Assets/Sprites/Tiles/Furniture/mushroom_in_a_bottle";

    /// <summary>
    ///     設定增益的儲存與時間顯示行為。
    ///     Configures buff persistence and time display.
    /// </summary>
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    /// <summary>
    ///     召喚物存在時延長增益，否則移除增益。
    ///     Refreshes the buff while the minion exists; otherwise removes it.
    /// </summary>
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<BabyMushroomMinion>()] > 0)
        {
            player.buffTime[buffIndex] = 18000;
        }
        else
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}