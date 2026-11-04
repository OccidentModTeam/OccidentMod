// 明確啟用環境變數時才建立 Jormungandr 造型預覽。
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.NPCs.Prototypes;

namespace Occident.Common.Worlds;

// 只有明確啟用本機美術預覽時才自動生成；一般遊戲不會自動生成這個 NPC。
public sealed class JormungandrPreviewSystem : ModSystem
{
    private bool spawned;

    public override void OnWorldLoad() => spawned = false;

    public override void PostUpdateWorld()
    {
        if (spawned
            || Main.netMode != NetmodeID.SinglePlayer
            || Main.gameMenu
            || Environment.GetEnvironmentVariable("OCCIDENT_JORMUNGANDR_PREVIEW") != "1")
        {
            return;
        }

        Player player = Main.LocalPlayer;

        if (!player.active || player.dead)
        {
            return;
        }

        spawned = true;

        int id = NPC.NewNPC(
            player.GetSource_Misc("JormungandrPreview"),
            (int)player.Center.X + 260,
            (int)player.Center.Y - 220,
            ModContent.NPCType<Jormungandr>(),
            ai0: 24,
            ai1: player.whoAmI);

        Mod.Logger.Info($"Jormungandr preview spawned: NPC {id}, 24 segments.");
    }
}