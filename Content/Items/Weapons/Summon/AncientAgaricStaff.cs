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
        // Vanilla staff defaults supply the ordinary use animation, sound and knockback.
        Item.CloneDefaults(ItemID.SlimeStaff);
        Item.width = 42;
        Item.height = 42;
        Item.damage = 9;
        Item.mana = 10;
        // No extra cooldown between summon uses; retain the vanilla use animation.
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
        // Start near the summoner; gravity puts the baby on the nearest ground below.
        Vector2 spawn = player.Center + new Vector2(player.direction * 28f, 0f);
        if (Collision.SolidCollision(spawn - new Vector2(11, 10), 22, 20)) spawn = player.Center;
        player.AddBuff(Item.buffType, 2);
        int index = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
        if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}
