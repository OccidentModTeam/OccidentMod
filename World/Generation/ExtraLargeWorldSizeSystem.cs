#nullable enable

using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.World.Generation;

/// <summary>
///     超大型世界尺寸與 Tilemap 初始化系統。
///     Extra-large world size and tilemap initialization.
/// </summary>
public sealed class ExtraLargeWorldSizeSystem : ModSystem
{
    public const int ExtraLargeWidth = 12000;
    public const int ExtraLargeHeight = 3600;

    // Tilemap 的尺寸建構式是 internal，
    // 必須透過反射取得。
    //
    // Tilemap's size constructor is internal
    // and must be accessed through reflection.
    private static ConstructorInfo? _tilemapConstructor;

    // 暫時停用尚未修復的超大型世界。
    // Disable unsupported extra-large worlds until fixed.
    public static bool FeatureEnabled => false;

    /// <summary>
    ///     載入時檢查 Tilemap 建構式並安裝掛鉤。
    ///     Resolve the internal constructor and install the hook.
    /// </summary>
    public override void Load()
    {
        if (!FeatureEnabled)
            return;

        _tilemapConstructor = typeof(Tilemap).GetConstructor(
            BindingFlags.Instance |
            BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(ushort),
                typeof(ushort)
            },
            null);

        if (_tilemapConstructor is null)
            Mod.Logger.Warn(
                "目前 tModLoader 沒有找到可用的 " +
                "Tilemap(ushort, ushort) 內部建構式。" +
                "超大型世界初始化將無法啟用。");

