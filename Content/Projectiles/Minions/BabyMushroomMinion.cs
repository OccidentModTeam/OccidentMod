using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.Buffs.Summon;

namespace Occident.Content.Projectiles.Minions;

public sealed class BabyMushroomMinion : ModProjectile
{
    private const int FrameCount = 8;
    private const int FrameWidth = 20;
    private const int FrameHeight = 20;
    private const float RiseGravity = 0.26f;
    private const float FallGravity = 0.34f;
    private const float AttackHopSpeed = 3f;
    private const float FollowHopSpeed = 3.2f;
    private const float ObstacleHopSpeed = 6f;
    // Source frames contain vertical animation offsets. Anchor each visible sole to
    // the physical hitbox so only projectile motion supplies the jump displacement.
    private static readonly int[] LegacyFeetY = { 34, 34, 30, 24, 28, 34, 32, 34 };
    private Texture2D measuredTexture;
    private int[] measuredFeetY;
    private int preparationTicks;
    private int landingTicks;
    private bool wasAirborne;

    public override string Texture => "Occident/Assets/Sprites/Summoned Entity/baby_mushroom";

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = FrameCount;
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 22;
        Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 14; i++) {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.Cloud, 0f, -1f, 100, Color.White, 0.8f);
            dust.noGravity = true;
            dust.velocity *= 0.6f;
        }
    }

    public override bool MinionContactDamage() => true;
    public override bool? CanCutTiles() => false;
    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        Player owner = Main.player[Projectile.owner];
        fallThrough = owner.Bottom.Y > Projectile.Bottom.Y + 48f;
        return true;
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) {
            owner.ClearBuff(ModContent.BuffType<BabyMushroomBuff>());
            return;
        }
        if (owner.HasBuff(ModContent.BuffType<BabyMushroomBuff>())) Projectile.timeLeft = 2;

        bool fallThrough = owner.Bottom.Y > Projectile.Bottom.Y + 48f;
        bool grounded = Projectile.velocity.Y == 0f &&
            Collision.TileCollision(Projectile.position, Vector2.UnitY, Projectile.width,
                Projectile.height, fallThrough).Y == 0f;
        if (grounded && wasAirborne) {
            landingTicks = 6;
            Projectile.velocity.X = 0f;
            Projectile.ai[2] = 0f;
        }
        wasAirborne = !grounded;

        NPC target = FindTarget(owner);
        Projectile.friendly = target != null;
        Vector2 destination = target?.Center ?? owner.Bottom +
            new Vector2(-owner.direction * (36 + Projectile.minionPos * 28), -10);
        float ownerDistance = Vector2.Distance(Projectile.Center, owner.Center);

        if (ownerDistance > 1800f && Main.myPlayer == Projectile.owner) {
            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[2] = 0f;
            Projectile.netUpdate = true;
        }

        // Return through terrain if stranded; normal following and combat stay on the ground.
        if (ownerDistance > 600f || Projectile.ai[1] == 1f) {
            Projectile.ai[1] = 1f;
            Projectile.ai[2] = 0f;
            Projectile.tileCollide = false;
            Vector2 delta = owner.Center - Projectile.Center;
            Projectile.velocity = (Projectile.velocity * 14f + delta.SafeNormalize(Vector2.Zero) * 12f) / 15f;
            if (delta.Length() < 48f && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
                Projectile.ai[1] = 0f;
                Projectile.tileCollide = true;
            }
        }
        else {
            Projectile.tileCollide = true;
            float dx = destination.X - Projectile.Center.X;
            float stopDistance = target == null ? 18f :
                Math.Max(2f, (Projectile.width + target.width) * 0.5f - 3f);
            if (grounded) {
                Projectile.velocity.X *= 0.7f;
                if (Projectile.ai[0] > 0f) Projectile.ai[0]--;
                if (Math.Abs(dx) > stopDistance && Projectile.ai[0] <= 0f && landingTicks <= 0) {
                    if (preparationTicks == 0) preparationTicks = 4;
                    if (--preparationTicks == 0) {
                        int direction = Math.Sign(dx);
                        // A jumping enemy is not an obstacle. Only a solid block
                        // directly ahead enables a high hop.
                        Vector2 probe = new(direction > 0 ? Projectile.Right.X + 1f : Projectile.Left.X - 5f,
                            Projectile.Bottom.Y - 16f);
                        bool blocked = Collision.SolidCollision(probe, 4, 16);
                        float hopSpeed = blocked ? ObstacleHopSpeed :
                            target != null ? AttackHopSpeed : FollowHopSpeed;
                        // Shorten the horizontal stride with the remaining gap;
                        // otherwise even a low hop can carry it past a small enemy.
                        float estimatedHeight = hopSpeed * hopSpeed / (2f * RiseGravity);
                        float flightTicks = Math.Max(1f, hopSpeed / RiseGravity +
                            MathF.Sqrt(2f * estimatedHeight / FallGravity) - 1f);
                        float speed = Math.Clamp((Math.Abs(dx) - stopDistance + 3f) / flightTicks,
                            0.3f, target != null ? 3f : 4f);
                        Projectile.velocity = new Vector2(direction * speed, -hopSpeed);
                        Projectile.ai[2] = direction;
                        Projectile.ai[0] = target != null ? 4f : 12f;
                        Projectile.netUpdate = true;
                    }
                }
                else preparationTicks = 0;
            }
            else if (target != null && Projectile.ai[2] != 0f) {
                // Keep the launch direction until landing. Approaching or passing
                // the enemy may brake this hop, but can never reverse it in midair.
                float remainingAhead = dx * Projectile.ai[2];
                if (remainingAhead <= stopDistance) Projectile.velocity.X *= 0.9f;
            }
            float gravity = Projectile.velocity.Y < 0f ? RiseGravity : FallGravity;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + gravity, 10f);
        }

        if (!grounded && Projectile.ai[2] != 0f)
            Projectile.spriteDirection = Projectile.direction = Math.Sign(Projectile.ai[2]);
        else if (Math.Abs(Projectile.velocity.X) > 0.1f)
            Projectile.spriteDirection = Projectile.direction = Math.Sign(Projectile.velocity.X);
        UpdateAnimation(grounded);
    }

    private void UpdateAnimation(bool grounded)
    {
        if (grounded && landingTicks > 0) {
            Projectile.frame = landingTicks-- > 3 ? 5 : 7;
        }
        else if (grounded && Projectile.velocity.Y >= 0f) {
            Projectile.frame = preparationTicks > 0 ? 1 : 0;
        }
        else if (Projectile.velocity.Y < -3f) Projectile.frame = 2;
        else if (Projectile.velocity.Y < -0.5f) Projectile.frame = 3;
        else if (Projectile.velocity.Y <= 0.5f) Projectile.frame = 4;
        else Projectile.frame = 6;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        int frame = Math.Clamp(Projectile.frame, 0, FrameCount - 1);
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        int width = FrameWidth;
        int height = FrameHeight;
        int feetY;
        if (texture.Width == FrameWidth && texture.Height == FrameHeight * FrameCount) {
            // The edited sheet uses eight 20x20 cells. Measure its actual feet once
            // rather than carrying offsets over from the older, larger artwork.
            if (measuredTexture != texture) {
                Color[] pixels = new Color[texture.Width * texture.Height];
                texture.GetData(pixels);
                measuredFeetY = new int[FrameCount];
                for (int index = 0; index < FrameCount; index++) {
                    measuredFeetY[index] = height;
                    bool found = false;
                    for (int y = height - 1; y >= 0 && !found; y--) {
                        for (int x = 0; x < width; x++) {
                            if (pixels[(index * height + y) * width + x].A == 0) continue;
                            measuredFeetY[index] = y + 1;
                            found = true;
                            break;
                        }
                    }
                }
                measuredTexture = texture;
            }
            feetY = measuredFeetY[frame];
        }
        else if (texture.Width == 32 && texture.Height == 288) {
            // Keep the original exported PNG usable until the edited sheet replaces it.
            width = 32;
            height = 36;
            feetY = LegacyFeetY[frame];
        }
        else {
            // Never fall back to drawing the whole sheet if an unexpected layout is supplied.
            return false;
        }
        Rectangle source = new(0, frame * height, width, height);
        Main.EntitySpriteDraw(texture,
            Projectile.Bottom - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            source, lightColor, Projectile.rotation, new Vector2(width / 2f, feetY),
            Projectile.scale, SpriteEffects.None, 0f);
        return false;
    }

    private NPC FindTarget(Player owner)
    {
        if (owner.HasMinionAttackTargetNPC) {
            NPC designated = Main.npc[owner.MinionAttackTargetNPC];
            if (designated.CanBeChasedBy(this) && Vector2.Distance(Projectile.Center, designated.Center) < 700f &&
                Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height,
                    designated.position, designated.width, designated.height)) return designated;
        }
        NPC closest = null;
        float range = 600f;
        foreach (NPC npc in Main.ActiveNPCs) {
            float distance = Vector2.Distance(Projectile.Center, npc.Center);
            if (!npc.CanBeChasedBy(this) || distance >= range ||
                !Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height,
                    npc.position, npc.width, npc.height)) continue;
            closest = npc;
            range = distance;
        }
        return closest;
    }
}
