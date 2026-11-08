#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using OccidentMod.Content.Tiles.SporeDream;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森區域、地形、環境及舊世界修復管理。
///     Spore Dream Forest region, terrain, environment
///     and legacy world migration.
/// </summary>
public sealed class SporeDreamEnvironmentSystem : ModSystem
{
    public const int UndergroundDepth = 35;

    private const int CurrentSoilVersion = 2;
    private const int CurrentVegetationVersion = 2;
    private bool giantFoundationsUpdated;

    private int soilVersion;
    private int vegetationVersion;

    public static SporeDreamEnvironmentSystem Instance =>
        ModContent.GetInstance<SporeDreamEnvironmentSystem>();

    public int Left { get; private set; } = -1;

    public int[] Surface { get; private set; } =
        Array.Empty<int>();

    public int Right => Left + Surface.Length;

    public bool Exists =>
        Left >= 0 && Surface.Length > 0;

    public bool Ready { get; private set; }

    /// <summary>
    ///     清除世界與遷移狀態。
    ///     Reset forest and migration state.
    /// </summary>
    public override void ClearWorld()
    {
        Left = -1;
        Surface = Array.Empty<int>();
        Ready = false;

        soilVersion = 0;
        vegetationVersion = 0;
        giantFoundationsUpdated = false;
    }

    /// <summary>
    ///     設定森林生成區域。
    ///     Register the forest generation region.
    /// </summary>
    internal void SetRegion(int left, int[] surface)
    {
        if (surface is null ||
            surface.Length == 0 ||
            left < 1 ||
            left + surface.Length >= Main.maxTilesX)
        {
            ClearWorld();
            return;
        }

        foreach (var y in surface)
            if (y < 1 ||
                y + UndergroundDepth >= Main.maxTilesY)
            {
                ClearWorld();
                return;
            }

        Left = left;
        Surface = (int[])surface.Clone();

        Ready = false;
    }

    /// <summary>
    ///     標記新森林生成完成。
    ///     Mark a newly generated forest as completed.
    /// </summary>
    internal void MarkGenerated()
    {
        if (!Exists)
            return;

        Ready = true;

        // 新生成的世界不需要舊版地形修復。
        // Newly generated worlds need no legacy repair.
        soilVersion = CurrentSoilVersion;
        vegetationVersion = CurrentVegetationVersion;
        giantFoundationsUpdated = true;
    }

    public bool Contains(int x, int y)
    {
        if (!Ready || !Exists)
            return false;

        return x >= Left &&
               x < Right &&
               y >= 0 &&
               y <= Surface[x - Left] + UndergroundDepth;
    }

    /// <summary>
    ///     判斷座標是否位於預定森林地表範圍。
    ///     Check candidate forest surface bounds.
    /// </summary>
    internal bool IsGenerationSurface(int x, int y)
    {
        return Exists &&
               x >= Left &&
               x < Right &&
               y >= 0 &&
               y <= Surface[x - Left];
    }

    private static bool NaturalSoil(ushort type)
    {
        return type is
            TileID.Grass or
            TileID.Dirt or
            TileID.Stone or
            TileID.ClayBlock;
    }

