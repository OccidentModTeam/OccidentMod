#nullable enable

using Terraria;
using VesperMod.Integration.TModLoader.Projectiles;

namespace VesperMod.Integration.TModLoader.Players;

/// <summary>
///
/// 负责识别符合条件的召唤体系攻击。
/// 包括占用召唤栏位的召唤物、哨兵及有效派生弹幕。
/// 不接受仅具有召唤伤害类型的其他攻击。
///
/// Identifies eligible summon attacks,
/// including slot-consuming minions, sentries,
/// and their qualifying child projectiles.
/// Summon damage type alone does not grant eligibility.
///
/// </summary>

public static class PlayerSummon
{
    public static bool IsQualifiedAttack(
        Player player,
        Projectile projectile)
    {
        if (!projectile.friendly ||
            projectile.owner != player.whoAmI)
            return false;

        return projectile
            .GetGlobalProjectile<SummonOriginProjectile>()
            .IsQualified;
    }
}
