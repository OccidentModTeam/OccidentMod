using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.NPCs.Prototypes;

namespace Occident.Common.Worlds;

// Opt-in local art-review launch only. Normal games never auto-spawn this NPC.
public sealed class FungalWyrmPreviewSystem : ModSystem
{
    private bool spawned;
    public override void OnWorldLoad() => spawned = false;
    public override void PostUpdateWorld()
    {
        if (spawned || Main.netMode != NetmodeID.SinglePlayer || Main.gameMenu
            || Environment.GetEnvironmentVariable("OCCIDENT_WYRM_PREVIEW") != "1") return;
        Player player = Main.LocalPlayer;
        if (!player.active || player.dead) return;
        spawned = true;
        int id = NPC.NewNPC(player.GetSource_Misc("FungalWyrmOptInReview"),
            (int)player.Center.X + 260, (int)player.Center.Y - 220,
            ModContent.NPCType<FungalWyrmPreview>(), ai0: 24, ai1: player.whoAmI);
        Mod.Logger.Info($"Fungal wyrm opt-in preview spawned: NPC {id}, 24 segments.");
    }
}
