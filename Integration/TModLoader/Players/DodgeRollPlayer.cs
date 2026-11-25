using System;
using Microsoft.Xna.Framework;
using VesperMod.Features.Character.Attributes;
using VesperMod.Networking;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.Players;

internal sealed class DodgeRollPlayer : ModPlayer
{
    // Round down so a full base bar still covers four rolls.
    private const int StaminaCost = StaminaState.DefaultMaximum / 4;
    private const int Duration = 18;
    private const int DodgeDuration = 10;
    private const int Cooldown = 24;
    private const ulong ReplyTimeout = 120;
    // Allow the request/reply deadline plus the return trip for the start confirmation.
    private const ulong OfferTimeout = 2 * ReplyTimeout;
    private const float StartSpeed = 10f;
    private const float EndSpeed = 3f;

    private enum OfferStage { None, Offered, Confirmed, Granted }

    private int remaining;
    private int cooldown;
    private int direction;
    private float positionBeforeMovement;
    private bool movementSampled;
    private ulong nextRequest;
    private ulong pendingRequest;
    private ulong requestedAt;
    private int requestedDirection;
    private ulong offeredRollId;
    private ulong offeredAt;
    private OfferStage offerStage;
    private ulong ownerStartedAt;
    private ulong lastRequest;
    private ulong rollId;
    private ulong lastRollId;
    private ulong lastEndedRollId;

    private bool IsRolling => remaining > 0;
    private bool IsLocalPlayer => Player.whoAmI == Main.myPlayer;
    private float Progress => (Duration - remaining) / (float)Duration;
    private bool CanMove => Player.active && !Player.dead && !Player.ghost && !Player.CCed &&
        !Player.mount.Active && Player.grapCount == 0 && !Player.pulley && !Player.tongued &&
        !Player.shimmering && !Player.shimmerWet;
    private bool CanTakeOverMovement => CanMove && Player.dashDelay >= 0 &&
        !Player.ItemAnimationActive && !Player.channel;
    private bool CanStart => cooldown == 0 && !IsRolling && CanTakeOverMovement && IsGrounded();
    private bool RequestExpired => Main.GameUpdateCount - requestedAt >= ReplyTimeout;
    private bool OfferExpired => Main.GameUpdateCount - offeredAt >= OfferTimeout;

    internal StaminaState Stamina => Player.GetModPlayer<StaminaPlayer>().Stamina;
    internal ulong RollId => rollId;

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (VesperMod.RollKey?.JustPressed != true || Main.gameMenu || Main.playerInventory ||
            Main.ingameOptionsWindow || Main.mapFullscreen || Main.drawingPlayerChat ||
            Main.blockInput || Player.mouseInterface || pendingRequest != 0 || !CanStart || Stamina.Current < StaminaCost)
            return;

