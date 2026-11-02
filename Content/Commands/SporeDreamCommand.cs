using Terraria;
using Terraria.ModLoader;
using Occident.Common.Worlds;

namespace Occident.Content.Commands;

public sealed class SporeDreamCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "sporedream";
    public override string Usage => "/sporedream";
    public override string Description => "Show Spore Dream Forest location without modifying the world.";
    public override void Action(CommandCaller caller,string input,string[] args)
    {
        var forest=SporeDreamForestSystem.Instance;
        if(!forest.Exists) { caller.Reply("This world has no generated Spore Dream Forest. Create a new world to generate it."); return; }
        caller.Reply($"Spore Dream Forest: tiles X {forest.Left}–{forest.Right-1}; depth {SporeDreamForestSystem.UndergroundDepth}. Surface barrier only.");
    }
}
