using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.World.Biomes;

/// <summary>
///     孢夢之森獨立生態域。
///     Independent Spore Dream Forest biome.
/// </summary>
public sealed class SporeDreamForestBiome : ModBiome
{
    // 使用孢夢之森專屬地表背景。
    // Use the dedicated forest surface background.
    public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle =>
        Main.dedServ
            ? null
            : ModContent.GetInstance<
                SporeDreamSurfaceBackgroundStyle>();

    public override SceneEffectPriority Priority =>
        SceneEffectPriority.BiomeHigh;

    public override int Music =>
        Main.dayTime
            ? MusicID.Night
            : MusicID.OverworldDay;

    public override string BestiaryIcon =>
        "Terraria/Images/Item_5";

    public override string BackgroundPath =>
        "Terraria/Images/MapBG1";

    public override bool IsBiomeActive(Player player)
    {
        if (player == null || !player.active)
            return false;

        var position =
            player.Center.ToTileCoordinates();

        return SporeDreamEnvironmentSystem.Instance.Contains(
            position.X,
            position.Y
        );
    }
}