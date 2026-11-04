// 菇寶寶召喚物的地面跟隨、尋敵、跳躍攻擊與動畫。
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
    private const float AttackHopSpeed = 3.4f;
    private const float FollowHopSpeed = 3.2f;
    private const float ObstacleHopSpeed = 6f;
    // 來源影格本身含垂直偏移；將每格可見腳底對齊實體碰撞框，跳躍位移只由彈幕運動提供。
    private static readonly int[] LegacyFeetY = { 34, 34, 30, 24, 28, 34, 32, 34 };
    private Texture2D measuredTexture;
    private int[] measuredFeetY;
    private int preparationTicks;
    private int landingTicks;
    private bool wasAirborne;
    private int targetIndex=-1;
    private int targetSearchTimer;

    public override string Texture => "OccidentMod/Assets/Sprites/Summoned Entity/baby_mushroom";

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
        Projectile.localNPCHitCooldown = 18;
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

    // ai[0] 是地面跳躍間隔，ai[1] 是穿地返回旗標，ai[2] 是本次起跳方向。
    // 先維持主人與增益，再決定返回、地面跟隨或跳躍攻擊，最後更新動畫。
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
            landingTicks = 2;
            Projectile.velocity.X = 0f;
            Projectile.ai[2] = 0f;
        }
        wasAirborne = !grounded;

        NPC target = FindTarget(owner);
        if(target!=null) landingTicks=0;
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

        // 卡住或離主人太遠時才穿地返回，一般跟隨與戰鬥維持地面移動。
        float returnDistance=target!=null ? 900f : 600f;
        if (ownerDistance > returnDistance || Projectile.ai[1] == 1f) {
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
            float stopDistance = target == null ? 18f : 2f;
            if (grounded) {
                Projectile.velocity.X *= 0.7f;
                if (Projectile.ai[0] > 0f) Projectile.ai[0]--;
                if ((target!=null || Math.Abs(dx)>stopDistance) && Projectile.ai[0] <= 0f && landingTicks <= 0) {
                    if (preparationTicks == 0) preparationTicks = target!=null ? 1 : 4;
                    if (--preparationTicks == 0) {
                        int direction = Math.Abs(dx)>1f ? Math.Sign(dx) : (Projectile.direction==0 ? 1 : Projectile.direction);
                        // 跳起的敵人不算障礙物，只有正前方實心物塊才觸發高跳。
                        Vector2 probe = new(direction > 0 ? Projectile.Right.X + 1f : Projectile.Left.X - 5f,
                            Projectile.Bottom.Y - 16f);
                        bool blocked = Collision.SolidCollision(probe, 4, 16);
                        float hopSpeed = blocked ? ObstacleHopSpeed :
                            target != null ? AttackHopSpeed : FollowHopSpeed;
                        if(target!=null && target.Bottom.Y<Projectile.Bottom.Y-8f) {
                            float rise=Math.Clamp(Projectile.Bottom.Y-target.Bottom.Y+10f,22f,70f);
                            hopSpeed=Math.Max(hopSpeed,MathF.Sqrt(2f*RiseGravity*rise));
                        }
                        // 依剩餘距離縮短水平步幅，避免低跳也越過小型敵人。
                        float estimatedHeight = hopSpeed * hopSpeed / (2f * RiseGravity);
                        float flightTicks = Math.Max(1f, hopSpeed / RiseGravity +
                            MathF.Sqrt(2f * estimatedHeight / FallGravity) - 1f);
                        float speed = Math.Clamp((Math.Abs(dx) - stopDistance + 3f) / flightTicks,
                            target!=null ? 0.5f : 0.3f, target != null ? 6.5f : 4f);
                        Projectile.velocity = new Vector2(direction * speed, -hopSpeed);
                        Projectile.ai[2] = direction;
                        Projectile.ai[0] = target != null ? 0f : 12f;
                        Projectile.netUpdate = true;
                    }
                }
                else preparationTicks = 0;
            }
            else if (target != null && Projectile.ai[2] != 0f) {
                // 起跳後保持方向到落地；接近或越過敵人時可以減速，但不在空中反向。
                float remainingAhead = dx * Projectile.ai[2];
                if (remainingAhead <= stopDistance) Projectile.velocity.X *= 0.75f;
                else {
                    float pursuitSpeed=Math.Clamp(remainingAhead*.18f,1.2f,6.5f);
                    Projectile.velocity.X=MathHelper.Lerp(Projectile.velocity.X,Projectile.ai[2]*pursuitSpeed,.18f);
                }
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
            // 新版圖集為八格 20×20，直接量測各格腳底，不沿用舊大尺寸素材偏移。
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
            // 新版圖集替換完成前，仍相容原始匯出的 PNG。
            width = 32;
            height = 36;
            feetY = LegacyFeetY[frame];
        }
        else {
            // 遇到未知版面就停止自訂繪製，避免把整張圖集當成單格畫出。
            return false;
        }
        Rectangle source = new(0, frame * height, width, height);
        Main.EntitySpriteDraw(texture,
            Projectile.Bottom - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            source, lightColor, Projectile.rotation, new Vector2(width / 2f, feetY),
            Projectile.scale, Projectile.spriteDirection<0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        return false;
    }

    // 依搜尋冷卻與可見性挑選目標，保留仍有效的近距離對象。
    private NPC FindTarget(Player owner)
    {
        if (owner.HasMinionAttackTargetNPC) {
            NPC designated = Main.npc[owner.MinionAttackTargetNPC];
            if (designated.CanBeChasedBy(this) && Vector2.DistanceSquared(Projectile.Center, designated.Center) < 1000f*1000f &&
                CanSeeTarget(designated)) {
                targetIndex=designated.whoAmI;
                targetSearchTimer=0;
                return designated;
            }
        }
        // 兩次搜尋之間保留存活且在範圍內的目標；死亡或超出範圍時立即清掉，不等計時器。
        if(targetIndex>=0 && targetIndex<Main.maxNPCs && targetSearchTimer>0) {
            targetSearchTimer--;
            NPC cached=Main.npc[targetIndex];
            if(cached.CanBeChasedBy(this)
                && Vector2.DistanceSquared(Projectile.Center,cached.Center)<900f*900f
                && Vector2.DistanceSquared(owner.Center,cached.Center)<1100f*1100f) return cached;
        }
        NPC closest = null;
        float rangeSquared = 800f*800f;
        foreach (NPC npc in Main.ActiveNPCs) {
            if(!npc.CanBeChasedBy(this)) continue;
            float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
            if (distanceSquared >= rangeSquared || Vector2.DistanceSquared(owner.Center,npc.Center)>=1100f*1100f ||
                !CanSeeTarget(npc)) continue;
            closest = npc;
            rangeSquared = distanceSquared;
        }
        targetIndex=closest?.whoAmI ?? -1;
        targetSearchTimer=closest!=null ? 5 : 0;
        return closest;
    }

    private bool CanSeeTarget(NPC target)
    {
        // 小型地面角色的中心射線容易被鄰近地板擋住；補查上半身射線以辨識可見敵人，仍禁止穿整面牆攻擊。
        return Collision.CanHitLine(Projectile.position,Projectile.width,Projectile.height,
                target.position,target.width,target.height)
            || Collision.CanHitLine(Projectile.Top+new Vector2(0,2),1,1,
                target.Top+new Vector2(0,2),1,1);
    }
}