        On_WorldGen.clearWorld += ApplyExtraLargeSize;
    }

    /// <summary>
    ///     卸載時移除掛鉤與反射參照。
    ///     Remove the hook and cached constructor.
    /// </summary>
    public override void Unload()
    {
        if (FeatureEnabled)
            On_WorldGen.clearWorld -= ApplyExtraLargeSize;

        _tilemapConstructor = null;
    }

    /// <summary>
    ///     在世界生成開始前設定超大型尺寸。
    ///     Configure extra-large dimensions before world generation.
    /// </summary>
    private void ApplyExtraLargeSize(
        On_WorldGen.orig_clearWorld orig)
    {
        var requested =
            WorldOptionsSystem.PendingCreation
                is { ExtraLarge: true };

        // 普通世界完全交給 Terraria 原版處理。
        // Leave normal worlds unchanged.
        if (!requested)
        {
            orig();
            return;
        }

        // 若內部建構式不存在，不進入不安全的生成流程。
        // Fail before generation if the API is unavailable.
        if (_tilemapConstructor is null)
            throw new InvalidOperationException(
                "超大型世界初始化失敗：" +
                "目前 tModLoader 沒有支援的 Tilemap 建構式。");

        var worldData =
            Main.ActiveWorldFileData;

        if (worldData is null)
            throw new InvalidOperationException(
                "超大型世界初始化失敗：" +
                "找不到目前世界的檔案資料。");

        // 設定世界檔案的實際尺寸。
        // Configure the world file dimensions.
        worldData.SetWorldSize(
            ExtraLargeWidth,
            ExtraLargeHeight);

        // 在 clearWorld 前修改世界尺寸。
        // Set world dimensions before vanilla initialization.
        ApplyWorldDimensions();

        Mod.Logger.Info(
            "ExtraLarge world initialization requested: " +
            $"{ExtraLargeWidth}x{ExtraLargeHeight}");

        // 保留原版 clearWorld 的所有初始化程序。
        // Preserve all vanilla world initialization logic.
        orig();

        Mod.Logger.Info(
            "World dimensions after clearWorld: " +
            $"{Main.maxTilesX}x{Main.maxTilesY}");

        Mod.Logger.Info(
            "Tilemap dimensions after clearWorld: " +
            $"{Main.tile.Width}x{Main.tile.Height}");

        // 原版初始化可能重新設定世界尺寸。
        // Restore target dimensions if vanilla changed them.
        ApplyWorldDimensions();

        // 只有實際 Tilemap 尺寸不符時才重新配置。
        // Reallocate only when the tilemap has the wrong size.
        if (Main.tile.Width != ExtraLargeWidth ||
            Main.tile.Height != ExtraLargeHeight)
        {
            Mod.Logger.Info(
                "正在配置 12000x3600 超大型世界地塊資料。");

            AllocateExtraLargeTilemap();
        }

        // 世界尺寸與 Tilemap 必須完全一致。
        // World and tilemap dimensions must match exactly.
        ValidateDimensions();

        Mod.Logger.Info(
            "ExtraLarge tilemap validation passed.");

        Mod.Logger.Info(
            "超大型世界地塊初始化完成。");
    }

    /// <summary>
    ///     同步世界格數、像素邊界及網路區段數。
    ///     Synchronize dimensions, bounds and section counts.
    /// </summary>
    private static void ApplyWorldDimensions()
    {
        Main.maxTilesX = ExtraLargeWidth;
        Main.maxTilesY = ExtraLargeHeight;

        Main.rightWorld =
            ExtraLargeWidth * 16f;

        Main.bottomWorld =
            ExtraLargeHeight * 16f;

        // 網路世界區段必須覆蓋整張地圖。
        // Network sections must cover the full world.
        Main.maxSectionsX =
            (ExtraLargeWidth - 1) /
            Main.sectionWidth + 1;

        Main.maxSectionsY =
            (ExtraLargeHeight - 1) /
            Main.sectionHeight + 1;
    }

    /// <summary>
    ///     透過 tModLoader 內部建構式配置地塊資料。
    ///     Allocate tile data using the internal constructor.
    /// </summary>
    private static void AllocateExtraLargeTilemap()
    {
        var constructor =
            _tilemapConstructor
            ?? throw new InvalidOperationException(
                "Tilemap 內部建構式尚未初始化。");

        object? created;

        try
        {
            // 建構式會呼叫 TileData.SetLength，
            // 同步配置所有原版地塊資料。
            //
            // The constructor calls TileData.SetLength,
            // allocating the underlying tile data arrays.
            created = constructor.Invoke(
                new object[]
                {
                    (ushort)ExtraLargeWidth,
                    (ushort)ExtraLargeHeight
                });
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException(
                "超大型世界 Tilemap 配置失敗。",
                exception.InnerException ?? exception);
        }

        if (created is not Tilemap tilemap)
            throw new InvalidOperationException(
                "Tilemap 建構式沒有回傳有效的地塊資料。");

        // 只在新世界 clearWorld 之後替換空白 Tilemap。
        // Replace the blank tilemap after world clearing.
        Main.tile = tilemap;

        // 確保新世界沒有保留舊地塊內容。
        // Ensure the new map starts with cleared tile data.
        Main.tile.ClearEverything();
    }

    /// <summary>
    ///     在 TerrainPass 開始前驗證配置結果。
    ///     Validate tile allocation before TerrainPass.
    /// </summary>
    private static void ValidateDimensions()
    {
        if (Main.maxTilesX != ExtraLargeWidth ||
            Main.maxTilesY != ExtraLargeHeight)
            throw new InvalidOperationException(
                "超大型世界尺寸設定失敗：" +
                $"世界={Main.maxTilesX}x{Main.maxTilesY}，" +
                $"目標={ExtraLargeWidth}x{ExtraLargeHeight}。");

        if (Main.tile.Width != ExtraLargeWidth ||
            Main.tile.Height != ExtraLargeHeight)
            throw new InvalidOperationException(
                "超大型世界地塊尺寸錯誤：" +
                $"Tilemap={Main.tile.Width}x{Main.tile.Height}，" +
                $"目標={ExtraLargeWidth}x{ExtraLargeHeight}。");

        if (Main.maxSectionsX !=
            (ExtraLargeWidth - 1) /
            Main.sectionWidth + 1 ||
            Main.maxSectionsY !=
            (ExtraLargeHeight - 1) /
            Main.sectionHeight + 1)
            throw new InvalidOperationException(
                "超大型世界網路區段尺寸設定失敗。");
    }
}