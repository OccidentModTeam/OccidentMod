using VesperMod.Content.Items.Mounts;
using Terraria;
using Terraria.ModLoader;

namespace VesperMod.Content.Commands;

public sealed class PokaCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "poka";
    public override string Description => "Get Poka's mount whistle";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Player player = caller.Player;
        player.QuickSpawnItem(player.GetSource_Misc("PokaCommand"), ModContent.ItemType<PokaWhistle>());
    }
}