        int rollDirection = Player.direction;
        if (triggersSet.Left != triggersSet.Right)
            rollDirection = triggersSet.Right ? 1 : -1;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            pendingRequest = ++nextRequest;
            requestedAt = Main.GameUpdateCount;
            requestedDirection = rollDirection;
            RollSync.Request(Mod, rollDirection, pendingRequest);
        }
        else
            TryStart(rollDirection);
    }

    private bool TryStart(int rollDirection, ulong id = 0)
    {
        if (rollDirection is not (-1 or 1) || !CanStart || !Stamina.TrySpend(StaminaCost))
            return false;

        Begin(rollDirection);
        rollId = id;
        return true;
    }

    internal bool TryAcceptRequest(ulong request)
    {
        if (request <= lastRequest)
            return false;

        lastRequest = request;
        return true;
    }

    internal bool TryOffer(int rollDirection, ulong id)
    {
        // A newer request on the ordered connection supersedes an unconfirmed offer.
        ClearOffer();
        if (!CanStart || Stamina.Current < StaminaCost)
            return false;

        requestedDirection = rollDirection;
        offeredRollId = id;
        offeredAt = Main.GameUpdateCount;
        offerStage = OfferStage.Offered;
        return true;
    }

    internal void ConfirmOffer(ulong id)
    {
        if (id != offeredRollId || offerStage != OfferStage.Offered)
            return;

        if (OfferExpired || !CanStart || Stamina.Current < StaminaCost)
        {
            ClearOffer();
            RollSync.Reject(this, request: 0, id: id);
            return;
        }

        offerStage = OfferStage.Granted;
        offeredAt = Main.GameUpdateCount;
        RollSync.Grant(this, id);
    }

    internal void CommitStart(ulong id)
    {
        if (id != offeredRollId || offerStage != OfferStage.Granted)
            return;

        bool expired = OfferExpired;
        ClearOffer();
        // Eligibility was checked when granting. The owner's first movement can already be in transit.
        if (expired || !CanTakeOverMovement || cooldown != 0 || IsRolling || !Stamina.TrySpend(StaminaCost))
        {
            RollSync.Reject(this, request: 0, id: id);
            return;
        }

        Begin(requestedDirection);
        rollId = id;
        RollSync.BroadcastStart(this, direction);
    }

    internal void CancelOffer(ulong id)
    {
        if (id == offeredRollId)
            ClearOffer();
    }

    internal void ReceiveOffer(ulong request, ulong id)
    {
        if (request == pendingRequest && !RequestExpired && offeredRollId == 0)
        {
            offeredRollId = id;
            offerStage = OfferStage.Offered;
        }
    }

    internal void ReceiveGrant(ulong id)
    {
        if (id == offeredRollId && offerStage == OfferStage.Confirmed && !RequestExpired)
            offerStage = OfferStage.Granted;
    }

    internal void ReceiveRejection(ulong request, ulong id, int stamina, int recovery)
    {
        if (!Player.active || Player.dead || Player.ghost)
            return;

        if (id != 0 && rollId == id && id > lastRollId)
        {
            lastRollId = id;
            End();
        }
        else if ((request != 0 && request == pendingRequest) || (id != 0 && id == offeredRollId))
        {
            pendingRequest = 0;
            ClearOffer();
        }
        else
            return;

        Stamina.ApplySnapshot(stamina, recovery);
    }

    internal void ReceiveStart(int rollDirection, ulong id, int stamina, int recovery)
    {
        if (id <= lastRollId || !Player.active || Player.dead || Player.ghost || (IsLocalPlayer && rollId != id))
            return;
        lastRollId = id;

        if (IsLocalPlayer)
        {
            // Reconcile from the committed balance without re-starting the owner's clock or recovery delay.
            Stamina.ApplySnapshot(stamina, recovery, Main.GameUpdateCount - ownerStartedAt);
            return;
        }

        Stamina.ApplySnapshot(stamina, recovery);
        Begin(rollDirection);
        rollId = id;
    }

    internal void ReceiveEnd(ulong id, int stamina, int recovery)
    {
        if (id < lastRollId || id <= lastEndedRollId)
            return;
        lastRollId = id;
        lastEndedRollId = id;
        if (!Player.active || Player.dead || Player.ghost || (IsLocalPlayer && rollId != id))
            return;

        if (rollId != 0 && rollId <= id)
            End();
        if (!IsLocalPlayer)
            Stamina.ApplySnapshot(stamina, recovery);
    }

    private void Begin(int rollDirection)
    {
        direction = rollDirection;
        remaining = Duration;
        cooldown = Cooldown;
        movementSampled = false;
    }

    public override void SetControls()
    {
        if (!IsRolling || !CanTakeOverMovement)
            return;

        Player.controlLeft = Player.controlRight = Player.controlJump = false;
        Player.controlHook = Player.controlMount = Player.controlUseItem = Player.controlUseTile = false;
    }

    public override void PreUpdateMovement()
    {
        movementSampled = false;
        if (Main.netMode == NetmodeID.MultiplayerClient && IsLocalPlayer)
            AdvanceLocalOffer();

        if (!IsRolling)
            return;

        if ((IsLocalPlayer || Main.netMode == NetmodeID.Server) && !CanTakeOverMovement)
        {
            Interrupt();
            return;
        }

        // Vanilla sends the owning client's position and resolves tiles, slopes and platforms.
        if (!IsLocalPlayer)
            return;

        positionBeforeMovement = Player.position.X;
        movementSampled = true;
        Player.direction = direction;
        Player.velocity.X = direction * MathHelper.Lerp(StartSpeed, EndSpeed, Progress);
    }

    public override void PostUpdate()
    {
        if (pendingRequest != 0 && RequestExpired)
            CancelLocalOffer();
        if (Main.netMode == NetmodeID.Server && offeredRollId != 0 && OfferExpired)
        {
            RollSync.Reject(this, request: 0, id: offeredRollId);
            ClearOffer();
        }
        if (cooldown > 0)
            cooldown--;

        if (!IsRolling || (IsLocalPlayer && !movementSampled))
            return;

        if (IsLocalPlayer && Math.Abs(Player.position.X - positionBeforeMovement) < 0.5f)
            Interrupt();
        else if (--remaining == 0)
            SendEnd();
    }

    public override bool CanUseItem(Item item) => !IsRolling;

    public override bool FreeDodge(Player.HurtInfo info) =>
        IsRolling && remaining > Duration - DodgeDuration && info.Dodgeable && !info.PvP;

    public override void OnHurt(Player.HurtInfo info) => Interrupt();

    public override void OnEnterWorld() => ResetSession();

    public override void UpdateDead()
    {
        if (Main.netMode == NetmodeID.Server)
        {
            if (offeredRollId != 0)
                RollSync.Reject(this, request: 0, id: offeredRollId);
            Interrupt();
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && IsLocalPlayer)
            CancelLocalOffer();
        ResetRoll();
    }

    public override void PlayerDisconnect() => ResetSession();

    private void ResetSession()
    {
        ResetRoll();
        lastRequest = 0;
        lastRollId = lastEndedRollId = 0;
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (!IsRolling || drawInfo.headOnlyRender)
            return;

        drawInfo.rotationOrigin = Player.Size * 0.5f;
        drawInfo.rotation += MathHelper.TwoPi * Progress * direction * Player.gravDir;
        drawInfo.Position.Y -= MathF.Sin(MathHelper.Pi * Progress) * 5f * Player.gravDir;
    }

    private bool IsGrounded()
    {
        if (Player.velocity.Y * Player.gravDir < 0f)
            return false;

        var step = new Vector2(0f, Player.gravDir);
        return Collision.TileCollision(Player.position, step, Player.width, Player.height,
            gravDir: (int)Player.gravDir).Y != step.Y;
    }

    internal bool End(ulong id)
    {
        return id == rollId && End();
    }

    private bool End()
    {
        if (!IsRolling)
            return false;

        remaining = 0;
        return true;
    }

    private void Interrupt()
    {
        if (End())
            SendEnd();
    }

    private void SendEnd()
    {
        if (Main.netMode == NetmodeID.Server)
            RollSync.BroadcastEnd(this);
        else if (Main.netMode == NetmodeID.MultiplayerClient && IsLocalPlayer)
            RollSync.RequestEnd(Mod, rollId);
    }

    private void ResetRoll()
    {
        remaining = cooldown = 0;
        pendingRequest = 0;
        ClearOffer();
        movementSampled = false;
        rollId = 0;
    }

    private void AdvanceLocalOffer()
    {
        if (offerStage is not (OfferStage.Offered or OfferStage.Granted))
            return;

        if (RequestExpired || !CanStart || Stamina.Current < StaminaCost)
        {
            CancelLocalOffer();
            return;
        }

        if (offerStage == OfferStage.Offered)
        {
            offerStage = OfferStage.Confirmed;
            requestedAt = Main.GameUpdateCount;
            RollSync.Confirm(Mod, offeredRollId);
            return;
        }

        ulong id = offeredRollId;
        if (TryStart(requestedDirection, id))
        {
            ownerStartedAt = Main.GameUpdateCount;
            RollSync.NotifyStarted(Mod, id);
        }
        else
            RollSync.Cancel(Mod, id);
        pendingRequest = 0;
        ClearOffer();
    }

    private void CancelLocalOffer()
    {
        if (offeredRollId != 0)
            RollSync.Cancel(Mod, offeredRollId);
        pendingRequest = 0;
        ClearOffer();
    }

    private void ClearOffer()
    {
        offeredRollId = 0;
        offerStage = OfferStage.None;
    }
}
