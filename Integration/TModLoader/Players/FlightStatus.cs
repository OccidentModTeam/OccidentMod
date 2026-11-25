using Terraria;

namespace OccidentMod.Integration.TModLoader.Players;

internal readonly record struct FlightStatus(float Remaining, int Maximum, bool IsInfinite)
{
    public bool HasFlight => Maximum > 0;

    public static FlightStatus Read(Player player)
    {
        if (!player.active || player.dead || player.ghost || player.wingsLogic <= 0 || player.wingTimeMax <= 0)
            return default;

        // Reserved for a future explicit infinite-flight provider.
        return new FlightStatus(player.wingTime, player.wingTimeMax, IsInfinite: false);
    }
}
