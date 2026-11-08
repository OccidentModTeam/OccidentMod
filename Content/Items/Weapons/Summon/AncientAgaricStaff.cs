using Microsoft.Xna.Framework;
using OccidentMod.Content.Buffs.Summon;
using OccidentMod.Content.Projectiles.Minions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Items.Weapons.Summon;

/// <summary>
///     召喚菇寶寶的法杖，並賦予維持召喚物的增益。
///     Summoning staff that creates a baby mushroom minion and applies its sustaining buff.
/// </summary>
public sealed class AncientAgaricStaff : ModItem
{
    /// <summary>
    ///     取得武器的貼圖路徑。
    ///     Gets the weapon texture path.
    /// </summary>

    public override string Texture => "OccidentMod/Assets/Sprites/Items/Weapons/Summon/ancient_agaric_staff";

    /// <summary>
    ///     設定法杖的靜態屬性。
    ///     Configures the staff's static properties.
    /// </summary>
    public override void SetStaticDefaults()
    {
        Item.staff[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }

    /// <summary>
    ///     設定武器數值與召喚物類型。
    ///     Configures weapon stats and the summoned minion type.
    /// </summary>
    public override void SetDefaults()
    {
        // 沿用原版史萊姆法杖的動畫、音效與擊退設定。
        // Inherit vanilla Slime Staff animation, sound, and knockback.
        Item.CloneDefaults(ItemID.SlimeStaff);

        Item.width = 42;
        Item.height = 42;
        Item.damage = 9;
        Item.mana = 10;
        Item.reuseDelay = 0;
        Item.autoReuse = true;
        Item.DamageType = DamageClass.Summon;
        Item.noMelee = true;
        Item.buffType = ModContent.BuffType<BabyMushroomBuff>();
        Item.shoot = ModContent.ProjectileType<BabyMushroomMinion>();
    }

    /// <summary>
    ///     在玩家附近召喚菇寶寶並套用召喚增益。
    ///     Spawns a baby mushroom near the player and applies its summoning buff.
    /// </summary>
    public override bool Shoot(
        Player player,
        EntitySource_ItemUse_WithAmmo source,
        Vector2 position,
        Vector2 velocity,
        int type,
        int damage,
        float knockback)
    {
        var spawn = player.Center + new Vector2(player.direction * 28f, 0f);

        if (Collision.SolidCollision(spawn - new Vector2(11, 10), 22, 20))
            spawn = player.Center;

        player.AddBuff(Item.buffType, 2);

        var index = Projectile.NewProjectile(
            source,
            spawn,
            Vector2.Zero,
            type,
            damage,
            knockback,
            player.whoAmI);

        if (index < Main.maxProjectiles)
            Main.projectile[index].originalDamage = Item.damage;

        return false;
    }
}