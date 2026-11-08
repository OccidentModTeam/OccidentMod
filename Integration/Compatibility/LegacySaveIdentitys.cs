#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.IO;
using Terraria.ModLoader.IO;

namespace OccidentMod.Integration.Compatibility;

/// <summary>
///     舊版《輓歌》存檔識別名稱相容工具。
///     Legacy Occident save identity compatibility helper.
/// </summary>
internal static class LegacySaveIdentitys
{
    private const BindingFlags Flags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    // 僅保留已確認的舊模組名稱。
    // Only include confirmed legacy mod names.
    private static readonly HashSet<string> PreviousNames =
        new(StringComparer.Ordinal)
        {
            "OccidentMod",
            "Occident"
        };

    private static readonly FieldInfo? UsedMods =
        typeof(WorldFileData).GetField(
            "usedMods",
            Flags);

    private static readonly FieldInfo? GeneratedVersions =
        typeof(WorldFileData).GetField(
            "modVersionsDuringWorldGen",
            Flags);

    private static readonly PropertyInfo? Headers =
        typeof(WorldFileData).GetProperty(
            "ModHeaders",
            Flags);

    /// <summary>
    ///     檢查 tModLoader 是否仍提供必要的內部成員。
    ///     Validate required tModLoader runtime members.
    /// </summary>
    internal static void ValidateRuntimeMembers()
    {
        if (UsedMods is null ||
            GeneratedVersions is null ||
            Headers?.GetGetMethod(true) is null)
            throw new MissingMemberException(
                "目前 tModLoader 的世界存檔內部結構" +
                "與舊存檔相容系統不一致。");
    }

    /// <summary>
    ///     判斷模組名稱是否屬於已確認的舊版名稱。
    ///     Check whether a mod name is a confirmed legacy name.
    /// </summary>
    private static bool IsLegacyName(
        string name,
        string currentName)
    {
        return !string.Equals(
                   name,
                   currentName,
                   StringComparison.Ordinal) &&
               PreviousNames.Contains(name);
    }

    /// <summary>
    ///     將舊版角色及世界紀錄映射至新版模組名稱。
    ///     Remap legacy player and world records.
    /// </summary>
    internal static IList<TagCompound> Remap(
        IList<TagCompound> records,
        string currentName)
    {
        if (records.Count == 0 ||
            string.IsNullOrWhiteSpace(currentName))
            return records;

        // 優先收集新版已存在的資料名稱。
        // Collect identities already owned by the new mod.
        HashSet<string> currentTypes =
            new(StringComparer.Ordinal);

        foreach (var entry in records)
        {
            if (!TryReadIdentity(
                    entry,
                    out var modName,
                    out var typeName))
                continue;

            if (string.Equals(
                    modName,
                    currentName,
                    StringComparison.Ordinal))
                currentTypes.Add(typeName);
        }

        List<TagCompound> result =
            new(records.Count);

        foreach (var entry in records)
        {
            if (!TryReadIdentity(
                    entry,
                    out var modName,
                    out var typeName))
            {
                result.Add(entry);
                continue;
            }

            if (!IsLegacyName(modName, currentName))
            {
                result.Add(entry);
                continue;
            }

            // 新版已存在同名資料時，不覆寫新版紀錄。
            // Prefer current records when identities collide.
            if (currentTypes.Contains(typeName)) continue;

            // 複製紀錄，不直接修改原始 TagCompound。
            // Clone the record before changing its identity.
            var migrated = CloneRecord(entry);

            migrated["mod"] = currentName;

            result.Add(migrated);
            currentTypes.Add(typeName);
        }

        return result;
    }

    /// <summary>
    ///     安全讀取一筆模組資料的識別名稱。
    ///     Safely read the mod identity of a save record.
    /// </summary>
    private static bool TryReadIdentity(
        TagCompound entry,
        out string modName,
        out string typeName)
    {
        modName = string.Empty;
        typeName = string.Empty;

        if (!entry.ContainsKey("mod") ||
            !entry.ContainsKey("name"))
            return false;

        try
        {
            modName = entry.GetString("mod");
            typeName = entry.GetString("name");

            return !string.IsNullOrWhiteSpace(modName) &&
                   !string.IsNullOrWhiteSpace(typeName);
        }
        catch
        {
            // 不修改格式不符的紀錄。
            // Preserve records with unexpected formats.
            return false;
        }
    }

    /// <summary>
    ///     複製 TagCompound 的所有資料欄位。
    ///     Create a shallow copy of a save record.
    /// </summary>
    private static TagCompound CloneRecord(
        TagCompound source)
    {
        TagCompound copy = new();

        foreach (KeyValuePair<string, object> pair in source) copy[pair.Key] = pair.Value;

        return copy;
    }

    /// <summary>
    ///     映射舊版世界標頭中的模組識別名稱。
    ///     Remap legacy mod identities in world headers.
    /// </summary>
    internal static void RemapHeader(
        WorldFileData world,
        string currentName)
    {
        if (string.IsNullOrWhiteSpace(currentName))
            return;

        ValidateRuntimeMembers();

        RemapUsedMods(world, currentName);

        RemapGeneratedVersions(world, currentName);

        RemapModHeaders(world, currentName);
    }

    /// <summary>
    ///     更新世界使用過的模組名稱集合。
    ///     Update the world's used-mod list.
    /// </summary>
    private static void RemapUsedMods(
        WorldFileData world,
        string currentName)
    {
        if (UsedMods?.GetValue(world) is not
            IList<string> mods)
            return;

        for (var i = 0; i < mods.Count; i++)
            if (IsLegacyName(mods[i], currentName))
                mods[i] = currentName;

        // 若原集合允許移除重複值，整理名稱。
        // Remove duplicates when the collection is writable.
        if (mods.IsReadOnly)
            return;

        HashSet<string> seen =
            new(StringComparer.Ordinal);

        for (var i = mods.Count - 1; i >= 0; i--)
            if (!seen.Add(mods[i]))
                mods.RemoveAt(i);
    }

    /// <summary>
    ///     更新世界生成時記錄的模組版本名稱。
    ///     Update mod-version identities recorded at worldgen.
    /// </summary>
    private static void RemapGeneratedVersions(
        WorldFileData world,
        string currentName)
    {
        if (GeneratedVersions?.GetValue(world) is not
            IDictionary<string, Version> versions)
            return;

        foreach (var previous in PreviousNames)
        {
            if (!IsLegacyName(previous, currentName) ||
                !versions.TryGetValue(
                    previous,
                    out var version))
                continue;

            if (!versions.ContainsKey(currentName)) versions[currentName] = version;

            versions.Remove(previous);
        }
    }

    /// <summary>
    ///     更新世界標頭內的模組資料鍵。
    ///     Update mod data keys stored in world headers.
    /// </summary>
    private static void RemapModHeaders(
        WorldFileData world,
        string currentName)
    {
        if (Headers?.GetValue(world) is not
            IDictionary<string, TagCompound> headers)
            return;

        foreach (var previous in PreviousNames)
        {
            if (!IsLegacyName(previous, currentName))
                continue;

            var oldKey =
                previous + "/WorldOptionsSystem";

            var newKey =
                currentName + "/WorldOptionsSystem";

            if (!headers.TryGetValue(
                    oldKey,
                    out var oldHeader))
                continue;

            // 不覆寫新版世界標頭。
            // Never overwrite an existing current header.
            if (!headers.ContainsKey(newKey)) headers[newKey] = oldHeader;

            headers.Remove(oldKey);
        }
    }
}