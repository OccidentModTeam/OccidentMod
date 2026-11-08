#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Integration.Compatibility;

/// <summary>
///     舊版角色與世界存檔識別名稱相容系統。
///     Legacy player and world save identity migration.
/// </summary>
public sealed class LegacySaveMigrationSystem : ModSystem
{
    private const BindingFlags MethodFlags =
        BindingFlags.Static |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    private Hook? headerHook;

    private Hook? playerHook;
    private Hook? worldHook;

    /// <summary>
    ///     初始化相容系統並驗證掛鉤。
    ///     Initialize and validate compatibility hooks.
    /// </summary>
    public override void Load()
    {
        try
        {
            LegacySaveIdentitys.ValidateRuntimeMembers();

            var playerMethod = FindExact(
                "PlayerIO",
                "LoadModData",
                typeof(Player),
                typeof(IList<TagCompound>));

            var worldMethod = FindExact(
                "WorldIO",
                "LoadModData",
                typeof(IList<TagCompound>));

            var headerMethod = FindExact(
                "WorldIO",
                "LoadWorldHeader",
                typeof(WorldFileData),
                typeof(TagCompound));

            // 所有方法都符合預期後才開始安裝。
            // Verify every method before installing hooks.
            playerHook = new Hook(
                playerMethod,
                (Action<
                    PlayerLoad,
                    Player,
                    IList<TagCompound>>)LoadPlayer);

            worldHook = new Hook(
                worldMethod,
                (Action<
                    WorldLoad,
                    IList<TagCompound>>)LoadWorld);

            headerHook = new Hook(
                headerMethod,
                (Action<
                    HeaderLoad,
                    WorldFileData,
                    TagCompound>)LoadHeader);

            Mod.Logger.Info(
                "舊存檔識別名稱相容掛鉤已安裝。");
        }
        catch (Exception exception)
        {
            // 部分掛鉤安裝成功時也必須全部回復。
            // Roll back all partially installed hooks.
            DisposeHooks();

            Mod.Logger.Warn(
                "舊存檔相容功能未啟用。" +
                "目前的 tModLoader 方法簽章可能已變更。" +
                $"詳細原因：{exception}");
        }
    }

    /// <summary>
    ///     卸載時移除所有掛鉤。
    ///     Remove all hooks during unload.
    /// </summary>
    public override void Unload()
    {
        DisposeHooks();
    }

    /// <summary>
    ///     安全移除已安裝的掛鉤。
    ///     Dispose installed hooks safely.
    /// </summary>
    private void DisposeHooks()
    {
        headerHook?.Dispose();
        headerHook = null;

        worldHook?.Dispose();
        worldHook = null;

        playerHook?.Dispose();
        playerHook = null;
    }

    /// <summary>
    ///     依完整方法簽章尋找 tModLoader 內部方法。
    ///     Find an internal method by its exact signature.
    /// </summary>
    private static MethodInfo FindExact(
        string typeName,
        string methodName,
        params Type[] parameterTypes)
    {
        var type =
            typeof(Mod).Assembly.GetType(
                "Terraria.ModLoader.IO." + typeName)
            ?? throw new TypeLoadException(
                $"找不到內部型別：{typeName}");

        var method = type.GetMethod(
            methodName,
            MethodFlags,
            null,
            parameterTypes,
            null);

        if (method is null)
            throw new MissingMethodException(
                type.FullName,
                methodName);

        // 所有相容委派均預期目標方法回傳 void。
        // Every compatibility delegate expects void.
        if (method.ReturnType != typeof(void))
            throw new InvalidOperationException(
                $"方法 {typeName}.{methodName} " +
                "的回傳型別不符合預期。");

        return method;
    }

    /// <summary>
    ///     載入角色資料前轉換舊版模組識別名稱。
    ///     Remap legacy identities before loading player data.
    /// </summary>
    private void LoadPlayer(
        PlayerLoad original,
        Player player,
        IList<TagCompound> records)
    {
        IList<TagCompound> migrated;

        try
        {
            migrated = LegacySaveIdentitys.Remap(
                records,
                Mod.Name);
        }
        catch (Exception exception)
        {
            // 名稱轉換失敗時保留原始資料。
            // Fall back to original records on remap failure.
            Mod.Logger.Warn(
                "角色舊存檔名稱轉換失敗，" +
                $"改用原始資料：{exception}");

            migrated = records;
        }

        // 不攔截原始讀檔錯誤。
        // Preserve normal loader exception behavior.
        original(player, migrated);
    }

    /// <summary>
    ///     載入世界資料前轉換舊版模組識別名稱。
    ///     Remap legacy identities before loading world data.
    /// </summary>
    private void LoadWorld(
        WorldLoad original,
        IList<TagCompound> records)
    {
        IList<TagCompound> migrated;

        try
        {
            migrated = LegacySaveIdentitys.Remap(
                records,
                Mod.Name);
        }
        catch (Exception exception)
        {
            Mod.Logger.Warn(
                "世界舊存檔名稱轉換失敗，" +
                $"改用原始資料：{exception}");

            migrated = records;
        }

        original(migrated);
    }

    /// <summary>
    ///     讀取世界標頭後修正舊版模組識別。
    ///     Remap legacy identities after loading world headers.
    /// </summary>
    private void LoadHeader(
        HeaderLoad original,
        WorldFileData world,
        TagCompound header)
    {
        // 先執行 tModLoader 原本的標頭載入。
        // Always execute the original loader first.
        original(world, header);

        try
        {
            LegacySaveIdentitys.RemapHeader(
                world,
                Mod.Name);
        }
        catch (Exception exception)
        {
            // 標頭映射失敗不應阻止正常世界列表載入。
            // Header remapping must not block world listing.
            Mod.Logger.Warn(
                "舊世界標頭名稱轉換失敗：" +
                exception);
        }
    }

    // 委派簽章必須與目標方法完全一致。
    // Delegate signatures must match the target methods.
    private delegate void PlayerLoad(
        Player player,
        IList<TagCompound> records);

    private delegate void WorldLoad(
        IList<TagCompound> records);

    private delegate void HeaderLoad(
        WorldFileData world,
        TagCompound header);
}