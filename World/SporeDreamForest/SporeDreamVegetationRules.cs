using System;
using OccidentMod.Content.Tiles.SporeDream;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森植被放置規則。
///     Spore Dream Forest vegetation placement rules.
/// </summary>
internal static class SporeDreamVegetationRules
{
    /// <summary>
    ///     檢查致幻果生長空間及間距。
    ///     Validate Pandora fruit placement.
    /// </summary>
    internal static bool CanPlaceFruit(
        int x,
        int ground)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists ||
            x < forest.Left ||
            x + 1 >= forest.Right)
            return false;

        if (!WorldGen.InWorld(x + 1, ground, 2))
            return false;

        var grass =
            ModContent.TileType<SporeDreamGrass>();

        var fruit =
            ModContent.TileType<PandoraPlant>();

        // 確認兩格寬的地面。
        // Validate two-tile ground support.
        for (var dx = 0; dx < 2; dx++)
        {
            var support = Main.tile[x + dx, ground];

            if (!support.HasTile ||
                support.TileType != grass ||
                support.IsHalfBlock ||
                support.Slope != 0)
                return false;

            for (var dy = 1; dy <= 2; dy++)
            {
                var space =
                    Main.tile[x + dx, ground - dy];

                if (space.HasTile ||
                    space.LiquidAmount > 0)
                    return false;
            }
        }

        // 限制附近致幻果的密度。
        // Limit nearby Pandora fruit density.
        var left = Math.Max(1, x - 28);
        var right = Math.Min(
            Main.maxTilesX - 2,
            x + 28
        );

        var top = Math.Max(1, ground - 16);
        var bottom = Math.Min(
            Main.maxTilesY - 2,
            ground + 16
        );

        for (var scanX = left;
             scanX <= right;
             scanX++)
        for (var scanY = top;
             scanY <= bottom;
             scanY++)
        {
            var tile = Main.tile[scanX, scanY];

            if (tile.HasTile &&
                tile.TileType == fruit)
                return false;
        }

        return true;
    }
}