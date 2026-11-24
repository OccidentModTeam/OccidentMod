using System.IO;
using OccidentMod.Content.Mounts;
using OccidentMod.Integration.TModLoader.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Networking;

internal static class PokaAnimationSync
{
    internal const byte MessageType = 4;

    internal static void Send(PokaRider rider, byte state, int ignore = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket packet = rider.Mod.GetPacket();
        packet.Write(MessageType);
        packet.Write((byte)rider.Player.whoAmI);
        packet.Write(state);
        packet.Send(ignoreClient: ignore);
    }

    internal static void Receive(BinaryReader reader, int sender)
    {
        int index = reader.ReadByte();
        byte state = reader.ReadByte();
        if (index >= Main.maxPlayers || state > PokaRider.ReleaseCharge ||
            (Main.netMode == NetmodeID.Server && index != sender))
            return;

        Player player = Main.player[index];
        if (!player.active || player.dead || !PokaMount.IsMounted(player))
            return;

        var rider = player.GetModPlayer<PokaRider>();
        rider.ReceiveCharge(state);
        if (Main.netMode == NetmodeID.Server)
            Send(rider, state, sender);
    }
}
