#nullable enable

using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.Projectiles;

/// <summary>
///
/// 记录弹幕是否来自玩家直接使用的武器。
/// 允许有效武器弹幕的派生攻击继承来源资格。
/// 无法确认来源的弹幕默认不参与额外处理。
///
/// Records whether a projectile originates from
/// a player-used weapon.
/// Allows child projectiles to inherit confirmed origins.
/// Unknown origins are excluded from additional processing.
///
/// </summary>

public sealed class WeaponOriginProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public bool IsQualified { get; private set; }

    public override void OnSpawn(
        Projectile projectile,
        IEntitySource source)
    {
        IsQualified = false;

        // 排除命中触发的额外攻击。
        if (source is IEntitySource_OnHit)
            return;

        // 检查是否由玩家当前手持武器生成。
        if (source is IEntitySource_WithStatsFromItem itemSource)
        {
            var player = itemSource.Player;
            var item = itemSource.Item;

            IsQualified = projectile.owner == player.whoAmI &&
                          !item.IsAir &&
                          !item.accessory &&
                          item.damage > 0 &&
                          ReferenceEquals(item, player.HeldItem);

            return;
        }

        // 从已确认的武器弹幕继承来源。
        if (source is EntitySource_Parent
            {
                Entity: Projectile parent
            } && parent.owner == projectile.owner)
        {
            IsQualified = parent
                .GetGlobalProjectile<WeaponOriginProjectile>()
                .IsQualified;
        }
    }
}