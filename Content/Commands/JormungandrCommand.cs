#nullable enable

using System;
using Microsoft.Xna.Framework;
using OccidentMod.Content.NPCs.Bosses;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Commands;

/// <summary>
///     耶夢加得開發者測試指令。
///     Developer testing commands for Jormungandr.
/// </summary>
public sealed class JormungandrCommand : ModCommand
{
    private const int DefaultSegments = 24;
    private const int MinimumSegments = 14;
    private const int MaximumSegments = 48;

    // 單人直接執行，多人交由伺服器執行。
    // Execute locally in singleplayer and on server in multiplayer.
    public override CommandType Type => CommandType.World;

    public override string Command => "jormungandr";

    public override string Usage =>
        "/jormungandr [14-48|clear]";

    public override string Description =>
        "召喚耶夢加得測試個體、指定節數或清除個體。";

    /// <summary>
    ///     處理召喚與清除指令。
    ///     Handle spawn and clear commands.
    /// </summary>
    public override void Action(
        CommandCaller caller,
        string input,
        string[] args)
    {
        if (args.Length > 1)
        {
            caller.Reply("參數過多。用法：" + Usage);
            return;
        }

        var clear = args.Length == 1 &&
                    args[0].Equals(
                        "clear",
                        StringComparison.OrdinalIgnoreCase);

        var segments = DefaultSegments;

        if (!clear && args.Length == 1)
            if (!int.TryParse(args[0], out segments) ||
                segments < MinimumSegments ||
                segments > MaximumSegments)
            {
                caller.Reply(
                    "節數必須介於 14～48。用法：" + Usage);
                return;
            }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            caller.Reply(
                "此指令必須由伺服器執行。");
            return;
        }

        var npcType = ModContent.NPCType<Jormungandr>();

        var removed = ClearExisting(npcType);

        if (clear)
        {
            caller.Reply(
                $"已清除 {removed} 個耶夢加得測試個體。");
            return;
        }

        var player = caller.Player;

        if (player is null ||
            !player.active ||
            player.dead)
        {
            caller.Reply(
                "召喚失敗：找不到有效的玩家。");
            return;
        }

        // 在玩家右上方召喚測試個體。
        // Spawn the preview entity above and to the right.
        var position =
            player.Center + new Vector2(260f, -220f);

        var npcId = NPC.NewNPC(
            player.GetSource_Misc("JormungandrDebug"),
            (int)position.X,
            (int)position.Y,
            npcType,
            0,
            segments,
            player.whoAmI);

        if (npcId < 0 || npcId >= Main.maxNPCs)
        {
            caller.Reply(
                "召喚失敗：NPC 欄位不足或生成失敗。");
            return;
        }

        var npc = Main.npc[npcId];

        if (!npc.active || npc.type != npcType)
        {
            caller.Reply(
                "召喚失敗：耶夢加得沒有成功建立。");
            return;
        }

        // 同步 NPC 的節數與所屬玩家。
        // Synchronize segment count and owner.
        npc.ai[0] = segments;
        npc.ai[1] = player.whoAmI;
        npc.netUpdate = true;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(
                MessageID.SyncNPC,
                number: npcId);

        caller.Reply(
            $"耶夢加得召喚成功！節數：{segments}，" +
            $"NPC ID：{npcId}。");
    }

    /// <summary>
    ///     清除現有耶夢加得測試個體。
    ///     Remove existing Jormungandr preview entities.
    /// </summary>
    private static int ClearExisting(int npcType)
    {
        var removed = 0;

        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];

            if (!npc.active || npc.type != npcType)
                continue;

            npc.active = false;
            npc.netUpdate = true;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(
                    MessageID.SyncNPC,
                    number: i);

            removed++;
        }

        return removed;
    }
}