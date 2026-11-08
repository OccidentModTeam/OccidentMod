#nullable enable
using System;
using System.Linq;
using OccidentMod.World.Generation;
using Terraria;
using Terraria.ID;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森專屬世界生成器。
///     Spore Dream Forest world generator.
/// </summary>
internal static class SporeDreamGenerator
{
    /// <summary>
    ///     執行孢夢之森完整地形生成。
    ///     Execute Spore Dream Forest terrain generation.
    /// </summary>
    internal static void Generate(GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        // 世界尺寸必須由地塊初始化階段決定，
        // 不能在這個生成階段重新設定 Main.maxTilesX/Y。
        // World dimensions must already be initialized.
        if (context.WorldWidth != Main.maxTilesX ||
            context.WorldHeight != Main.maxTilesY)
            throw new InvalidOperationException(
                "孢夢之森生成失敗：世界尺寸與生成環境不一致。");

        forest.ClearWorld();

        // 第一階段：搜尋安全生成位置。
        // Phase 1: Find a safe generation site.
        var search =
            SporeDreamSiteSelector.Find(
                context.WorldWidth,
                context.SpawnX,
                context.InspectColumn
            );

        if (search.Sites.Count == 0)
            throw new InvalidOperationException(
                "孢夢之森生成失敗：找不到符合安全條件的地形位置。"
            );

        // 優先保留完整寬度的森林。
        // Prefer full-width forest candidates.
        var widest = search.Sites.Max(site => site.Surface.Length
        );

        var candidates =
            search.Sites
                .Where(site =>
                    site.Surface.Length == widest)
                .ToList();

        var bestScore = candidates.Min(site => site.Score
        );

        var scoreSlack =
            SporeDreamGenerationScale.Distance(
                240,
                context.WorldWidth
            );

        candidates = candidates
            .Where(site =>
                site.Score <= bestScore + scoreSlack)
            .ToList();

        var selected =
            candidates[
                WorldGen.genRand.Next(candidates.Count)
            ];

        forest.SetRegion(
            selected.Left,
            selected.Surface
        );

        if (!forest.Exists)
            throw new InvalidOperationException(
                "孢夢之森生成失敗：森林範圍登記失敗。"
            );

        // 第二階段：移除區域內原版樹木。
        // Phase 2: Remove native trees.
        ClearNativeTrees(forest);

        // 第三階段：調整自然地形。
        // Phase 3: Apply terrain variation.
        forest.VaryTerrain();

        // 第四階段：清除地面雜物。
        // Phase 4: Clear native surface rubble.
        ClearSurfaceRubble(forest);

        // 第五階段：轉換森林土壤。
        // Phase 5: Convert forest soil.
        forest.ConvertSoil();

        // 第六階段：生成巨型蘑菇。
        // Phase 6: Populate giant mushrooms.
        // 彎曲巨菇的數量限制已在系統內控管；
        // 不要在此額外呼叫 PopulateSides，避免重複生成。
        // Do not invoke side population again here.
        forest.PopulateGiants();

        // 第七階段：生成地被與致幻果。
        // Phase 7: Populate vegetation.
        forest.PopulateVegetation();

        // 完成所有必要地形後啟用生態域。
        // Activate the biome after terrain generation.
        forest.MarkGenerated();
    }

    /// <summary>
    ///     檢查是否為原版自然樹木。
    ///     Identify vanilla natural tree tiles.
    /// </summary>
    private static bool IsNativeTree(ushort type)
    {
        return type is
            TileID.Trees or
            TileID.VanityTreeSakura or
            TileID.VanityTreeYellowWillow;
    }

    /// <summary>
    ///     清除根部位於森林內的原版樹木。
    ///     Remove native trees rooted inside the forest.
    /// </summary>
    private static void ClearNativeTrees(
        SporeDreamEnvironmentSystem forest)
    {
        if (!forest.Exists)
            return;

        for (var x = forest.Left;
             x < forest.Right;
             x++)
        {
            var ground =
                forest.Surface[x - forest.Left];

            for (var y = Math.Max(1, ground - 4);
                 y <= Math.Min(
                     Main.maxTilesY - 2,
                     ground + 1);
                 y++)
            {
                var root = Main.tile[x, y];
                var soil = Main.tile[x, y + 1];

                if (!root.HasTile ||
                    !IsNativeTree(root.TileType) ||
                    !soil.HasTile ||
                    !Main.tileSolid[soil.TileType])
                    continue;

                WorldGen.KillTile(
                    x,
                    y,
                    noItem: true
                );

                break;
            }
        }
    }

    /// <summary>
    ///     清除森林地表原生碎石與裝飾堆。
    ///     Clear native rubble and decorative piles.
    /// </summary>
    private static void ClearSurfaceRubble(
        SporeDreamEnvironmentSystem forest)
    {
        if (!forest.Exists)
            return;

        for (var x = forest.Left;
             x < forest.Right;
             x++)
        {
            var ground =
                forest.Surface[x - forest.Left];

            var top = Math.Max(
                1,
                ground - 64
            );

            var bottom = Math.Min(
                Main.maxTilesY - 2,
                ground + 3
            );

            for (var y = top; y <= bottom; y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile ||
                    Main.wallHouse[tile.WallType])
                    continue;

                if (tile.TileType is not (
                    TileID.SmallPiles or
                    TileID.LargePiles or
                    TileID.LargePiles2))
                    continue;

                WorldGen.KillTile(
                    x,
                    y,
                    noItem: true
                );
            }
        }
    }
}