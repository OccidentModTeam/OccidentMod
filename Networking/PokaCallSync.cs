using System;
using System.IO;
using Microsoft.Xna.Framework;
using OccidentMod.Content.Mounts;
using OccidentMod.Integration.TModLoader.Players;
using OccidentMod.Presentation.Audio;
using Terraria;
using Terraria.ID;

namespace OccidentMod.Networking;

internal static class PokaCallSync
{
    internal const byte MessageType = 6;
    private const float HearingRangeSquared = 1000f * 1000f;
    private const float PitchRange = 0.04f;

    internal static void Update(PokaRider rider, ref int delay)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || rider.Player.dead)
            return;

        if (delay == 0)
            delay = Random.Shared.Next(7 * 60, 16 * 60 + 1);
        else if (--delay == 0)
            Emit(rider);
    }

    private static void Emit(PokaRider rider)
    {
        Player source = rider.Player;
        if (Main.netMode == NetmodeID.MultiplayerClient || !source.active || source.dead ||
            !PokaMount.IsMounted(source))
            return;

        Vector2 position = source.Center;
        float pitch = (Random.Shared.NextSingle() * 2f - 1f) * PitchRange;
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            PokaCallAudio.Play(source.whoAmI, position, pitch);
            return;
        }

        var packet = rider.Mod.GetPacket();
        packet.Write(MessageType);
        packet.Write((byte)source.whoAmI);
        packet.Write(position.X);
        packet.Write(position.Y);
        packet.Write(pitch);
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player listener = Main.player[i];
            if (listener.active && Vector2.DistanceSquared(listener.Center, position) <= HearingRangeSquared)
                packet.Send(toClient: i);
        }
    }

    internal static void Receive(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || Main.dedServ)
            return;

        int source = reader.ReadByte();
        Vector2 position = new(reader.ReadSingle(), reader.ReadSingle());
        float pitch = reader.ReadSingle();
        if (source >= Main.maxPlayers || !Main.player[source].active ||
            !float.IsFinite(position.X) || !float.IsFinite(position.Y) ||
            !float.IsFinite(pitch) || Math.Abs(pitch) > PitchRange ||
            Vector2.DistanceSquared(Main.LocalPlayer.Center, position) > HearingRangeSquared)
            return;

        PokaCallAudio.Play(source, position, pitch);
    }
}
