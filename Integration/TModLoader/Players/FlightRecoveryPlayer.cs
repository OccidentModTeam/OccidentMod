using System;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.Integration.TModLoader.Players;

public sealed class FlightRecoveryPlayer : ModPlayer
{
    private const float LandingRecoveryFraction = 0.15f;
    private const int LandingRecoveryCooldownTicks = 120;
    private const float RecoveryFractionPerTick = 0.20f / 60f;

    private bool needsInitialFlightTime;
    private bool hasGroundSample;
    private bool movingTowardGround;
    private bool wasGrounded;
    private int landingCooldown;

    public override void Load() => IL_Player.Update += PreserveFlightTimeOnGroundReset;

    public override void Unload() => IL_Player.Update -= PreserveFlightTimeOnGroundReset;

    public override void OnEnterWorld() => ResetFlightRecovery();

    public override void OnRespawn() => ResetFlightRecovery();

    public override void PostUpdateEquips()
    {
        if (!needsInitialFlightTime || !IsLocalPlayer(Player) || !FlightStatus.Read(Player).HasFlight)
            return;

        // Equipped wings have supplied their capacity before vanilla processes flight input.
        Player.wingTime = Math.Max(Player.wingTime, Player.wingTimeMax);
        needsInitialFlightTime = false;
        hasGroundSample = false;
    }

    public override void PreUpdateMovement()
    {
        movingTowardGround = Player.velocity.Y * Player.gravDir > 0f && Player.grapCount == 0;
    }

    public override void PostUpdate()
    {
        if (!IsLocalPlayer(Player))
            return;

        if (landingCooldown > 0)
            landingCooldown--;

        var flight = FlightStatus.Read(Player);
        bool grounded = Player.velocity.Y == 0f && !Player.tongued && !Player.shimmering &&
            !Player.pulley;
        bool landed = flight.HasFlight && hasGroundSample && grounded && !wasGrounded && movingTowardGround;
        wasGrounded = grounded;
        hasGroundSample = flight.HasFlight;

        if (!flight.HasFlight || flight.Remaining >= flight.Maximum)
            return;

        float recovery = Player.controlJump ? 0f : RecoveryFractionPerTick;
        if (landed && landingCooldown == 0)
        {
            recovery = LandingRecoveryFraction;
            landingCooldown = LandingRecoveryCooldownTicks;
        }

        if (recovery > 0f)
            Player.wingTime = Math.Min(flight.Maximum, flight.Remaining + flight.Maximum * recovery);
    }

    private void ResetFlightRecovery()
    {
        needsInitialFlightTime = true;
        hasGroundSample = false;
        movingTowardGround = false;
        wasGrounded = false;
        landingCooldown = 0;
    }

    private static bool IsLocalPlayer(Player player) => !Main.dedServ && player.whoAmI == Main.myPlayer;

    private void PreserveFlightTimeOnGroundReset(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before,
            instruction => instruction.MatchCallvirt<Mount>(nameof(Mount.ResetFlightTime)),
            instruction => instruction.MatchLdarg(0),
            instruction => instruction.MatchLdarg(0),
            instruction => instruction.MatchLdfld<Player>(nameof(Player.wingTimeMax)),
            instruction => instruction.MatchConvR4(),
            instruction => instruction.MatchStfld<Player>(nameof(Player.wingTime))))
        {
            Mod.Logger.Warn("Could not locate the vanilla ground wing-time refill.");
            return;
        }

        // Preserve the remaining wing time at the vanilla ground reset.
        cursor.Index += 5;
        cursor.EmitLdarg0();
        cursor.EmitDelegate<Func<float, Player, float>>((maximum, player) =>
            IsLocalPlayer(player) ? Math.Min(player.wingTime, maximum) : maximum);
    }
}
