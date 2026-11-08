#nullable enable

using System;
using Microsoft.Xna.Framework;
using OccidentMod.Content.Tiles.SporeDream;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Commands;

/// <summary>
///     孢夢之森開發者測試指令。
///     Developer commands for Spore Dream Forest.
/// </summary>
public sealed class SporeDreamDebugCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "sporedream";

    public override string Usage =>
        "/sporedream status | audit | tp";

    public override string Description =>
        "查看孢夢之森狀態、生成統計，或傳送到森林。";

    /// <summary>
    ///     處理開發者輸入的測試指令。
    ///     Handle developer debug commands.
    /// </summary>
    public override void Action(
        CommandCaller caller,
        string input,
        string[] args)
    {
        var action = args.Length == 0
            ? "status"
            : args[0].ToLowerInvariant();

        switch (action)
        {
            case "status":
                ShowStatus(caller);
                break;

            case "audit":
                ShowAudit(caller);
                break;

            case "tp":
                TeleportToForest(caller);
                break;

            default:
                caller.Reply(
                    "未知的測試指令。可使用：" +
                    "/sporedream status、" +
                    "/sporedream audit、" +
                    "/sporedream tp。");
                break;
        }
    }

    /// <summary>
    ///     顯示世界尺寸與森林狀態。
    ///     Display world dimensions and forest status.
    /// </summary>
    private static void ShowStatus(CommandCaller caller)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        caller.Reply("【孢夢之森－狀態】");

        caller.Reply(
            $"世界尺寸：{Main.maxTilesX} × " +
            $"{Main.maxTilesY} 格");

        caller.Reply(
            $"世界大小：{GetWorldSizeName()}");

        if (!forest.Exists)
        {
            caller.Reply(
                "森林狀態：目前世界沒有孢夢之森。");
            return;
        }

        caller.Reply(
            $"森林狀態：{(forest.Ready ? "已就緒" : "尚未就緒")}");

        caller.Reply(
            $"森林範圍：X {forest.Left}～" +
            $"{forest.Right - 1}");

        caller.Reply(
            $"森林寬度：{forest.Right - forest.Left} 格");

        if (forest.Surface.Length > 0)
        {
            GetSurfaceRange(
                forest.Surface,
                out var minimum,
                out var maximum);

            caller.Reply(
                $"地表高度：Y {minimum}～{maximum}");
        }

        var playerTile =
            caller.Player.Center.ToTileCoordinates();

        caller.Reply(
            $"目前位置：X {playerTile.X}、" +
            $"Y {playerTile.Y}");

        caller.Reply(
            $"是否位於森林：{(forest.Contains(
                playerTile.X,
                playerTile.Y) ? "是" : "否")}");
    }

    /// <summary>
    ///     統計孢夢之森地塊與巨菇數量。
    ///     Audit tiles and giant mushrooms in the forest.
    /// </summary>
    private static void ShowAudit(CommandCaller caller)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        caller.Reply("【孢夢之森－生成統計】");

        if (!forest.Exists ||
            forest.Surface.Length == 0)
        {
            caller.Reply(
                "統計失敗：目前世界沒有孢夢之森。");
            return;
        }

        var grassType =
            ModContent.TileType<SporeDreamGrass>();

        var plantType =
            ModContent.TileType<SporeDreamPlants>();

        var straightType =
            ModContent.TileType<SporeDreamGiant>();

        var curvedType =
            ModContent.TileType<SporeDreamCurvedGiant>();

        var grassTiles = 0;
        var plantTiles = 0;

        var straightCaps = 0;
        var straightBodyTiles = 0;

        var curvedTiles = 0;

        var left = Math.Max(1, forest.Left);
        var right = Math.Min(
            Main.maxTilesX - 2,
            forest.Right - 1);

        // 由世界頂部掃描到森林地表下方，
        // 避免高大的直立巨菇超出原本的統計高度。
        //
        // Scan from the top of the world to below
        // the forest surface to include tall crowns.
        for (var x = left; x <= right; x++)
        {
            var surfaceIndex = x - forest.Left;

            if (surfaceIndex < 0 ||
                surfaceIndex >= forest.Surface.Length)
                continue;

            var bottom = Math.Min(
                Main.maxTilesY - 2,
                forest.Surface[surfaceIndex] + 70);

            for (var y = 1; y <= bottom; y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile)
                    continue;

                int type = tile.TileType;

                if (type == grassType)
                {
                    grassTiles++;
                    continue;
                }

                if (type == plantType)
                {
                    plantTiles++;
                    continue;
                }

                if (type == curvedType)
                {
                    curvedTiles++;
                    continue;
                }

                if (type != straightType)
                    continue;

                // 菌蓋使用框架 Y = 0。
                // 菇柄使用其他框架 Y 值。
                //
                // Crown tiles use frame Y = 0.
                // Stem tiles use other frame Y values.
                if (tile.TileFrameY == 0)
                {
                    // 雙欄直立巨菇只統計主欄。
                    // Count only the primary half
                    // of paired straight mushrooms.
                    if (SporeDreamGiant.IsPrimaryTile(x, y))
                        straightCaps++;
                }
                else
                {
                    straightBodyTiles++;
                }
            }
        }

        GetSurfaceRange(
            forest.Surface,
            out var minimum,
            out var maximum);

        caller.Reply(
            $"森林範圍：X {forest.Left}～" +
            $"{forest.Right - 1}");

        caller.Reply(
            $"森林寬度：{forest.Right - forest.Left} 格");

        caller.Reply(
            $"地表高度：Y {minimum}～{maximum}");

        caller.Reply(
            $"孢夢草皮：{grassTiles} 格");

        caller.Reply(
            $"森林植物：{plantTiles} 格");

        caller.Reply(
            $"直立巨菇（完整菌蓋）：{straightCaps} 株");

        caller.Reply(
            $"直立巨菇菇柄：{straightBodyTiles} 格");

        caller.Reply(
            $"彎曲巨菇：{curvedTiles} 格");

        caller.Reply(
            "註：彎曲巨菇統計的是占用地塊數，" +
            "不是實際株數。");

        if (straightCaps == 0 &&
            straightBodyTiles > 0)
            caller.Reply(
                "提醒：偵測到直立菇柄，" +
                "但沒有偵測到完整菌蓋。" +
                "可能是菌蓋被砍除，或框架資料需要檢查。");
        else if (straightCaps > 0)
            caller.Reply(
                "直立巨菇檢查：已偵測到完整菌蓋。");
        else
            caller.Reply(
                "直立巨菇檢查：掃描範圍內沒有" +
                "偵測到直立巨菇地塊。");

        if (curvedTiles == 0)
            caller.Reply(
                "提醒：目前掃描範圍內沒有" +
                "偵測到彎曲巨菇地塊。");

        caller.Reply("生成統計完成。");
    }

    /// <summary>
    ///     傳送開發者到孢夢之森的安全位置。
    ///     Teleport the developer to a safe forest location.
    /// </summary>
    private static void TeleportToForest(CommandCaller caller)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists ||
            forest.Surface.Length == 0)
        {
            caller.Reply(
                "傳送失敗：目前世界沒有孢夢之森。");
            return;
        }

        var player = caller.Player;

        if (!player.active || player.dead)
        {
            caller.Reply(
                "傳送失敗：角色目前無法傳送。");
            return;
        }

        var middle =
            (forest.Left + forest.Right) / 2;

        // 由森林中央往兩側尋找安全位置。
        // Search outward from the forest center.
        for (var distance = 0;
             distance < forest.Surface.Length / 2;
             distance++)
            foreach (var direction in new[] { 1, -1 })
            {
                var x = middle + distance * direction;

                if (x < forest.Left ||
                    x >= forest.Right)
                    continue;

                var ground =
                    forest.Surface[x - forest.Left];

                // 嘗試不同高度，避免傳送到巨菇或其他障礙內。
                // Try several heights above ground.
                for (var lift = 3; lift <= 12; lift++)
                {
                    Vector2 destination = new(
                        x * 16f + 8f - player.width / 2f,
                        (ground - lift) * 16f -
                        player.height);

                    if (!IsSafeTeleport(
                            destination,
                            player.width,
                            player.height))
                        continue;

                    player.Teleport(destination, 1);

                    if (Main.netMode ==
                        NetmodeID.MultiplayerClient)
                        NetMessage.SendData(
                            MessageID.TeleportEntity,
                            -1,
                            -1,
                            null,
                            0,
                            player.whoAmI,
                            destination.X,
                            destination.Y,
                            1);

                    caller.Reply(
                        $"傳送成功：已抵達孢夢之森，" +
                        $"目標 X {x}、Y {ground - lift}。");

                    return;
                }
            }

        caller.Reply(
            "傳送失敗：森林內找不到安全的落腳位置。");
    }

    /// <summary>
    ///     檢查目標位置是否可以安全放入玩家。
    ///     Check whether a player fits safely at destination.
    /// </summary>
    private static bool IsSafeTeleport(
        Vector2 position,
        int width,
        int height)
    {
        var left =
            (int)MathF.Floor(position.X / 16f);

        var top =
            (int)MathF.Floor(position.Y / 16f);

        var right =
            (int)MathF.Ceiling(
                (position.X + width) / 16f);

        var bottom =
            (int)MathF.Ceiling(
                (position.Y + height) / 16f);

        if (!WorldGen.InWorld(left, top, 10) ||
            !WorldGen.InWorld(right, bottom, 10))
            return false;

        return !Collision.SolidCollision(
            position,
            width,
            height);
    }

    /// <summary>
    ///     取得世界大小的中文名稱。
    ///     Get the Chinese world size label.
    /// </summary>
    private static string GetWorldSizeName()
    {
        return Main.maxTilesX switch
        {
            4200 => "小型",
            6400 => "中型",
            8400 => "大型",
            12000 when Main.maxTilesY == 3600 =>
                "特大型",

            _ => "自訂尺寸"
        };
    }

    /// <summary>
    ///     計算森林地表最高與最低位置。
    ///     Get the minimum and maximum surface height.
    /// </summary>
    private static void GetSurfaceRange(
        int[] surface,
        out int minimum,
        out int maximum)
    {
        minimum = int.MaxValue;
        maximum = int.MinValue;

        foreach (var height in surface)
        {
            minimum = Math.Min(minimum, height);
            maximum = Math.Max(maximum, height);
        }

        if (surface.Length == 0)
        {
            minimum = 0;
            maximum = 0;
        }
    }
}