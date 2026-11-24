using System.IO;
using OccidentMod.Integration.TModLoader.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Networking;

internal static class RollSync
{
    // This format adds an offer/confirmation round trip before the server commits a roll.
    internal const byte MessageType = 10;
    private const sbyte RejectAction = 0;
    private const sbyte End = 2;
    private const sbyte Offer = 3;
    private const sbyte ConfirmAction = 4;
    private const sbyte GrantAction = 5;
    private const sbyte StartedAction = 6;
    private const sbyte CancelAction = 7;
    private static ulong nextRollId;

    internal static void Request(Mod mod, int direction, ulong request) => SendRequest(mod, (sbyte)direction, request);

    internal static void RequestEnd(Mod mod, ulong rollId) => SendRequest(mod, End, rollId);

    internal static void Confirm(Mod mod, ulong rollId) => SendRequest(mod, ConfirmAction, rollId);

    internal static void NotifyStarted(Mod mod, ulong rollId) => SendRequest(mod, StartedAction, rollId);

    internal static void Cancel(Mod mod, ulong rollId) => SendRequest(mod, CancelAction, rollId);

    private static void SendRequest(Mod mod, sbyte action, ulong id)
    {
        ModPacket packet = mod.GetPacket();
        packet.Write(MessageType);
        packet.Write(action);
        packet.Write(id);
        packet.Send();
    }

    internal static void Receive(BinaryReader reader, int sender)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            int action = reader.ReadSByte();
            ulong id = reader.ReadUInt64();
            if (id == 0 || (uint)sender >= Main.maxPlayers ||
                !Main.player[sender].TryGetModPlayer(out DodgeRollPlayer roll))
                return;

            switch (action)
            {
                case End:
                    if (roll.End(id))
                        BroadcastEnd(roll);
                    break;
                case ConfirmAction:
                    roll.ConfirmOffer(id);
                    break;
                case StartedAction:
                    roll.CommitStart(id);
                    break;
                case CancelAction:
                    roll.CancelOffer(id);
                    break;
                case -1 or 1:
                    if (roll.TryAcceptRequest(id))
                    {
                        ulong offerId = ++nextRollId;
                        if (roll.TryOffer(action, offerId))
                            SendSnapshot(roll, Offer, id, offerId, sender);
                        else
                            Reject(roll, request: id, id: 0);
                    }
                    break;
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            int index = reader.ReadByte();
            int action = reader.ReadSByte();
            ulong request = reader.ReadUInt64();
            ulong rollId = reader.ReadUInt64();
            int stamina = reader.ReadInt32();
            int recovery = reader.ReadByte();
            bool valid = action switch
            {
                End => rollId != 0 && request == 0,
                RejectAction => index == Main.myPlayer && ((rollId != 0) != (request != 0)),
                Offer => index == Main.myPlayer && rollId != 0 && request != 0,
                GrantAction => index == Main.myPlayer && rollId != 0 && request == 0,
                -1 or 1 => rollId != 0 && request == 0,
                _ => false
            };
            if (!valid || index >= Main.maxPlayers || !Main.player[index].TryGetModPlayer(out DodgeRollPlayer roll))
                return;

            switch (action)
            {
                case Offer:
                    roll.ReceiveOffer(request, rollId);
                    break;
                case GrantAction:
                    roll.ReceiveGrant(rollId);
                    break;
                case RejectAction:
                    roll.ReceiveRejection(request, rollId, stamina, recovery);
                    break;
                case End:
                    roll.ReceiveEnd(rollId, stamina, recovery);
                    break;
                default:
                    roll.ReceiveStart(action, rollId, stamina, recovery);
                    break;
            }
        }
    }

    internal static void Reject(DodgeRollPlayer roll, ulong request, ulong id) =>
        SendSnapshot(roll, RejectAction, request, id, roll.Player.whoAmI);

    internal static void Grant(DodgeRollPlayer roll, ulong id) =>
        SendSnapshot(roll, GrantAction, 0, id, roll.Player.whoAmI);

    internal static void BroadcastStart(DodgeRollPlayer roll, int direction) =>
        SendSnapshot(roll, direction, 0, roll.RollId, -1);

    internal static void BroadcastEnd(DodgeRollPlayer roll) => SendSnapshot(roll, End, 0, roll.RollId, -1);

    private static void SendSnapshot(DodgeRollPlayer roll, int action, ulong request, ulong rollId, int recipient)
    {
        ModPacket packet = roll.Mod.GetPacket();
        packet.Write(MessageType);
        packet.Write((byte)roll.Player.whoAmI);
        packet.Write((sbyte)action);
        packet.Write(request);
        packet.Write(rollId);
        packet.Write(roll.Stamina.Current);
        packet.Write((byte)roll.Stamina.TicksUntilRecovery);
        packet.Send(recipient);
    }
}
