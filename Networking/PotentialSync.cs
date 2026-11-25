using System.IO;
using VesperMod.Integration.TModLoader;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Networking;

internal static class PotentialSync
{
    private const byte MessageType = 2;
    private const byte ReleaseRequest = 3;
    private static ulong nextRevision;

    public static void Send(PotentialChargePlayer charge, int toWho = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = charge.Mod.GetPacket();
        packet.Write(MessageType);
        packet.Write((byte)charge.Player.whoAmI);
        packet.Write(++nextRevision);
        packet.Write(charge.Potential.Current);
        packet.Write(charge.Potential.IsActive);
        packet.Send(toWho);
    }

    public static void RequestRelease(Mod mod)
    {
        ModPacket packet = mod.GetPacket();
        packet.Write(ReleaseRequest);
        packet.Send();
    }

    internal static void Receive(BinaryReader reader, int sender, byte message)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            if (message == ReleaseRequest && (uint)sender < Main.maxPlayers &&
                Main.player[sender].TryGetModPlayer(out PotentialChargePlayer owner))
            {
                owner.TryRelease();
            }

            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient || message != MessageType)
            return;

        int playerIndex = reader.ReadByte();
        ulong revision = reader.ReadUInt64();
        float current = reader.ReadSingle();
        byte active = reader.ReadByte();

        if (playerIndex < Main.maxPlayers && active <= 1 &&
            Main.player[playerIndex].TryGetModPlayer(out PotentialChargePlayer charge))
        {
            charge.ReceiveSnapshot(revision, current, active != 0);
        }
    }
}
