using System.IO;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Networking;

/// <summary>
///     負責擷取、傳送及驗證玩家職業與休閒模式狀態。
///     Captures, transmits, and validates player class
///     and casual mode states.
/// </summary>
internal static class PlayerStateNetworking
{
    /// <summary>
    ///     擷取玩家目前的職業與休閒模式狀態。
    ///     Captures the player's current class and casual mode state.
    /// </summary>
    internal static PlayerSyncState Capture(Player player)
    {
        var classes = player.GetModPlayer<ClassPlayer>();

        return new PlayerSyncState(
            classes.CurrentClass,
            classes.HasInitialClass,
            player.GetModPlayer<CasualModePlayer>().IsActive
        );
    }

    /// <summary>
    ///     向指定接收端發送玩家狀態封包。
    ///     Sends a player state packet to the specified recipient.
    /// </summary>
    internal static void Send(
        Mod mod,
        Player player,
        int toWho = -1,
        int ignoreWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer ||
            player.whoAmI < 0 ||
            player.whoAmI >= Main.maxPlayers)
            return;

        var packet = mod.GetPacket();

        packet.Write(PlayerSyncState.MessageType);
        packet.Write((byte)player.whoAmI);

        Capture(player).Write(packet);

        packet.Send(toWho, ignoreWho);
    }

    /// <summary>
    ///     接收、驗證並套用玩家狀態，伺服器負責轉送合法資料。
    ///     Receives, validates, and applies player state,
    ///     with the server relaying accepted data.
    /// </summary>
    internal static void Receive(
        Mod mod,
        BinaryReader reader,
        int whoAmI,
        byte? messageType = null)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        var server = Main.netMode == NetmodeID.Server;

        if (!PlayerSyncState.TryReadPacket(
                reader,
                Main.maxPlayers,
                server,
                whoAmI,
                out var index,
                out var received,
                messageType))
            return;

        var player = Main.player[index];
        var classes = player.GetModPlayer<ClassPlayer>();

        var accepted = received.Normalize(
            player.difficulty == 0,
            server && classes.HasInitialClass
        );

        classes.ApplyNetworkState(
            accepted.Class,
            accepted.HasInitialClass
        );

        player.GetModPlayer<CasualModePlayer>().Enabled =
            accepted.CasualMode;

        if (server)
        {
            // 只轉送通過驗證的狀態。
            // Relay only validated state data.
            Send(mod, player, -1, whoAmI);

            // 若伺服器修正了資料，將修正結果回傳原玩家。
            // Return corrected data to the original sender.
            if (accepted != received) Send(mod, player, whoAmI);
        }
    }
}