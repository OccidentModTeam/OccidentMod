using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.Players;

internal sealed class ResonancePreviewPlayer : ModPlayer
{
    private const int ChargeTicks = 30 * 60;
    private int elapsedTicks;

    public float Fraction => (float)elapsedTicks / ChargeTicks;
    public bool IsFull => elapsedTicks >= ChargeTicks;

    public override void OnEnterWorld() => elapsedTicks = 0;

    public override void PostUpdate()
    {
        if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer &&
            !Player.dead && !Player.ghost && !IsFull)
        {
            elapsedTicks++;
        }
    }
}