    /// <summary>
    ///     生成平滑的森林地表起伏。
    ///     Apply smooth forest terrain variations.
    /// </summary>
    internal void VaryTerrain()
    {
        if (!Exists)
            return;

        var offsets =
            SporeDreamTerrainProfile.CreateOffsets(
                Surface.Length,
                WorldGen.genRand.Next
            );

        // 先檢查原有地形是否允許修改。
        // Validate original terrain before modifying it.
        for (var k = 0; k < Surface.Length; k++)
        {
            var x = Left + k;
            var oldGround = Surface[k];
            var newGround = oldGround + offsets[k];

            var safe = true;

            for (var y = Math.Min(oldGround, newGround);
                 y <= Math.Max(oldGround, newGround) + 1;
                 y++)
            {
                if (!WorldGen.InWorld(x, y, 2))
                {
                    safe = false;
                    break;
                }

                var tile = Main.tile[x, y];

                if (tile.LiquidAmount > 0 ||
                    Main.wallHouse[tile.WallType])
                {
                    safe = false;
                    break;
                }

                if (y >= oldGround &&
                    (!tile.HasTile ||
                     !NaturalSoil(tile.TileType)))
                {
                    safe = false;
                    break;
                }

                if (y < oldGround &&
                    tile.HasTile &&
                    tile.TileType is not (
                        TileID.Trees or
                        TileID.Plants or
                        TileID.Plants2 or
                        TileID.Sunflower))
                {
                    safe = false;
                    break;
                }
            }

            if (!safe)
                offsets[k] = 0;
        }

        // 在受保護地形附近逐漸收斂。
        // Smooth offsets near protected terrain.
        for (var k = 1; k < offsets.Length; k++)
            offsets[k] = Math.Clamp(
                offsets[k],
                Math.Min(0, offsets[k - 1] - 1),
                Math.Max(0, offsets[k - 1] + 1)
            );

        for (var k = offsets.Length - 2; k >= 0; k--)
            offsets[k] = Math.Clamp(
                offsets[k],
                Math.Min(0, offsets[k + 1] - 1),
                Math.Max(0, offsets[k + 1] + 1)
            );

        // 正式修改地表並更新高度。
        // Apply changes and update surface heights.
        for (var k = 0; k < Surface.Length; k++)
        {
            var x = Left + k;
            var oldGround = Surface[k];
            var newGround = oldGround + offsets[k];

            for (var y = newGround; y < oldGround; y++)
            {
                var tile = Main.tile[x, y];

                tile.ClearTile();
                tile.HasTile = true;
                tile.TileType = TileID.Dirt;
                tile.IsHalfBlock = false;
                tile.Slope = SlopeType.Solid;
            }

            for (var y = oldGround; y < newGround; y++)
                Main.tile[x, y].ClearTile();

            Surface[k] = newGround;
        }
    }

    /// <summary>
    ///     將天然土壤轉換成孢夢之森土壤。
    ///     Convert natural soil into forest soil.
    /// </summary>
    internal void ConvertSoil()
    {
        if (!Exists)
            return;

        var grass =
            ModContent.TileType<SporeDreamGrass>();

        for (var k = 0; k < Surface.Length; k++)
        {
            var x = Left + k;
            var ground = Surface[k];

            for (var y = Math.Max(1, ground - 45);
                 y < ground;
                 y++)
            {
                var tile = Main.tile[x, y];

                if (tile.HasTile &&
                    tile.TileType is TileID.Plants or
                        TileID.Plants2 or
                        TileID.Sunflower)
                    tile.ClearTile();
            }

            for (var y = ground;
                 y <= Math.Min(
                     Main.maxTilesY - 2,
                     ground + UndergroundDepth);
                 y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile)
                    continue;

                if (tile.TileType == TileID.Grass ||
                    (tile.TileType == TileID.Dirt &&
                     (y == ground ||
                      !Main.tile[x, y - 1].HasTile)))
                {
                    tile.TileType = (ushort)grass;
                    tile.TileFrameX = 0;
                    tile.TileFrameY = 0;
                }
                else if (tile.TileType == TileID.Dirt)
                {
                    tile.TileType = TileID.Mud;
                    tile.TileFrameX = 0;
                    tile.TileFrameY = 0;
                }
            }
        }

        RepairBuriedSoilShapes();

