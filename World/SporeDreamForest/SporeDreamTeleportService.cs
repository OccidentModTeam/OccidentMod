#nullable enable

using System;
using Microsoft.Xna.Framework;
using OccidentMod.Networking;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     F6 與背包按鈕共用的測試傳送實作。
///     Shared test teleport service for keyboard and UI.
/// </summary>
internal static class SporeDreamTeleportService
{
    // 各玩家下一次允許傳送的遊戲刻數。
    // Next permitted teleport tick for each player.
    private static readonly ulong[] NextAllowedTick =
        new ulong[Main.maxPlayers];

    /// <summary>
    ///     重設玩家傳送冷卻紀錄。
    ///     Reset teleport cooldown records.
    /// </summary>
    internal static void Reset()
    {
        Array.Clear(NextAllowedTick);
    }

    /// <summary>
    ///     由本機快捷鍵或介面提出傳送要求。
    ///     Request teleportation from keyboard or UI.
    /// </summary>
    internal static void Request(Mod mod)
    {
        if (Main.gameMenu || Main.dedServ)
            return;

        var player = Main.LocalPlayer;

        if (!player.active || player.dead)
            return;

        if (!SporeDreamEnvironmentSystem.Instance.Exists)
        {
            Main.NewText(
                "傳送失敗：目前世界沒有孢夢之森。",
                230,
                190,
                150);

            return;
        }

        // 多人模式交由伺服器決定傳送位置。
        // Multiplayer requests are handled by the server.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = mod.GetPacket();

            packet.Write(
                PacketIds.SporeDreamTeleportRequest);

            packet.Send();
            return;
        }

        TryTeleport(player);
    }

    /// <summary>
    ///     伺服器接收玩家的傳送請求。
    ///     Handle a player teleport request on the server.
    /// </summary>
    internal static void ReceiveRequest(int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server ||
            whoAmI < 0 ||
            whoAmI >= Main.maxPlayers)
            return;

        TryTeleport(Main.player[whoAmI]);
    }

    /// <summary>
    ///     將玩家傳送到森林內的安全位置。
    ///     Teleport a player to a safe forest location.
    /// </summary>
    internal static bool TryTeleport(Player player)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient ||
            !player.active ||
            player.dead ||
            player.whoAmI < 0 ||
            player.whoAmI >= NextAllowedTick.Length)
            return false;

        var playerId = player.whoAmI;

        // 限制連續傳送，避免重複請求。
        // Prevent repeated teleport requests.
        if (Main.GameUpdateCount <
            NextAllowedTick[playerId])
            return false;

        NextAllowedTick[playerId] =
            Main.GameUpdateCount + 60;

        if (!TryFindDestination(
                player,
                out var destination))
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(
                    "傳送失敗：找不到安全的落腳位置。",
                    230,
                    190,
                    150);

            return false;
        }

        // 多人模式先同步目的地附近的地圖區塊。
        // Synchronize destination sections before teleporting.
        if (Main.netMode == NetmodeID.Server)
            RemoteClient.CheckSection(
                playerId,
                destination);

        player.Teleport(
            destination,
            TeleportationStyleID.RodOfDiscord);

        player.velocity = Vector2.Zero;

        player.fallStart =
            (int)(destination.Y / 16f);

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(
                MessageID.TeleportEntity,
                number: 0,
                number2: playerId,
                number3: destination.X,
                number4: destination.Y,
                number5: TeleportationStyleID.RodOfDiscord);
        else
            Main.NewText(
                "已傳送至孢夢之森。",
                150,
                225,
                170);

        return true;
    }

    /// <summary>
    ///     搜尋森林中央附近的安全落腳位置。
    ///     Find a safe destination near the forest center.
    /// </summary>
    private static bool TryFindDestination(
        Player player,
        out Vector2 destination)
    {
        destination = default;

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists ||
            forest.Surface.Length == 0)
            return false;

        var center = forest.Surface.Length / 2;

        // 從森林中央向左右兩側搜尋。
        // Search outward from the forest center.
        for (var radius = 0;
             radius < forest.Surface.Length;
             radius++)
        {
            var sideCount = radius == 0 ? 1 : 2;

            for (var side = 0;
                 side < sideCount;
                 side++)
            {
                var column = center +
                             (side == 0 ? radius : -radius);

                if (column < 1 ||
                    column >= forest.Surface.Length - 2)
                    continue;

                var x = forest.Left + column;

                var ground = forest.Surface[column];

                // 檢查地表附近是否有可站立的平台。
                // Find solid ground near the stored surface.
                for (var y = Math.Max(3, ground - 8);
                     y <= Math.Min(
                         Main.maxTilesY - 3,
                         ground + 20);
                     y++)
                {
                    if (!WorldGen.InWorld(x, y, 3) ||
                        !WorldGen.SolidTile(x, y) ||
                        !WorldGen.SolidTile(x + 1, y))
                        continue;

                    Vector2 position = new(
                        x * 16 + 16 -
                        player.width * 0.5f,
                        y * 16 - player.height);

                    // 避免將角色傳進實體地塊。
                    // Avoid teleporting into solid tiles.
                    if (Collision.SolidCollision(
                            position,
                            player.width,
                            player.height))
                        continue;

                    var liquid = false;

                    var left =
                        (int)(position.X / 16f);

                    var right =
                        (int)((position.X +
                            player.width - 1) / 16f);

                    var top =
                        (int)(position.Y / 16f);

                    // 避免選到被液體覆蓋的位置。
                    // Reject submerged destinations.
                    for (var tx = left;
                         tx <= right && !liquid;
                         tx++)
                    for (var ty = top;
                         ty < y;
                         ty++)
                        if (!WorldGen.InWorld(
                                tx,
                                ty,
                                1) ||
                            Main.tile[tx, ty]
                                .LiquidAmount > 0)
                        {
                            liquid = true;
                            break;
                        }

                    if (liquid)
                        continue;

                    destination = position;
                    return true;
                }
            }
        }

        return false;
    }
}