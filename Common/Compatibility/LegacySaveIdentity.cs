using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using Terraria.IO;
using Terraria.ModLoader.IO;

namespace Occident.Common.Compatibility;

// Keep old player/world system data readable after package renames.
internal static class LegacySaveIdentity
{
    internal const string PreviousName = "ArkheCrowns";
    private const string PreviousPackageName = "Occident";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo UsedMods = typeof(WorldFileData).GetField("usedMods", Flags);
    private static readonly FieldInfo GeneratedVersions = typeof(WorldFileData).GetField("modVersionsDuringWorldGen", Flags);
    private static readonly PropertyInfo Headers = typeof(WorldFileData).GetProperty("ModHeaders", Flags);

    // Check all required members before any load hook is installed. A changed tML
    // version must report a compatibility error, not fail midway through loading a save.
    internal static void ValidateRuntimeMembers()
    {
        if (UsedMods is null) throw new MissingFieldException(typeof(WorldFileData).FullName, "usedMods");
        if (GeneratedVersions is null) throw new MissingFieldException(typeof(WorldFileData).FullName, "modVersionsDuringWorldGen");
        if (Headers?.GetGetMethod(true) is null) throw new MissingMemberException(typeof(WorldFileData).FullName, "ModHeaders getter");
    }

    internal static IList<TagCompound> Remap(IList<TagCompound> records, string currentName)
    {
        // Current saves are the common case: no copied list and no nested searches.
        bool needsMigration = false;
        foreach (var entry in records)
            if (IsLegacyRecord(entry))
            {
                needsMigration = true;
                break;
            }
        if (!needsMigration)
            return records;

        var currentTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in records)
            if (entry.GetString("mod") == currentName)
                currentTypes.Add(entry.GetString("name"));
        var result = new List<TagCompound>(records.Count);
        foreach (var entry in records)
        {
            string type = entry.GetString("name");
            if (!IsLegacyRecord(entry))
            {
                result.Add(entry);
                continue;
            }
            // 新資料已存在時優先使用新資料，避免舊紀錄覆蓋目前的選擇。
            if (currentTypes.Contains(type))
                continue;
            var migrated = (TagCompound)entry.Clone();
            migrated["mod"] = currentName;
            result.Add(migrated);
        }
        return result;
    }

    private static bool IsLegacyRecord(TagCompound entry) =>
        entry.GetString("mod") == PreviousPackageName
        || (entry.GetString("mod") == PreviousName
            && entry.GetString("name") is "ClassPlayer" or "WorldOptionsSystem");

    internal static void RemapHeader(WorldFileData world, string currentName)
    {
        RemapHeaderName(world, PreviousPackageName, currentName);
        RemapHeaderName(world, PreviousName, currentName);
    }

    private static void RemapHeaderName(WorldFileData world, string previousName, string currentName)
    {
        if (UsedMods.GetValue(world) is IList<string> mods && mods.Contains(previousName))
            UsedMods.SetValue(world, mods.Select(name => name == previousName ? currentName : name).Distinct().ToList());
        string previousKey = previousName + "/WorldOptionsSystem", currentKey = currentName + "/WorldOptionsSystem";
        if (Headers.GetValue(world) is Dictionary<string, TagCompound> headers && headers.TryGetValue(previousKey, out var header))
        {
            if (!headers.ContainsKey(currentKey)) headers[currentKey] = header;
            headers.Remove(previousKey);
        }
        var versions = GeneratedVersions.GetValue(world) as Dictionary<string, Version>;
        if (versions is not null && versions.TryGetValue(previousName, out var version))
        {
            if (!versions.ContainsKey(currentName)) versions[currentName] = version;
            versions.Remove(previousName);
        }
    }
}
