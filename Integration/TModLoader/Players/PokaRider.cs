using System;
using Microsoft.Xna.Framework;
using VesperMod.Content.Mounts;
using VesperMod.Networking;
using VesperMod.Presentation.Rendering;
using Terraria;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.Players;

public sealed class PokaRider : ModPlayer
{
    private const int FullChargeTicks = 45;
    private const int TerrainGraceTicks = 8;
    private const int RiseStartTick = 19;
    internal const byte EndCharge = 0;
    internal const byte BeginCharge = 1;
    internal const byte ReleaseCharge = 2;
    private const float MinimumJumpSpeed = 6.5f;
    private const float MaximumJumpSpeed = 13f;
    private const float MaximumAscent = 14f * 16f;
    private const float SlowFallThreshold = 9f * 16f;
    private const float SlowFallSpeed = 3.5f;
    private const int CrouchDuration = 36;

    private int callDelay;
    private bool jumpHeld;
    private bool grounded;
    private bool floating;
    private int chargeTicks;
    private int unsupportedTicks;
    private int crouchTick = -1;
    private bool charging;
    private float? launchHeight;
    private bool slowFall;
    private float idleClock;
    private float walkClock;
    private int waterBobTick;

    private bool MovingOnSurface => (grounded || floating) && Math.Abs(Player.velocity.X) > 0.15f;

    internal int FullChargeElapsed { get; private set; }

    internal float ChargeProgress => charging ? chargeTicks / (float)FullChargeTicks : 0f;

    public override void SetControls()
    {
        if (!PokaMount.IsMounted(Player))
            return;

        jumpHeld = Player.controlJump;
        Player.controlJump = false;
    }

    public override void PostUpdateRunSpeeds()
    {
        if (!PokaMount.IsMounted(Player) || launchHeight.HasValue || !PokaBuoyancy.TryGetSurface(Player, out _))
            return;

        Player.ignoreWater = true;
        Player.maxRunSpeed *= 0.65f;
        Player.accRunSpeed *= 0.65f;
    }

    public override void PreUpdateMovement()
    {
        if (!PokaMount.IsMounted(Player) || Player.whoAmI != Main.myPlayer)
            return;

        bool hasWater = PokaBuoyancy.TryGetSurface(Player, out float surface);
        floating = !launchHeight.HasValue && hasWater;
        bool canCharge = Player.grapCount == 0 && !Player.pulley && !Player.tongued &&
            !Player.shimmering && !Player.frozen && !Player.stoned;
        bool canLaunch = canCharge && (floating || grounded && Player.velocity.Y * Player.gravDir >= 0f);
        unsupportedTicks = canLaunch ? 0 : Math.Min(unsupportedTicks + 1, TerrainGraceTicks + 1);

        if (canLaunch && jumpHeld && crouchTick < 0)
        {
            if (!charging)
            {
                charging = true;
                PokaAnimationSync.Send(this, BeginCharge);
            }

            chargeTicks = Math.Min(chargeTicks + 1, FullChargeTicks);
        }
        else if (charging && (!canCharge || canLaunch || unsupportedTicks > TerrainGraceTicks))
        {
            if (canLaunch && !jumpHeld)
            {
                float speed = MathHelper.Lerp(MinimumJumpSpeed, MaximumJumpSpeed, ChargeProgress);
                Player.velocity.Y = -speed * Player.gravDir;
                Player.jump = 0;
                launchHeight = Player.Center.Y;
                slowFall = false;
                floating = false;
                crouchTick = RiseStartTick;
                PokaAnimationSync.Send(this, ReleaseCharge);
            }
            else
            {
                PokaAnimationSync.Send(this, EndCharge);
            }

            charging = false;
            chargeTicks = 0;
        }

        if (floating)
            PokaBuoyancy.Apply(Player, surface, walkClock, ref waterBobTick);

        float verticalSpeed = Player.velocity.Y * Player.gravDir;
        if (launchHeight is float start)
        {
            float ascent = (start - Player.Center.Y) * Player.gravDir;
            slowFall |= ascent >= SlowFallThreshold;

            if (verticalSpeed < 0f)
            {
                float remaining = Math.Max(0f, MaximumAscent - ascent);
                Player.velocity.Y = -Math.Min(-verticalSpeed, remaining) * Player.gravDir;
            }
            else if (verticalSpeed > 0f)
            {
                launchHeight = null;
            }
        }

        if (floating)
            slowFall = false;
        else if (slowFall && verticalSpeed > SlowFallSpeed)
            Player.velocity.Y = SlowFallSpeed * Player.gravDir;
    }

    public override void PostUpdate()
    {
        if (!PokaMount.IsMounted(Player))
        {
            jumpHeld = grounded = floating = charging = false;
            chargeTicks = unsupportedTicks = 0;
            crouchTick = -1;
            launchHeight = null;
            slowFall = false;
            idleClock = walkClock = 0;
            callDelay = 0;
            waterBobTick = 0;
            FullChargeElapsed = 0;
            return;
        }

        PokaCallSync.Update(this, ref callDelay);

        if (charging && Player.whoAmI != Main.myPlayer)
            chargeTicks = Math.Min(chargeTicks + 1, FullChargeTicks);

        FullChargeElapsed = ChargeProgress >= 1f ? FullChargeElapsed + 1 : 0;
        grounded = Player.velocity.Y == 0f;
        if (Player.whoAmI != Main.myPlayer)
            floating = crouchTick < 0 && PokaBuoyancy.TryGetSurface(Player, out _);
        if (grounded && !launchHeight.HasValue)
            slowFall = false;

        if (crouchTick >= 0 && ++crouchTick >= CrouchDuration)
            crouchTick = -1;

        if (MovingOnSurface)
        {
            walkClock = (walkClock + Math.Abs(Player.velocity.X) / 19f) % PokaMount.WalkFrameCount;
            idleClock = 0;
        }
        else
        {
            idleClock = (idleClock + 1) % PokaAnimation.IdleDuration;
            walkClock = 0;
        }
    }

    internal void ReceiveCharge(byte state)
    {
        charging = state == BeginCharge;
        chargeTicks = 0;
        crouchTick = state == ReleaseCharge ? RiseStartTick : -1;
    }

    internal (string Animation, int Frame) GetPose() =>
        PokaAnimation.GetPose(charging, ChargeProgress, MovingOnSurface, crouchTick, walkClock, idleClock);
}
