#nullable enable

using System.IO;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Networking;

/// <summary>
///     模組網路封包分派入口。
///     Centralized mod packet dispatcher.
/// </summary>
internal static class PacketRouter
{
    /// <summary>
    ///     接收並分派模組網路封包。
    ///     Receive and dispatch mod network packets.
    /// </summary>
    internal static void Receive(
        Mod mod,
        BinaryReader reader,
        int whoAmI)
    {
        byte message;

        try
        {
            message = reader.ReadByte();
        }
        catch (EndOfStreamException)
        {
            return;
        }

        switch (message)
        {
            // 玩家職業及狀態同步。
            // Player class and state synchronization.
            case PacketIds.PlayerSyncState:
                PlayerStateNetworking.Receive(
                    mod,
                    reader,
                    whoAmI,
                    message
                );
                break;

            // 孢夢之森測試傳送請求。
            // Spore Dream Forest test teleport request.
            case PacketIds.SporeDreamTeleportRequest:
                if (Main.netMode == NetmodeID.Server)
                    SporeDreamTeleportService.ReceiveRequest(
                        whoAmI
                    );
                break;

            // 彎曲巨菇幾何及世界資料同步。
            // Curved mushroom geometry synchronization.
            case PacketIds.SporeDreamCurvedSync:
                if (Main.netMode ==
                    NetmodeID.MultiplayerClient)
                    SporeDreamCurvedSystem.Instance
                        .NetReceive(reader);
                break;

            // 客戶端請求播放矮菇彈跳。
            // Client requests a mushroom spring animation.
            case PacketIds.SporeDreamSpringRequest:
                if (Main.netMode == NetmodeID.Server)
                    SporeDreamCurvedSystem.Instance
                        .ReceiveSpring(
                            reader,
                            whoAmI,
                            true
                        );
                break;

            // 伺服器同步矮菇彈跳動畫。
            // Server synchronizes the spring animation.
            case PacketIds.SporeDreamSpringAnimation:
                if (Main.netMode ==
                    NetmodeID.MultiplayerClient)
                    SporeDreamCurvedSystem.Instance
                        .ReceiveSpring(
                            reader,
                            whoAmI,
                            false
                        );
                break;

            // 客戶端請求種植完整矮菇。
            // Client requests dwarf mushroom placement.
            case PacketIds.SporeDreamDwarfPlaceRequest:
                if (Main.netMode == NetmodeID.Server)
                    SporeDreamDwarfPlacement.Receive(
                        reader,
                        whoAmI
                    );
                break;

            default:
                mod.Logger.Warn(
                    $"收到未知的模組封包 ID：{message}"
                );
                break;
        }
    }
}