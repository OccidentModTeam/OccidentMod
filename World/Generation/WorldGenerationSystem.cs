#nullable enable

using System;
using System.Collections.Generic;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace OccidentMod.World.Generation;

/// <summary>
///     模組世界生成調度中心。
///     Central world-generation dispatcher.
/// </summary>
public sealed class WorldGenerationSystem : ModSystem
{
    public override void ModifyWorldGenTasks(
        List<GenPass> tasks,
        ref double totalWeight)
    {
        // 優先排在原版 Micro Biomes 之後。
        // Prefer generating after vanilla micro biomes.
        var index = tasks.FindIndex(pass => pass.Name == "Micro Biomes");

        var insertAfter = index >= 0;

        // 若原版生成階段名稱變動，改在最終清理之前執行。
        // Fall back to before Final Cleanup if necessary.
        if (index < 0)
            index = tasks.FindIndex(pass => pass.Name == "Final Cleanup");

        if (index < 0)
        {
            Mod.Logger.Warn(
                "孢夢之森：找不到可用的原版世界生成階段，" +
                "本次未註冊森林生成流程。");
            return;
        }

        tasks.Insert(
            index + (insertAfter ? 1 : 0),
            new PassLegacy(
                "Spore Dream Forest",
                GenerateSporeDreamForest));
    }

    /// <summary>
    ///     產生森林並快取地表欄位結果。
    ///     Generate the forest with cached terrain inspection.
    /// </summary>
    private void GenerateSporeDreamForest(
        GenerationProgress progress,
        GameConfiguration configuration)
    {
        progress.Message = "正在尋找孢夢之森的生成位置";

        var worldWidth = Main.maxTilesX;
        var worldHeight = Main.maxTilesY;

        if (worldWidth <= 0 || worldHeight <= 0)
            throw new InvalidOperationException(
                "孢夢之森生成失敗：世界尺寸尚未正確初始化。");

        // 在本次世界生成中，地形尚未受森林生成器修改，
        // 可以安全重用同一欄的搜尋結果。
        // Reuse inspected columns within this generation pass.
        Dictionary<int, WorldColumn> columns = new();

        WorldColumn InspectCached(int x)
        {
            if (!columns.TryGetValue(x, out var result))
            {
                result = InspectColumn(x);
                columns.Add(x, result);
            }

            return result;
        }

        var spawnX = Main.spawnTileX;

        if (spawnX <= 0 || spawnX >= worldWidth)
        {
            Mod.Logger.Warn(
                "孢夢之森：生成時出生點座標無效，" +
                "改以世界中央進行選址。");
            spawnX = worldWidth / 2;
        }

        GenerationContext context = new(
            worldWidth,
            worldHeight,
            spawnX,
            InspectCached);

        SporeDreamGenerator.Generate(context);
        progress.Message = "孢夢之森生成完成";
    }

    /// <summary>
    ///     檢查指定 X 座標的地表安全性。
    ///     Inspect the safety of a surface column.
    /// </summary>
    private static WorldColumn InspectColumn(int x)
    {
        if (x < 1 || x >= Main.maxTilesX - 1)
            return new WorldColumn(0, false, false, false);

        var startY = Math.Max(
            60,
            (int)Main.worldSurface - 110);

        var endY = Math.Min(
            Main.maxTilesY - 2,
            (int)Main.worldSurface + 150);

        if (endY <= startY)
            return new WorldColumn(0, false, false, false);

        var ground = -1;
        var grass = false;
        var evil = false;

        for (var y = startY; y <= endY; y++)
        {
            var tile = Main.tile[x, y];

            if (!tile.HasTile)
                continue;

            var type = tile.TileType;

            if (type is TileID.CorruptGrass or
                TileID.CrimsonGrass or
                TileID.Ebonstone or
                TileID.Crimstone or
                TileID.Ebonsand or
                TileID.Crimsand)
                evil = true;

            if (ground >= 0 ||
                !Main.tileSolid[type] ||
                Main.tileSolidTop[type])
                continue;

            ground = y;
            grass = type is TileID.Grass or
                TileID.GolfGrass or
                TileID.MushroomGrass;
        }

        if (ground < 0)
            return new WorldColumn(0, false, false, evil);

        // 測量地面上方連續的積水深度。
        // Measure the liquid depth immediately above ground.
        var waterDepth = 0;

        for (var y = ground - 1;
             y >= Math.Max(1, ground - 20);
             y--)
        {
            if (Main.tile[x, y].LiquidAmount == 0)
                break;

            waterDepth++;
        }

        var safe =
            !evil &&
            ground >= startY &&
            ground < endY &&
            waterDepth <= 8;

        return new WorldColumn(
            ground,
            safe,
            grass,
            evil,
            waterDepth);
    }
}