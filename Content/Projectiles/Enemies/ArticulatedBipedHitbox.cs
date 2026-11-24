using System;
using Microsoft.Xna.Framework;
using OccidentMod.Content.NPCs.Enemies;
using OccidentMod.Presentation.Audio;
using OccidentMod.Presentation.Rendering;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Projectiles.Enemies;

// Hostile hitboxes use Terraria's normal player immunity and multiplayer damage handling.
public sealed class ArticulatedBipedHitbox : ModProjectile
{
    private const int StompHalfHeight = 24;
    private const int WaveSpeed = 4;
    private const int WaveThickness = 8;
    private const float StompLaunchSpeed = -6.8f;
    private const float HeadHitWidth = 29f;
    private bool playedImpact;

    public override string Texture => ArticulatedBipedRig.AssetPath + "Body";
    private int StompRadius => (int)Projectile.ai[1];
    private bool IsStomp => StompRadius > 0;
    private int WaveAge => (int)Projectile.ai[2];
    private int WaveDuration => (StompRadius + WaveSpeed - 1) / WaveSpeed;
    private float WaveFront => Math.Min(StompRadius, WaveAge * WaveSpeed);

    private ArticulatedBiped Attacker
    {
        get
        {
            int index = (int)Projectile.ai[0] - 1;
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active)
                return null;

            return Main.npc[index].ModNPC as ArticulatedBiped;
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = 44;
        Projectile.height = 36;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = ArticulatedBipedRig.StrikeHitEnd - ArticulatedBipedRig.StrikeHitStart + 3;
        Projectile.hide = true;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (IsStomp)
        {
            UpdateStompWave();
            return;
        }

        if (TryGetHeadSweep(out _, out Vector2 current))
            Projectile.Center = current;
    }

    private void UpdateStompWave()
    {
        if (!playedImpact)
        {
            playedImpact = true;
            Vector2 center = Projectile.Center;
            Projectile.width = StompRadius * 2;
            Projectile.height = StompHalfHeight * 2;
            Projectile.Center = center;
            Projectile.timeLeft = WaveDuration + 1;
            if (!Main.dedServ)
                ShowStomp(center);
        }

        Projectile.ai[2]++;
        if (!Main.dedServ && WaveAge <= WaveDuration)
        {
            ShowWaveFront(-1);
            ShowWaveFront(1);
        }
    }

    public override bool CanHitPlayer(Player target) => Projectile.damage > 0;

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        if (!IsStomp)
            return;

        target.velocity.Y = Math.Min(target.velocity.Y, StompLaunchSpeed);
        if (Main.netMode == NetmodeID.MultiplayerClient && target.whoAmI == Main.myPlayer)
            NetMessage.SendData(MessageID.PlayerControls, number: target.whoAmI);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (IsStomp)
            return InWaveFront(targetHitbox);

        if (!TryGetHeadSweep(out Vector2 previous, out Vector2 current))
            return false;

        float collisionPoint = 0f;
        return Collision.CheckAABBvLineCollision(new Vector2(targetHitbox.X, targetHitbox.Y),
            new Vector2(targetHitbox.Width, targetHitbox.Height), previous, current, HeadHitWidth, ref collisionPoint);
    }

    private bool InWaveFront(Rectangle target)
    {
        Vector2 center = Projectile.Center;
        if (WaveAge == 0 || WaveAge > WaveDuration || target.Bottom < center.Y - StompHalfHeight
            || target.Bottom > center.Y + StompHalfHeight)
            return false;

        float front = WaveFront;
        float rear = Math.Max(0f, front - WaveThickness);
        bool left = target.Right > center.X - front && target.Left < center.X - rear;
        bool right = target.Right > center.X + rear && target.Left < center.X + front;
        return left || right;
    }

    public override bool PreDraw(ref Color lightColor) => false;

    private bool TryGetHeadSweep(out Vector2 previous, out Vector2 current)
    {
        previous = current = Vector2.Zero;
        ArticulatedBiped attacker = Attacker;
        if (attacker is null || attacker.Motion != BipedMotion.NeckStrike)
            return false;

        int tick = attacker.AttackTick;
        int poseTick = attacker.StrikePoseTick(tick);
        if (poseTick < ArticulatedBipedRig.StrikeHitStart - 2 || poseTick > ArticulatedBipedRig.StrikeHitEnd)
            return false;

        NPC npc = attacker.NPC;
        Vector2 anchor = npc.Bottom + new Vector2(0f, npc.gfxOffY);
        previous = ArticulatedBipedRig.HeadHitCenter(anchor, npc.direction, attacker.AttackPose(tick - 1));
        current = ArticulatedBipedRig.HeadHitCenter(anchor, npc.direction, attacker.AttackPose(tick));
        return true;
    }

    private void ShowStomp(Vector2 center)
    {
        if (Attacker is ArticulatedBiped attacker)
            attacker.PlayStompSound(center);
        else
            HighwatcherAudio.PlayBirdCall(center);

        for (int i = 0; i < 7; i++)
        {
            Vector2 point = center + new Vector2(Main.rand.NextFloat(-8f, 8f), -4f);
            Dust.NewDustPerfect(point, DustID.Dirt, new Vector2(Main.rand.NextFloat(-1.3f, 1.3f),
                Main.rand.NextFloat(-3f, -1f)));
        }
    }

    private void ShowWaveFront(int direction)
    {
        Vector2 front = Projectile.Center + new Vector2(direction * WaveFront, -4f);
        for (int i = 0; i < 2; i++)
        {
            Vector2 point = front + new Vector2(Main.rand.NextFloat(-2f, 2f), 0f);
            Dust dust = Dust.NewDustPerfect(point, DustID.Dirt,
                new Vector2(direction * 1.2f, Main.rand.NextFloat(-2.5f, -1f)));
            dust.scale = Main.rand.NextFloat(0.85f, 1.25f);
        }
    }
}
