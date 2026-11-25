using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Projectiles.Enemies;
using OccidentMod.Integration.TModLoader.Configs;
using OccidentMod.Presentation.Audio;
using OccidentMod.Presentation.Rendering;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.NPCs.Enemies;

public sealed class ArticulatedBiped : ModNPC
{
    internal const int CollisionWidth = 36;
    internal const int CollisionHeight = 126;
    private const float WalkSpeed = 0.88f;
    private const float StompApproachDistance = 64f;
    private const int StompApproachTicks = 72;
    private const int StompCooldown = 60;
    private const float StrikeReach = 82f;
    private const int StrikeCooldown = 108;
    private const int ReactiveStrikeWindup = 14;
    private const int ReactiveStrikeCooldown = 120;
    private const int SummonCount = 4;
    private static readonly int[] CrimsonEnemies = [NPCID.Crimera, NPCID.FaceMonster, NPCID.BloodCrawler];
    private static readonly int[] HardmodeCrimsonEnemies =
        [NPCID.Crimera, NPCID.FaceMonster, NPCID.BloodCrawler, NPCID.Herpling, NPCID.FloatyGross, NPCID.IchorSticker];

    private float walkPhase;
    private int approachTicks;
    private int approachTarget = -1;
    private int reactionTarget = -1;
    private float lastTargetVerticalSpeed;
    private int jumpReactionCooldown;
    private HighwatcherAudio audio;

    public override string Texture => ArticulatedBipedRig.AssetPath + "Body";

    internal BipedMotion Motion => (BipedMotion)(int)NPC.ai[0];
    internal int AttackTick => (int)NPC.ai[1];
    private int HoldTicks => (int)NPC.ai[2];
    private bool ReactiveStrike => Motion == BipedMotion.NeckStrike && NPC.ai[2] == 1f;
    private int StrikeTimeSaved => ReactiveStrike ? ArticulatedBipedRig.StrikeHitStart - ReactiveStrikeWindup : 0;
    private bool ReachedHalfHealth => NPC.ai[3] == 1f;
    private bool AtHalfHealth => NPC.life <= NPC.lifeMax / 2;
    private static bool IsAuthority => Main.netMode != NetmodeID.MultiplayerClient;

    internal void PlayStompSound(Vector2 position) => audio.Stomp(position);
    internal BipedPose AttackPose(int tick) => ArticulatedBipedRig.GetPose(Motion, StrikePoseTick(tick), HoldTicks);

    internal int StrikePoseTick(int tick)
    {
        if (!ReactiveStrike)
            return tick;

        return tick < ReactiveStrikeWindup
            ? tick * ArticulatedBipedRig.StrikeHitStart / ReactiveStrikeWindup
            : tick + StrikeTimeSaved;
    }

    public override void Load()
    {
        if (!Main.dedServ)
            ArticulatedBipedRig.LoadTextures();
    }

    public override void Unload() => ArticulatedBipedRig.UnloadTextures();

    public override void SetDefaults()
    {
        NPC.width = CollisionWidth;
        NPC.height = CollisionHeight;
        NPC.aiStyle = -1;
        NPC.lifeMax = 300;
        NPC.defense = 12;
        NPC.damage = 0;
        NPC.knockBackResist = 0.18f;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
        => ModContent.GetInstance<HighwatcherPopulation>().CanSpawnNaturally(spawnInfo) ? 0.025f : 0f;

    public override void OnKill() => ModContent.GetInstance<HighwatcherPopulation>().RecordDefeat(NPC.Bottom);

    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (ReachedHalfHealth || AtHalfHealth)
            modifiers.FinalDamage *= 0.85f;
    }

    public override void AI()
    {
        if (NPC.life <= 0)
            return;

        if (IsAuthority && jumpReactionCooldown > 0)
            jumpReactionCooldown--;

        if (IsAuthority && !ReachedHalfHealth && AtHalfHealth)
        {
            NPC.ai[3] = 1f;
            approachTicks = 0;
            StartMotion(BipedMotion.SummonCall);
        }

        bool summoning = Motion == BipedMotion.SummonCall;
        if (!Main.dedServ)
        {
            float? progress = summoning && AttackTick < ArticulatedBipedRig.SummonCallTicks
                ? AttackTick / (float)ArticulatedBipedRig.SummonCallTicks
                : null;
            audio.Update(NPC, progress);
        }

        if (summoning)
        {
            UpdateSummonCall();
            return;
        }

        if (Motion != BipedMotion.Walk)
        {
            if (!IsAuthority || !TryReactToJump())
                UpdateAttack();
            return;
        }

        Chase();
    }

