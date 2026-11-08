#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Buffs.Summon;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Projectiles.Minions;

/// <summary>
///     菇寶寶召喚物，負責地面跟隨、尋敵、跳躍攻擊與動畫。
///     Baby mushroom minion that handles ground movement, targeting, hopping attacks, and animation.
/// </summary>
public sealed class BabyMushroomMinion : ModProjectile
{
    private const int FrameCount = 8;
    private const int FrameWidth = 20;
    private const int FrameHeight = 20;

    private const float RiseGravity = 0.26f;
    private const float FallGravity = 0.34f;
    private const float AttackHopSpeed = 3.4f;
    private const float FollowHopSpeed = 3.2f;
    private const float ObstacleHopSpeed = 6f;

    // 舊版圖集的腳底位置。
    // Foot positions in the legacy sprite sheet.
    private static readonly int[] LegacyFeetY = { 34, 34, 30, 24, 28, 34, 32, 34 };
    private int landingTicks;
    private int[]? measuredFeetY;

    private Texture2D? measuredTexture;
    private int preparationTicks;
    private int targetIndex = -1;
    private int targetSearchTimer;
    private bool wasAirborne;

    /// <summary>
    ///     取得召喚物的圖集路徑。
    ///     Gets the minion sprite sheet path.
    /// </summary>

    public override string Texture => "OccidentMod/Assets/Sprites/Projectiles/Minions/baby_mushroom";

