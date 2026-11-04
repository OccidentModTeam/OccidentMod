// Jormungandr 的本機造型預覽指令，不代表正式首領玩法。
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.NPCs.Prototypes;

namespace Occident.Content.Commands;

public sealed class JormungandrCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "jormungandr";

    public override string Usage => "/jormungandr [14-48|clear]";

    public override string Description =>
        "Spawn a harmless Jormungandr art preview with a custom segment count.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
        {
            caller.Reply("Art preview currently supports single player only.");
            return;
        }

        int count = 24;

        bool clear = args.Length == 1 &&
                     args[0].Equals("clear", StringComparison.OrdinalIgnoreCase);

        if (args.Length > 1 ||
            (!clear &&
             args.Length == 1 &&
             (!int.TryParse(args[0], out count) || count < 14 || count > 48)))
        {
            caller.Reply(Usage);
            return;
        }

        int type = ModContent.NPCType<Jormungandr>();

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type == type)
                npc.active = false;
        }

        if (clear)
        {
            caller.Reply("Jormungandr preview cleared.");
            return;
        }

        Player player = caller.Player;

        NPC.NewNPC(
            player.GetSource_Misc("JormungandrArtPreview"),
            (int)player.Center.X + 260,
            (int)player.Center.Y - 220,
            type,
            ai0: count,
            ai1: player.whoAmI);

        caller.Reply(
            $"Jormungandr: {count} segments. Harmless visual prototype. /jormungandr clear to remove.");
    }
}