        for (var k = 0; k < Surface.Length; k++)
        {
            var x = Left + k;

            for (var y = Surface[k] - 1;
                 y <= Surface[k] + UndergroundDepth;
                 y++)
                if (WorldGen.InWorld(x, y, 1))
                    WorldGen.SquareTileFrame(x, y);
        }
    }

    /// <summary>
    ///     修復埋在土中的殘留半磚和斜坡。
    ///     Repair buried soil slopes and half blocks.
    /// </summary>
    private void RepairBuriedSoilShapes()
    {
        var grass =
            ModContent.TileType<SporeDreamGrass>();

        for (var x = Left; x < Right; x++)
        for (var y = Surface[x - Left];
             y <= Math.Min(
                 Main.maxTilesY - 2,
                 Surface[x - Left] + UndergroundDepth);
             y++)
        {
            var tile = Main.tile[x, y];

            if (!tile.HasTile ||
                (tile.TileType != grass &&
                 tile.TileType != TileID.Mud &&
                 tile.TileType != TileID.Dirt))
                continue;

            if (!tile.IsHalfBlock &&
                tile.Slope == SlopeType.Solid)
                continue;

            if (Main.wallHouse[tile.WallType])
                continue;

            if (!WorldGen.SolidTile(x - 1, y) ||
                !WorldGen.SolidTile(x + 1, y) ||
                !WorldGen.SolidTile(x, y - 1) ||
                !WorldGen.SolidTile(x, y + 1))
                continue;

            tile.IsHalfBlock = false;
            tile.Slope = SlopeType.Solid;
        }
    }

    /// <summary>
    ///     檢查或建立巨型蘑菇的自然地基。
    ///     Validate or construct a giant mushroom foundation.
    /// </summary>
    public bool PrepareGiantFoundation(
        int x,
        int ground,
        bool apply,
        int width = 1)
    {
        if (!Exists ||
            width is < 1 or > 2 ||
            x - 3 < Left ||
            x + width + 2 >= Right)
            return false;

        var grass =
            ModContent.TileType<SporeDreamGrass>();

        var plants =
            ModContent.TileType<SporeDreamPlants>();

        var tops = new int[width + 6];

        if (!SporeDreamGiantLayout.TryFoundationProfile(
                Surface.AsSpan(x - 3 - Left, width + 6),
                ground,
                tops,
                width))
            return false;

        // 先完整檢查，不留下部分修改。
        // Validate first to avoid partial modifications.
        for (var dx = -3; dx <= width + 2; dx++)
        {
            var column = x + dx;
            var old = Surface[column - Left];
            var target = tops[dx + 3];

            for (var y = target;
                 y <= Math.Max(old + 1, target + 1);
                 y++)
            {
                if (!WorldGen.InWorld(column, y, 2))
                    return false;

                var tile = Main.tile[column, y];

                if (tile.LiquidAmount > 0 ||
                    Main.wallHouse[tile.WallType])
                    return false;

                if (y >= old &&
                    (!tile.HasTile ||
                     (tile.TileType != grass &&
                      tile.TileType != TileID.Mud &&
                      tile.TileType != TileID.Dirt)))
                    return false;

                if (y < old &&
                    tile.HasTile &&
                    tile.TileType != plants)
                    return false;
            }
        }

        if (!apply)
            return true;

        for (var dx = -3; dx <= width + 2; dx++)
        {
            var column = x + dx;
            var old = Surface[column - Left];
            var target = tops[dx + 3];

            for (var y = target;
                 y <= Math.Max(old + 1, target + 1);
                 y++)
            {
                var tile = Main.tile[column, y];

                tile.ClearTile();
                tile.HasTile = true;

                tile.TileType = (ushort)(
                    y == target ? grass : TileID.Mud
                );

                tile.IsHalfBlock = false;
                tile.Slope = SlopeType.Solid;
            }

            Surface[column - Left] = target;
        }

        for (var dx = -4; dx <= width + 3; dx++)
        for (var y = ground - 4;
             y <= ground + 6;
             y++)
            if (WorldGen.InWorld(x + dx, y, 1))
                WorldGen.SquareTileFrame(
                    x + dx,
                    y,
                    false
                );

        return true;
    }

    /// <summary>
    ///     依舊版候選機率生成巨型蘑菇。
    ///     Populate giant mushrooms using the original chance.
    /// </summary>
    internal int PopulateGiants()
    {
        if (!Exists ||
            Main.netMode == NetmodeID.MultiplayerClient)
            return 0;

        var curvedBudget = Math.Max(
            1,
            (int)(Surface.Length / 32f * 0.25f)
        );

        var tallPair = WorldGen.genRand.NextBool(2);

        var tallCount = tallPair
            ? SporeDreamCurvedSystem.Instance.TryTallPair()
            : 0;

        var curvedCount =
            SporeDreamCurvedSystem.Instance.PopulateSides(
                curvedBudget
            );

        var count = tallCount + curvedCount;

        List<int> candidates = new();

        for (var x = Left + 5; x < Right - 5; x++)
            candidates.Add(x);

        for (var k = candidates.Count - 1; k > 0; k--)
        {
            var other = WorldGen.genRand.Next(k + 1);

            (candidates[k], candidates[other]) =
                (candidates[other], candidates[k]);
        }

        foreach (var x in candidates)
        {
            if (WorldGen.genRand.Next(100) >=
                SporeDreamGenerationScale.GiantCandidatePercent)
                continue;

            var y = Surface[x - Left];

            if (!SporeDreamCurvedSystem.Instance.TryFloor(x, y))
                continue;

            curvedCount++;
            count++;
        }

        Mod.Logger.Info(
            $"Spore Dream Forest giants: {count}, " +
            $"curved: {curvedCount}, tall: {tallCount}"
        );

        return count;
    }

    /// <summary>
    ///     在巨菇生成後放置地被植物。
    ///     Place vegetation after mushroom generation.
    /// </summary>
    internal void PopulateVegetation()
    {
        if (!Exists)
            return;

        var fruit = ModContent.TileType<PandoraPlant>();
        var plants = ModContent.TileType<SporeDreamPlants>();

        for (var x = Left + 4; x < Right - 4; x++)
        {
            var y = Surface[x - Left];

            if (WorldGen.genRand.NextBool(110) &&
                SporeDreamVegetationRules.CanPlaceFruit(x, y))
                WorldGen.PlaceObject(
                    x,
                    y - 1,
                    fruit,
                    true,
                    WorldGen.genRand.Next(3)
                );
            else if (WorldGen.genRand.NextBool(3))
                WorldGen.PlaceObject(
                    x,
                    y - 1,
                    plants,
                    true,
                    WorldGen.genRand.Next(
                        SporeDreamPlants.StyleCount
                    )
                );
        }
    }

    /// <summary>
    ///     舊世界載入後執行一次性修復。
    ///     Perform versioned repairs after loading an old world.
    /// </summary>
    public override void PostWorldLoad()
    {
        if (!Exists || !Ready ||
            Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (soilVersion < CurrentSoilVersion)
        {
            var converted = RepairLegacySoil();

            RepairBuriedSoilShapes();
            RefreshSoilFrames();

            soilVersion = CurrentSoilVersion;

            Mod.Logger.Info(
                "孢夢之森舊世界土壤修復完成：" +
                $"轉換 {converted} 格土壤。"
            );
        }

        if (vegetationVersion < CurrentVegetationVersion)
        {
            // 舊世界僅更新既有植被周圍的地塊框架。
            // Do not regenerate giants in an existing world.
            RefreshVegetationFrames();

            vegetationVersion =
                CurrentVegetationVersion;

            Mod.Logger.Info(
                "孢夢之森舊世界植被框架修復完成。"
            );
        }

        if (!giantFoundationsUpdated)
        {
            var repaired =
                RepairLegacyGiantFoundations();

            giantFoundationsUpdated = true;

            Mod.Logger.Info(
                "孢夢之森巨菇基座檢查完成：" +
                $"修復 {repaired} 處。"
            );
        }
    }

    /// <summary>
    ///     僅轉換舊世界中的天然裸露泥土。
    ///     Repair natural legacy dirt without touching buildings.
    /// </summary>
    private int RepairLegacySoil()
    {
        var converted = 0;

        for (var x = Left; x < Right; x++)
        {
            var ground = Surface[x - Left];

            for (var y = Math.Max(1, ground);
                 y <= Math.Min(
                     Main.maxTilesY - 2,
                     ground + UndergroundDepth);
                 y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile ||
                    tile.TileType != TileID.Dirt ||
                    Main.wallHouse[tile.WallType])
                    continue;

                tile.TileType = TileID.Mud;
                tile.TileFrameX = 0;
                tile.TileFrameY = 0;

                converted++;
            }
        }

        return converted;
    }

    /// <summary>
    ///     刷新舊世界土壤地塊框架。
    ///     Refresh legacy soil tile frames.
    /// </summary>
    private void RefreshSoilFrames()
    {
        var grass =
            ModContent.TileType<SporeDreamGrass>();

        for (var x = Math.Max(1, Left - 1);
             x <= Math.Min(Main.maxTilesX - 2, Right);
             x++)
        {
            var index = Math.Clamp(
                x - Left,
                0,
                Surface.Length - 1);

            var ground = Surface[index];

            for (var y = Math.Max(1, ground - 1);
                 y <= Math.Min(
                     Main.maxTilesY - 2,
                     ground + UndergroundDepth + 1);
                 y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile)
                    continue;

                if (tile.TileType == TileID.Dirt ||
                    tile.TileType == TileID.Mud ||
                    tile.TileType == TileID.Grass ||
                    tile.TileType == grass)
                    WorldGen.SquareTileFrame(
                        x,
                        y,
                        false);
            }
        }
    }

    /// <summary>
    ///     刷新舊世界既有植被附近的地塊框架。
    ///     Refresh frames surrounding existing vegetation.
    /// </summary>
    private void RefreshVegetationFrames()
    {
        var plants =
            ModContent.TileType<SporeDreamPlants>();

        var fruit =
            ModContent.TileType<PandoraPlant>();

        for (var x = Left; x < Right; x++)
        {
            var ground = Surface[x - Left];

            for (var y = Math.Max(1, ground - 8);
                 y <= Math.Min(
                     Main.maxTilesY - 2,
                     ground + 2);
                 y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile)
                    continue;

                if (tile.TileType == plants ||
                    tile.TileType == fruit)
                    WorldGen.SquareTileFrame(
                        x,
                        y,
                        false);
            }
        }
    }

    /// <summary>
    ///     修復舊版雙欄巨菇的自然地基。
    ///     Repair legacy paired giant mushroom foundations.
    /// </summary>
    private int RepairLegacyGiantFoundations()
    {
        var repaired = 0;

        var giant =
            ModContent.TileType<SporeDreamGiant>();

        for (var x = Left + 3; x < Right - 4; x++)
        {
            var ground = Surface[x - Left];

            for (var y = Math.Max(1, ground - 40);
                 y <= ground;
                 y++)
            {
                var tile = Main.tile[x, y];

                if (!tile.HasTile ||
                    tile.TileType != giant)
                    continue;

                if (!SporeDreamGiant.IsPairedTile(x, y) ||
                    !SporeDreamGiant.IsPrimaryTile(x, y))
                    continue;

                SporeDreamGiant.GetStemBounds(
                    x,
                    y,
                    out _,
                    out var bottom,
                    out _);

                if (PrepareGiantFoundation(
                        x,
                        bottom,
                        true,
                        2))
                    repaired++;

                break;
            }
        }

        return repaired;
    }

    /// <summary>
    ///     儲存森林範圍與修復版本。
    ///     Save forest data and migration versions.
    /// </summary>
    public override void SaveWorldData(TagCompound tag)
    {
        if (!Exists)
            return;

        tag["SporeDreamLeft"] = Left;
        tag["SporeDreamSurface"] = Surface;
        tag["SporeDreamReady"] = Ready;

        tag["SporeDreamSoilVersion"] =
            soilVersion;

        tag["SporeDreamVegetationVersion"] =
            vegetationVersion;

        tag["SporeDreamGiantFoundationsUpdated"] =
            giantFoundationsUpdated;
    }

    /// <summary>
    ///     讀取新舊版本的森林資料。
    ///     Load current and legacy forest world data.
    /// </summary>
    public override void LoadWorldData(TagCompound tag)
    {
        ClearWorld();

        if (!tag.ContainsKey("SporeDreamLeft") ||
            !tag.ContainsKey("SporeDreamSurface"))
            return;

        var left =
            tag.GetInt("SporeDreamLeft");

        var surface =
            tag.GetIntArray("SporeDreamSurface");

        SetRegion(left, surface);

        if (!Exists)
            return;

        // 舊版沒有 Ready 欄位，但森林已經生成完成。
        // Legacy worlds do not contain the Ready field.
        Ready = !tag.ContainsKey("SporeDreamReady") ||
                tag.GetBool("SporeDreamReady");

        soilVersion =
            tag.GetInt("SporeDreamSoilVersion");

        vegetationVersion =
            tag.GetInt("SporeDreamVegetationVersion");

        giantFoundationsUpdated =
            tag.GetBool(
                "SporeDreamGiantFoundationsUpdated");
    }

    /// <summary>
    ///     將森林範圍同步給多人客戶端。
    ///     Synchronize forest data to multiplayer clients.
    /// </summary>
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Left);
        writer.Write(Surface.Length);

        foreach (var y in Surface)
            writer.Write(y);

        writer.Write(Ready);
    }

    /// <summary>
    ///     接收多人森林範圍資料。
    ///     Receive multiplayer forest data.
    /// </summary>
    public override void NetReceive(BinaryReader reader)
    {
        var left = reader.ReadInt32();
        var length = reader.ReadInt32();

        if (length < 0 || length > Main.maxTilesX)
            throw new InvalidDataException(
                "Invalid Spore Dream Forest surface length."
            );

        var surface = new int[length];

        for (var i = 0; i < length; i++)
            surface[i] = reader.ReadInt32();

        var ready = reader.ReadBoolean();

        SetRegion(left, surface);

        if (ready && Exists)
            Ready = true;
    }
}