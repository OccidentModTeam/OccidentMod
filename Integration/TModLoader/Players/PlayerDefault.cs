
#nullable enable

using Terraria;
using VesperMod.Integration.TModLoader.Projectiles;

namespace VesperMod.Integration.TModLoader.Players;

/// <summary>
///
/// 负责识别玩家直接使用武器造成的攻击。
/// 包括直接命中及具有有效武器来源的弹幕。
///
/// Identifies attacks caused by player-used weapons,
/// including direct hits and confirmed weapon projectiles.
///
/// </summary>

public static class PlayerDefault
{
    public static bool IsDirectWeaponAttack(Player player, Item item)
    {
        return !item.IsAir &&
               !item.accessory &&
               item.damage > 0 &&
               ReferenceEquals(item, player.HeldItem);
    }

    public static bool IsWeaponProjectile(
        Player player,
        Projectile projectile)
    {
        if (!projectile.friendly ||
            projectile.owner != player.whoAmI)
            return false;

        return projectile
            .GetGlobalProjectile<WeaponOriginProjectile>()
            .IsQualified;
    }
}
