using VesperMod.Features.Character.Classes;
using VesperMod.Networking;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace VesperMod.Integration.TModLoader;

/// <summary>Owns the player's potential state and passive charge and release timing.</summary>
public sealed class PotentialChargePlayer : ModPlayer
{
    private const float DefaultMaximum = 100f;
    private const string SavedPotentialKey = "Potential";
    private const int TicksPerSecond = 60;
    private const int ChargeTicks = 20 * TicksPerSecond;
    private const int ReleaseTicks = 5 * TicksPerSecond;
    private const int ChargeSyncInterval = 12;
    private const int ReleaseSyncInterval = 6;
    private int releaseTicksRemaining;
    private float savedPotential;
    private bool multiplayerSession;
    private ulong lastSnapshotRevision;

    public PotentialState Potential { get; } = new(DefaultMaximum);

    internal void ReceiveSnapshot(ulong revision, float current, bool active)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && revision > lastSnapshotRevision &&
            Potential.TryApplySnapshot(current, active))
        {
            lastSnapshotRevision = revision;
        }
    }

    // Keep the local character save separate from the server's multiplayer mirror.
    public override void SaveData(TagCompound tag) =>
        tag[SavedPotentialKey] = multiplayerSession ? savedPotential : Potential.Current;

    public override void LoadData(TagCompound tag)
    {
        releaseTicksRemaining = 0;
        float current = tag.GetFloat(SavedPotentialKey);
        if (!Potential.TryApplySnapshot(current, active: false))
            Potential.TryApplySnapshot(0f, active: false);

        savedPotential = Potential.Current;
    }

    public override void OnEnterWorld()
    {
        multiplayerSession = Main.netMode != NetmodeID.SinglePlayer;
        if (multiplayerSession && lastSnapshotRevision == 0)
            Potential.TryApplySnapshot(0f, active: false);
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (global::VesperMod.VesperMod.ReleasePotentialKey?.JustPressed != true ||
            !Potential.IsFull || Potential.IsActive || Player.dead || Player.ghost)
        {
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            PotentialSync.RequestRelease(Mod);
        else
            TryRelease();
    }

    internal void TryRelease()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead ||
            Player.ghost || !Potential.TryBeginRelease())
        {
            return;
        }

        releaseTicksRemaining = ReleaseTicks;
        PotentialSync.Send(this);
    }

    public override void PostUpdate()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active)
            return;

        if (releaseTicksRemaining > 0)
        {
            AdvanceRelease();
            return;
        }

        if (!Player.dead && !Player.ghost && Potential.ChargeOverTime(ChargeTicks) &&
            (Potential.IsFull || Main.GameUpdateCount % ChargeSyncInterval == 0))
        {
            PotentialSync.Send(this, Player.whoAmI);
        }
    }

    public override void UpdateDead()
    {
        if (releaseTicksRemaining > 0)
            AdvanceRelease();
    }

    private void AdvanceRelease()
    {
        releaseTicksRemaining--;
        Potential.AdvanceRelease((float)releaseTicksRemaining / ReleaseTicks);
        if (releaseTicksRemaining % ReleaseSyncInterval == 0)
            PotentialSync.Send(this);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
        PotentialSync.Send(this, toWho);
}