    /// <summary>
    ///     設定召喚物影格及原版召喚物標記。
    ///     Configures animation frames and vanilla minion flags.
    /// </summary>
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = FrameCount;
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }

    /// <summary>
    ///     設定召喚物的碰撞、傷害與物理屬性。
    ///     Configures minion collision, damage, and physics properties.
    /// </summary>
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
        Projectile.localNPCHitCooldown = 18;
    }

    /// <summary>
    ///     召喚時產生雲霧粒子。
    ///     Creates cloud particles when the minion spawns.
    /// </summary>
    public override void OnSpawn(IEntitySource source)
    {
        if (Main.dedServ)
            return;

        for (var i = 0; i < 14; i++)
        {
            var dust = Dust.NewDustDirect(
                Projectile.position,
                Projectile.width,
                Projectile.height,
                DustID.Cloud,
                0f,
                -1f,
                100,
                Color.White,
                0.8f);

            dust.noGravity = true;
            dust.velocity *= 0.6f;
        }
    }

    /// <summary>
    ///     允許召喚物透過接觸造成傷害。
    ///     Enables contact damage for the minion.
    /// </summary>
    public override bool MinionContactDamage()
    {
        return true;
    }

    /// <summary>
    ///     禁止召喚物切割植物。
    ///     Prevents the minion from cutting tiles.
    /// </summary>
    public override bool? CanCutTiles()
    {
        return false;
    }

    /// <summary>
    ///     保留原本的碰撞處理，不因碰撞直接銷毀。
    ///     Preserves collision handling without destroying the projectile.
    /// </summary>
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        return false;
    }

    /// <summary>
    ///     根據玩家位置決定是否穿過平台。
    ///     Determines platform fall-through based on the owner's position.
    /// </summary>
    public override bool TileCollideStyle(
        ref int width,
        ref int height,
        ref bool fallThrough,
        ref Vector2 hitboxCenterFrac)
    {
        var owner = Main.player[Projectile.owner];

        fallThrough = owner.Bottom.Y > Projectile.Bottom.Y + 48f;

        return true;
    }

    /// <summary>
    ///     更新召喚物的生命週期、移動、尋敵與跳躍攻擊。
    ///     Updates the minion lifecycle, movement, targeting, and hopping attacks.
    /// </summary>
    /// <remarks>
    ///     ai[0]：地面跳躍間隔。
    ///     ai[1]：穿地返回旗標。
    ///     ai[2]：本次起跳方向。
    ///     ai[0]: Ground hopping interval.
    ///     ai[1]: Return-through-tiles flag.
    ///     ai[2]: Direction of the current hop.
    /// </remarks>
    public override void AI()
    {
        var owner = Main.player[Projectile.owner];

        if (!owner.active || owner.dead)
        {
            owner.ClearBuff(ModContent.BuffType<BabyMushroomBuff>());
            return;
        }

        if (owner.HasBuff(ModContent.BuffType<BabyMushroomBuff>()))
            Projectile.timeLeft = 2;

        var fallThrough = owner.Bottom.Y > Projectile.Bottom.Y + 48f;

        var grounded = Projectile.velocity.Y == 0f &&
                       Collision.TileCollision(
                           Projectile.position,
                           Vector2.UnitY,
                           Projectile.width,
                           Projectile.height,
                           fallThrough).Y == 0f;

        if (grounded && wasAirborne)
        {
            landingTicks = 2;
            Projectile.velocity.X = 0f;
            Projectile.ai[2] = 0f;
        }

        wasAirborne = !grounded;

        var target = FindTarget(owner);

        if (target is not null)
            landingTicks = 0;

        Projectile.friendly = target is not null;

        var destination = target?.Center ??
                          owner.Bottom + new Vector2(
                              -owner.direction * (36 + Projectile.minionPos * 28),
                              -10);

        var ownerDistance = Vector2.Distance(
            Projectile.Center,
            owner.Center);

        if (ownerDistance > 1800f && Main.myPlayer == Projectile.owner)
        {
            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[2] = 0f;
            Projectile.netUpdate = true;
        }

        // 卡住或距離過遠時穿地返回。
        // Return through tiles when stuck or too far from the owner.
        var returnDistance = target is not null ? 900f : 600f;

        if (ownerDistance > returnDistance || Projectile.ai[1] == 1f)
        {
            Projectile.ai[1] = 1f;
            Projectile.ai[2] = 0f;
            Projectile.tileCollide = false;

            var delta = owner.Center - Projectile.Center;

            Projectile.velocity =
                (Projectile.velocity * 14f +
                 delta.SafeNormalize(Vector2.Zero) * 12f) / 15f;

            if (delta.Length() < 48f &&
                !Collision.SolidCollision(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height))
            {
                Projectile.ai[1] = 0f;
                Projectile.tileCollide = true;
            }
        }
        else
        {
            Projectile.tileCollide = true;

            var dx = destination.X - Projectile.Center.X;
            var stopDistance = target is null ? 18f : 2f;

            if (grounded)
            {
                Projectile.velocity.X *= 0.7f;

                if (Projectile.ai[0] > 0f)
                    Projectile.ai[0]--;

                if ((target is not null || Math.Abs(dx) > stopDistance) &&
                    Projectile.ai[0] <= 0f &&
                    landingTicks <= 0)
                {
                    if (preparationTicks == 0)
                        preparationTicks = target is not null ? 1 : 4;

                    if (--preparationTicks == 0)
                    {
                        var direction = Math.Abs(dx) > 1f
                            ? Math.Sign(dx)
                            : Projectile.direction == 0
                                ? 1
                                : Projectile.direction;

                        // 只有前方實心物塊才觸發高跳。
                        // Only solid blocks ahead trigger a higher jump.
                        Vector2 probe = new(
                            direction > 0
                                ? Projectile.Right.X + 1f
                                : Projectile.Left.X - 5f,
                            Projectile.Bottom.Y - 16f);

                        var blocked = Collision.SolidCollision(
                            probe,
                            4,
                            16);

                        var hopSpeed = blocked
                            ? ObstacleHopSpeed
                            : target is not null
                                ? AttackHopSpeed
                                : FollowHopSpeed;

                        if (target is not null &&
                            target.Bottom.Y < Projectile.Bottom.Y - 8f)
                        {
                            var rise = Math.Clamp(
                                Projectile.Bottom.Y - target.Bottom.Y + 10f,
                                22f,
                                70f);

                            hopSpeed = Math.Max(
                                hopSpeed,
                                MathF.Sqrt(2f * RiseGravity * rise));
                        }

                        // 依距離計算跳躍水平速度。
                        // Calculate horizontal hop speed from remaining distance.
                        var estimatedHeight =
                            hopSpeed * hopSpeed / (2f * RiseGravity);

                        var flightTicks = Math.Max(
                            1f,
                            hopSpeed / RiseGravity +
                            MathF.Sqrt(
                                2f * estimatedHeight / FallGravity) - 1f);

                        var speed = Math.Clamp(
                            (Math.Abs(dx) - stopDistance + 3f) / flightTicks,
                            target is not null ? 0.5f : 0.3f,
                            target is not null ? 6.5f : 4f);

                        Projectile.velocity = new Vector2(
                            direction * speed,
                            -hopSpeed);

                        Projectile.ai[2] = direction;
                        Projectile.ai[0] = target is not null ? 0f : 12f;
                        Projectile.netUpdate = true;
                    }
                }
                else
                {
                    preparationTicks = 0;
                }
            }
            else if (target is not null && Projectile.ai[2] != 0f)
            {
                // 空中保持起跳方向，接近目標時減速。
                // Preserve hop direction in midair and slow down near the target.
                var remainingAhead = dx * Projectile.ai[2];

                if (remainingAhead <= stopDistance)
                {
                    Projectile.velocity.X *= 0.75f;
                }
                else
                {
                    var pursuitSpeed = Math.Clamp(
                        remainingAhead * 0.18f,
                        1.2f,
                        6.5f);

                    Projectile.velocity.X = MathHelper.Lerp(
                        Projectile.velocity.X,
                        Projectile.ai[2] * pursuitSpeed,
                        0.18f);
                }
            }

            var gravity = Projectile.velocity.Y < 0f
                ? RiseGravity
                : FallGravity;

            Projectile.velocity.Y = Math.Min(
                Projectile.velocity.Y + gravity,
                10f);
        }

        if (!grounded && Projectile.ai[2] != 0f)
            Projectile.spriteDirection =
                Projectile.direction = Math.Sign(Projectile.ai[2]);
        else if (Math.Abs(Projectile.velocity.X) > 0.1f)
            Projectile.spriteDirection =
                Projectile.direction = Math.Sign(Projectile.velocity.X);

        UpdateAnimation(grounded);
    }

    /// <summary>
    ///     根據地面狀態與垂直速度更新動畫影格。
    ///     Updates animation frames based on grounding and vertical velocity.
    /// </summary>
    private void UpdateAnimation(bool grounded)
    {
        if (grounded && landingTicks > 0)
            Projectile.frame = landingTicks-- > 3 ? 5 : 7;
        else if (grounded && Projectile.velocity.Y >= 0f)
            Projectile.frame = preparationTicks > 0 ? 1 : 0;
        else if (Projectile.velocity.Y < -3f)
            Projectile.frame = 2;
        else if (Projectile.velocity.Y < -0.5f)
            Projectile.frame = 3;
        else if (Projectile.velocity.Y <= 0.5f)
            Projectile.frame = 4;
        else
            Projectile.frame = 6;
    }

    /// <summary>
    ///     依圖集格式繪製召喚物，並將可見腳底對齊碰撞框。
    ///     Draws the minion using supported sprite sheets and aligns its visible feet.
    /// </summary>
    public override bool PreDraw(ref Color lightColor)
    {
        var frame = Math.Clamp(
            Projectile.frame,
            0,
            FrameCount - 1);

        var texture = TextureAssets.Projectile[Type].Value;

        var width = FrameWidth;
        var height = FrameHeight;
        int feetY;

        if (texture.Width == FrameWidth &&
            texture.Height == FrameHeight * FrameCount)
        {
            // 新版八格 20×20 圖集。
            // New eight-frame 20×20 sprite sheet.
            if (measuredTexture != texture || measuredFeetY is null)
            {
                var pixels = new Color[
                    texture.Width * texture.Height];

                texture.GetData(pixels);

                measuredFeetY = new int[FrameCount];

                for (var index = 0; index < FrameCount; index++)
                {
                    measuredFeetY[index] = height;
                    var found = false;

                    for (var y = height - 1; y >= 0 && !found; y--)
                    for (var x = 0; x < width; x++)
                    {
                        if (pixels[
                                (index * height + y) * width + x
                            ].A == 0)
                            continue;

                        measuredFeetY[index] = y + 1;
                        found = true;
                        break;
                    }
                }

                measuredTexture = texture;
            }

            feetY = measuredFeetY[frame];
        }
        else if (texture.Width == 32 && texture.Height == 288)
        {
            // 相容舊版 32×36 圖集。
            // Support the legacy 32×36 sprite sheet.
            width = 32;
            height = 36;
            feetY = LegacyFeetY[frame];
        }
        else
        {
            // 未知圖集格式不進行自訂繪製。
            // Skip custom drawing for unsupported sprite sheets.
            return false;
        }

        Rectangle source = new(
            0,
            frame * height,
            width,
            height);

        Main.EntitySpriteDraw(
            texture,
            Projectile.Bottom - Main.screenPosition +
            new Vector2(0f, Projectile.gfxOffY),
            source,
            lightColor,
            Projectile.rotation,
            new Vector2(width / 2f, feetY),
            Projectile.scale,
            Projectile.spriteDirection < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None);

        return false;
    }

    /// <summary>
    ///     優先選擇玩家指定目標，否則沿用有效快取或搜尋最近敵人。
    ///     Prioritizes the designated target, then cached or nearest valid enemies.
    /// </summary>
    private NPC? FindTarget(Player owner)
    {
        if (owner.HasMinionAttackTargetNPC)
        {
            var designated = Main.npc[owner.MinionAttackTargetNPC];

            if (designated.CanBeChasedBy(this) &&
                Vector2.DistanceSquared(
                    Projectile.Center,
                    designated.Center) < 1000f * 1000f &&
                CanSeeTarget(designated))
            {
                targetIndex = designated.whoAmI;
                targetSearchTimer = 0;
                return designated;
            }
        }

        // 搜尋冷卻期間沿用有效目標。
        // Reuse a valid cached target during the search cooldown.
        if (targetIndex >= 0 &&
            targetIndex < Main.maxNPCs &&
            targetSearchTimer > 0)
        {
            targetSearchTimer--;

            var cached = Main.npc[targetIndex];

            if (cached.CanBeChasedBy(this) &&
                Vector2.DistanceSquared(
                    Projectile.Center,
                    cached.Center) < 900f * 900f &&
                Vector2.DistanceSquared(
                    owner.Center,
                    cached.Center) < 1100f * 1100f)
                return cached;
        }

        NPC? closest = null;
        var rangeSquared = 800f * 800f;

        foreach (var npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(this))
                continue;

            var distanceSquared = Vector2.DistanceSquared(
                Projectile.Center,
                npc.Center);

            if (distanceSquared >= rangeSquared ||
                Vector2.DistanceSquared(
                    owner.Center,
                    npc.Center) >= 1100f * 1100f ||
                !CanSeeTarget(npc))
                continue;

            closest = npc;
            rangeSquared = distanceSquared;
        }

        targetIndex = closest?.whoAmI ?? -1;
        targetSearchTimer = closest is not null ? 5 : 0;

        return closest;
    }

    /// <summary>
    ///     使用碰撞射線判斷召喚物是否能看見目標。
    ///     Checks target visibility using collision-aware sight lines.
    /// </summary>
    private bool CanSeeTarget(NPC target)
    {
        // 補查上半身射線，避免地面附近的射線遭地板阻擋。
        // Check an upper-body ray when the ground-level ray is obstructed.
        return Collision.CanHitLine(
                   Projectile.position,
                   Projectile.width,
                   Projectile.height,
                   target.position,
                   target.width,
                   target.height)
               || Collision.CanHitLine(
                   Projectile.Top + new Vector2(0, 2),
                   1,
                   1,
                   target.Top + new Vector2(0, 2),
                   1,
                   1);
    }
}