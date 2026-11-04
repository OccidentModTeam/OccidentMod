// 召喚菇寶寶的法杖，產生召喚物並賦予維持用增益。
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.Buffs.Summon;
using Occident.Content.Projectiles.Minions;

namespace Occident.Content.Items.Weapons.Summon;

public sealed class AncientAgaricStaff : ModItem
{
    public override string Texture => "OccidentMod/Assets/Sprites/Weapon/Summon/ancient_agaric_staff";

    public override void SetStaticDefaults()
    {
        Item.staff[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }

    public override void SetDefaults()
    {
        // 沿用原版法杖預設值以取得一般使用動畫、音效與擊退。
        Item.CloneDefaults(ItemID.SlimeStaff);
        Item.width = 42;
        Item.height = 42;
        Item.damage = 9;
        Item.mana = 10;
        // 不額外增加召喚間隔，保留原版使用動畫。
        Item.reuseDelay = 0;
        Item.autoReuse = true;
        Item.DamageType = DamageClass.Summon;
        Item.noMelee = true;
        Item.buffType = ModContent.BuffType<BabyMushroomBuff>();
        Item.shoot = ModContent.ProjectileType<BabyMushroomMinion>();
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        // 在召喚者附近生成，由重力使菇寶寶落到下方最近地面。
        Vector2 spawn = player.Center + new Vector2(player.direction * 28f, 0f);
        if (Collision.SolidCollision(spawn - new Vector2(11, 10), 22, 20)) spawn = player.Center;
        player.AddBuff(Item.buffType, 2);
        int index = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
        if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}
