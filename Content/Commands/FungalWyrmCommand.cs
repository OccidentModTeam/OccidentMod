using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.NPCs.Prototypes;

namespace Occident.Content.Commands;

public sealed class FungalWyrmCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "fungalwyrm";
    public override string Usage => "/fungalwyrm [14-48|clear]";
    public override string Description => "Spawn a harmless fungal wyrm art preview with a custom segment count.";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.netMode != NetmodeID.SinglePlayer) { caller.Reply("Art preview currently supports single player only."); return; }
        int count = 24;
        bool clear = args.Length == 1 && args[0].Equals("clear", StringComparison.OrdinalIgnoreCase);
        if (args.Length > 1 || (!clear && args.Length == 1 && (!int.TryParse(args[0], out count) || count < 14 || count > 48))) {
            caller.Reply(Usage); return;
        }
        int type = ModContent.NPCType<FungalWyrmPreview>();
        foreach (NPC npc in Main.ActiveNPCs) if (npc.type == type) npc.active = false;
        if (clear) { caller.Reply("Fungal wyrm preview cleared."); return; }
        Player player = caller.Player;
        NPC.NewNPC(player.GetSource_Misc("FungalWyrmArtPreview"), (int)player.Center.X + 260,
            (int)player.Center.Y - 220, type, ai0: count, ai1: player.whoAmI);
        caller.Reply($"Fungal wyrm: {count} segments. Harmless visual prototype. /fungalwyrm clear to remove.");
    }
}
