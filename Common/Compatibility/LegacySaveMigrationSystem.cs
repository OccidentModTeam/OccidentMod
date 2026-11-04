// 在原版讀檔入口掛接名稱相容轉換，卸載時完整移除掛鉤。
using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Occident.Common.Compatibility;

public sealed class LegacySaveMigrationSystem : ModSystem
{
    private Hook playerHook, worldHook, headerHook;
    private delegate void PlayerLoad(Player player, IList<TagCompound> records);
    private delegate void WorldLoad(IList<TagCompound> records);
    private delegate void HeaderLoad(WorldFileData world, TagCompound header);

    public override void Load()
    {
        try
        {
            LegacySaveIdentity.ValidateRuntimeMembers();
            playerHook = new Hook(Find("PlayerIO", "LoadModData"),
                (Action<PlayerLoad, Player, IList<TagCompound>>)LoadPlayer);
            worldHook = new Hook(Find("WorldIO", "LoadModData"),
                (Action<WorldLoad, IList<TagCompound>>)LoadWorld);
            headerHook = new Hook(Find("WorldIO", "LoadWorldHeader"),
                (Action<HeaderLoad, WorldFileData, TagCompound>)LoadHeader);
        }
        catch
        {
            // 後續掛鉤可能安裝失敗；先卸除已裝好的掛鉤，再將錯誤交回載入器。
            Unload();
            throw;
        }
    }

    public override void Unload()
    {
        headerHook?.Dispose();
        worldHook?.Dispose();
        playerHook?.Dispose();
        headerHook = worldHook = playerHook = null;
    }

    private static MethodInfo Find(string type, string method) => typeof(Mod).Assembly
        .GetType("Terraria.ModLoader.IO." + type)?.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(type, method);

    private void LoadPlayer(PlayerLoad orig, Player player, IList<TagCompound> records) =>
        orig(player, LegacySaveIdentity.Remap(records, Mod.Name));

    private void LoadWorld(WorldLoad orig, IList<TagCompound> records) =>
        orig(LegacySaveIdentity.Remap(records, Mod.Name));

    private void LoadHeader(HeaderLoad orig, WorldFileData world, TagCompound header)
    {
        orig(world, header);
        LegacySaveIdentity.RemapHeader(world, Mod.Name);
    }
}

