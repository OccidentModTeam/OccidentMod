using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Common.Worlds;

namespace Occident.Content.Biomes;

public sealed class SporeDreamForest : ModBiome
{
    public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => Main.dedServ
        ? null : ModContent.GetInstance<SporeDreamSurfaceBackgroundStyle>();
    public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
    public override int Music => Main.dayTime ? MusicID.Night : MusicID.OverworldDay;
    public override string BestiaryIcon => "Terraria/Images/Item_5";
    public override string BackgroundPath => "Terraria/Images/MapBG1";
    public override bool IsBiomeActive(Player player)
    {
        var point=player.Center.ToTileCoordinates();
        return SporeDreamForestSystem.Instance.Contains(point.X,point.Y);
    }
}
