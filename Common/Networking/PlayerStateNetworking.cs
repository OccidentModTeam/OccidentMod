// 擷取、傳送與驗證職業及休閒模式，伺服器統一轉送接受的狀態。
using System.IO;
using Occident.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Common.Networking;

internal static class PlayerStateNetworking
{
    // 把分散在兩個 ModPlayer 的資料組成可比較、可序列化的快照。
    internal static PlayerSyncState Capture(Player player)
    {
        var classes = player.GetModPlayer<ClassPlayer>();
        return new(classes.CurrentClass, classes.HasInitialClass,
            player.GetModPlayer<CasualModePlayer>().IsActive);
    }

    internal static void Send(Mod mod, Player player, int toWho = -1, int ignoreWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer || player.whoAmI < 0 || player.whoAmI >= Main.maxPlayers) return;
        ModPacket packet = mod.GetPacket();
        packet.Write(PlayerSyncState.MessageType);
        packet.Write((byte)player.whoAmI);
        Capture(player).Write(packet);
        packet.Send(toWho, ignoreWho);
    }

    internal static void Receive(Mod mod, BinaryReader reader, int whoAmI, byte? messageType=null)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) return;
        bool server = Main.netMode == NetmodeID.Server;
        if (!PlayerSyncState.TryReadPacket(reader, Main.maxPlayers, server, whoAmI,
            out int index, out var received,messageType)) return;

        Player player = Main.player[index];
        var classes = player.GetModPlayer<ClassPlayer>();
        var accepted = received.Normalize(player.difficulty == 0, server && classes.HasInitialClass);
        classes.ApplyNetworkState(accepted.Class, accepted.HasInitialClass);
        player.GetModPlayer<CasualModePlayer>().Enabled = accepted.CasualMode;
        if (server) {
            // 只將驗證後的狀態轉送給其他客戶端；若內容經過修正，也回傳給原玩家。
            Send(mod, player, -1, whoAmI);
            if (accepted != received) Send(mod, player, whoAmI);
        }
    }
}