    private void Chase()
    {
        NPC.TargetClosest();
        if (IsAuthority && TryReactToJump())
            return;
        if (NPC.ai[1] > 0f)
            NPC.ai[1]--;

        Player target = Main.player[NPC.target];
        float speed = 0f;
        bool grounded = NPC.velocity.Y == 0f || NPC.collideY;
        if (target.active && !target.dead)
        {
            Vector2 offset = target.Bottom - NPC.Bottom;
            float horizontal = Math.Abs(offset.X);
            float vertical = Math.Abs(offset.Y);
            bool inStrikeRange = horizontal >= 18f && horizontal <= StrikeReach && vertical <= 70f;
            ArticulatedBipedConfig config = ModContent.GetInstance<ArticulatedBipedConfig>();
            bool close = horizontal <= Math.Min(StompApproachDistance, config.StompRadius + 12f) && vertical <= 32f;

            if (IsAuthority && TryStartAttack(config, close, inStrikeRange, grounded))
                return;

            bool holdingStrikeRange = !close && inStrikeRange;
            if (horizontal > 24f && !holdingStrikeRange)
                speed = NPC.direction * WalkSpeed;
        }
        else
        {
            approachTicks = 0;
            approachTarget = -1;
        }

        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, speed, 0.15f);
        NPC.spriteDirection = NPC.direction;
        if (speed != 0f && grounded)
        {
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
                ref NPC.gfxOffY);
            if (NPC.collideX)
                NPC.velocity.Y = -5f;
        }

        if (grounded && !NPC.collideX)
            walkPhase = (walkPhase + Math.Abs(NPC.velocity.X) / 44f) % 1f;
    }

    private bool TryStartAttack(ArticulatedBipedConfig config, bool close, bool inStrikeRange, bool grounded)
    {
        if (approachTarget != NPC.target || !close)
        {
            approachTarget = NPC.target;
            approachTicks = 0;
        }

        if (close && approachTicks < StompApproachTicks)
            approachTicks++;

        if (!grounded || AttackTick != 0)
            return false;

        if (close)
        {
            if (approachTicks < StompApproachTicks)
                return false;

            approachTicks = 0;
            StartMotion(BipedMotion.Stomp, config.GetStompHoldTicks(Main.rand.Next(100)));
            return true;
        }

        if (!inStrikeRange)
            return false;

        StartMotion(BipedMotion.NeckStrike);
        return true;
    }

    private bool TryReactToJump()
    {
        if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
        {
            reactionTarget = -1;
            return false;
        }

        Player target = Main.player[NPC.target];
        if (!target.active || target.dead)
        {
            reactionTarget = -1;
            return false;
        }

        float verticalSpeed = target.velocity.Y;
        bool tookOff = reactionTarget == NPC.target && verticalSpeed < -1f
            && (lastTargetVerticalSpeed >= -1f || verticalSpeed < lastTargetVerticalSpeed - 2.5f);
        reactionTarget = NPC.target;
        lastTargetVerticalSpeed = verticalSpeed;
        if (!tookOff || Motion == BipedMotion.NeckStrike || jumpReactionCooldown > 0)
            return false;

        Vector2 offset = target.Bottom - NPC.Bottom;
        float distance = Math.Abs(offset.X);
        if (distance < 16f || distance > StrikeReach + 4f || Math.Abs(offset.Y) > 96f)
            return false;

        int chance = Math.Clamp(ModContent.GetInstance<ArticulatedBipedConfig>().JumpReactionChancePercent, 0, 100);
        if (Main.rand.Next(100) >= chance)
            return false;

        approachTicks = 0;
        jumpReactionCooldown = ReactiveStrikeCooldown;
        NPC.direction = offset.X < 0f ? -1 : 1;
        StartMotion(BipedMotion.NeckStrike, 1);
        return true;
    }

    // ai[2] stores the stomp hold time or the fast neck-strike flag, depending on the motion.
    private void StartMotion(BipedMotion motion, int motionParameter = 0)
    {
        NPC.ai[0] = (int)motion;
        NPC.ai[1] = 0f;
        NPC.ai[2] = motionParameter;
        NPC.velocity.X = 0f;
        NPC.spriteDirection = NPC.direction;
        NPC.netUpdate = true;
    }

    private void ResumeWalk(int cooldown)
    {
        NPC.ai[0] = (int)BipedMotion.Walk;
        NPC.ai[1] = cooldown;
        NPC.netUpdate = true;
    }

    private void UpdateAttack()
    {
        NPC.velocity.X = 0f;
        bool stomp = Motion == BipedMotion.Stomp;
        int eventTick = stomp ? ArticulatedBipedRig.StompImpactTick(HoldTicks)
            : ArticulatedBipedRig.StrikeHitStart - StrikeTimeSaved;
        int end = stomp ? ArticulatedBipedRig.StompEndTick(HoldTicks)
            : ArticulatedBipedRig.StrikeEnd - StrikeTimeSaved;
        NPC.ai[1] = Math.Min(AttackTick + 1, end);

        if (!IsAuthority)
            return;

        if (AttackTick == eventTick)
        {
            ArticulatedBipedConfig config = ModContent.GetInstance<ArticulatedBipedConfig>();
            BipedPose pose = AttackPose(AttackTick);
            Vector2 center = stomp
                ? ArticulatedBipedRig.StompImpactPoint(NPC.Bottom, NPC.direction, pose)
                : ArticulatedBipedRig.HeadHitCenter(NPC.Bottom, NPC.direction, pose);
            int damage = Math.Clamp(stomp ? config.StompDamage : config.HeadStrikeDamage, 0, 500);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), center, Vector2.Zero,
                ModContent.ProjectileType<ArticulatedBipedHitbox>(), damage, 0f, Main.myPlayer,
                NPC.whoAmI + 1, stomp ? config.StompRadius : 0);
        }

        if (AttackTick >= end)
            ResumeWalk(stomp ? StompCooldown : StrikeCooldown);
    }

    private void UpdateSummonCall()
    {
        NPC.velocity.X = 0f;
        NPC.ai[1] = Math.Min(AttackTick + 1, ArticulatedBipedRig.SummonCallTicks);
        if (!IsAuthority || AttackTick < ArticulatedBipedRig.SummonCallTicks || !NPC.active || NPC.life <= 0)
            return;

        SummonCrimsonEnemies();
        ResumeWalk(StrikeCooldown);
    }

    private void SummonCrimsonEnemies()
    {
        int[] pool = Main.hardMode ? HardmodeCrimsonEnemies : CrimsonEnemies;
        for (int i = 0; i < SummonCount; i++)
        {
            int type = pool[Main.rand.Next(pool.Length)];
            var template = ContentSamples.NpcsByNetId[type];
            float offset = (i % 2 == 0 ? -1f : 1f) * (34f + (i / 2) * 26f);
            Vector2 bottom = FindSummonSpot(offset, template.width, template.height);
            int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)bottom.X, (int)bottom.Y, type, Target: NPC.target);
            if (index < Main.maxNPCs && Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: index);
        }
    }

    private Vector2 FindSummonSpot(float xOffset, int width, int height)
    {
        Vector2 bottom = NPC.Bottom + new Vector2(xOffset, -2f);
        for (int rise = 0; rise <= 64; rise += 16)
        {
            Vector2 point = bottom - new Vector2(0f, rise);
            if (WorldGen.InWorld((int)(point.X / 16f), (int)(point.Y / 16f), 10)
                && !Collision.SolidCollision(point - new Vector2(width / 2f, height), width, height))
                return point;
        }

        // If walls block the spread, use the open space already occupied by the summoner.
        return NPC.Center + new Vector2(MathHelper.Clamp(xOffset, -6f, 6f), 8f);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        float pace = Math.Clamp(Math.Abs(NPC.velocity.X) / WalkSpeed, 0f, 1f);
        if (NPC.velocity.Y != 0f && !NPC.collideY)
            pace = 0f;

        BipedPose pose = ArticulatedBipedRig.GetPose(Motion, StrikePoseTick(AttackTick), HoldTicks, walkPhase, pace);
        ArticulatedBipedRig.Draw(spriteBatch, NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY),
            NPC.direction, drawColor, pose);
        return false;
    }
}
