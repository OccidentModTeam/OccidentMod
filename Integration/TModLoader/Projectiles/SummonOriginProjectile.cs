#nullable enable

using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace OccidentMod.Integration.TModLoader.Projectiles;

/// <summary>
///
/// 记录弹幕是否来自有效召唤体系。
/// 认可占用栏位的召唤物、哨兵及其有效派生攻击。
/// 仅使用召唤伤害加成的其他攻击不自动获得资格。
///
/// Records whether a projectile belongs to
/// an eligible summon system.
/// Accepts slot-consuming minions, sentries,
/// and qualifying child projectiles.
/// Summon damage scaling alone is not sufficient.
///
/// </summary>

public sealed class SummonOriginProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public bool IsQualified { get; private set; }

    public override void OnSpawn(
        Projectile projectile,
        IEntitySource source)
    {
        IsQualified = false;

        // 排除命中触发的额外效果。
        if (source is IEntitySource_OnHit)
            return;

        // 认可占用召唤栏位的召唤物和哨兵。
        if ((projectile.minion && projectile.minionSlots > 0f) ||
            projectile.sentry)
        {
            IsQualified = true;
            return;
        }

        // 派生弹幕必须属于召唤关联攻击。
        if (!projectile.IsMinionOrSentryRelated)
            return;

        // 继承已确认的召唤主体来源。
        if (source is EntitySource_Parent
            {
                Entity: Projectile parent
            } && parent.owner == projectile.owner)
        {
            IsQualified = parent
                .GetGlobalProjectile<SummonOriginProjectile>()
                .IsQualified;
        }
    }
}