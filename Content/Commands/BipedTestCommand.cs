using Microsoft.Xna.Framework;
using VesperMod.Content.NPCs.Enemies;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Content.Commands;

public sealed class BipedTestCommand : ModCommand
{
    public override CommandType Type => CommandType.World;
    public override string Command => "bipedtest";
    public override string Description => "Spawn Highwatcher for testing";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Player player = caller.Player;
        if (player is null || !player.active || player.dead || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        int type = ModContent.NPCType<ArticulatedBiped>();
        Vector2 feet = player.Bottom + new Vector2(player.direction * 96f, 0f);
        Vector2 topLeft = feet - new Vector2(ArticulatedBiped.CollisionWidth / 2f, ArticulatedBiped.CollisionHeight);
        if (Collision.SolidCollision(topLeft, ArticulatedBiped.CollisionWidth, ArticulatedBiped.CollisionHeight))
        {
            caller.Reply("Move to an open area with enough space in front of you.");
            return;
        }

        int index = NPC.NewNPC(player.GetSource_Misc("BipedTest"), (int)feet.X, (int)feet.Y, type);
        if (index < Main.maxNPCs && Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: index);
    }
}
