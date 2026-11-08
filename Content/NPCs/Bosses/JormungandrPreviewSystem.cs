#nullable enable

using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.NPCs.Bosses;

/// <summary>
///     耶夢加得美術預覽系統。
///     Optional visual preview system for Jormungandr.
/// </summary>
public sealed class JormungandrPreviewSystem : ModSystem
{
    private bool spawned;

    /// <summary>
    ///     每次進入世界重設預覽狀態。
    ///     Reset preview state when entering a world.
    /// </summary>
    public override void OnWorldLoad()
    {
        spawned = false;
    }

    public override void OnWorldUnload()
    {
        spawned = false;
    }

    /// <summary>
    ///     只有明確啟用環境變數時才自動預覽。
    ///     Spawn only when explicitly enabled.
    /// </summary>
    public override void PostUpdateWorld()
    {
        if (spawned ||
            Main.netMode != NetmodeID.SinglePlayer ||
            Main.gameMenu)
            return;

        var enabled =
            Environment.GetEnvironmentVariable(
                "OCCIDENT_JORMUNGANDR_PREVIEW");

        if (enabled != "1")
            return;

        var player = Main.LocalPlayer;

        if (!player.active || player.dead)
            return;

        var npcType =
            ModContent.NPCType<Jormungandr>();

        var id = NPC.NewNPC(
            player.GetSource_Misc(
                "JormungandrPreview"),
            (int)player.Center.X + 260,
            (int)player.Center.Y - 220,
            npcType,
            ai0: 24,
            ai1: player.whoAmI);

        if (id < 0 ||
            id >= Main.maxNPCs ||
            !Main.npc[id].active)
        {
            Mod.Logger.Warn(
                "耶夢加得美術預覽生成失敗。");

            return;
        }

        spawned = true;

        Mod.Logger.Info(
            $"已生成耶夢加得美術預覽：" +
            $"NPC ID {id}，24 節。");
    }
}