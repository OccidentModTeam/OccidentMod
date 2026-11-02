using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using Terraria.IO;
using Terraria.ModLoader.IO;

namespace Occident.Common.Compatibility;

// 舊識別碼僅供存檔搬遷；所有新存檔、素材與實際模組名稱均使用 Occident。
internal static class LegacySaveIdentity
{
    internal const string PreviousName = "ArkheCrowns";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo UsedMods = typeof(WorldFileData).GetField("usedMods", Flags);
    private static readonly FieldInfo GeneratedVersions = typeof(WorldFileData).GetField("modVersionsDuringWorldGen", Flags);
    private static readonly PropertyInfo Headers = typeof(WorldFileData).GetProperty("ModHeaders", Flags);

    internal static IList<TagCompound> Remap(IList<TagCompound> records, string currentName)
    {
        // Current saves are the common case: no copied list and no nested searches.
        bool needsMigration = false;
        foreach (var entry in records)
            if (entry.GetString("mod") == PreviousName
                && entry.GetString("name") is "ClassPlayer" or "WorldOptionsSystem")
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
            if (entry.GetString("mod") != PreviousName || type is not ("ClassPlayer" or "WorldOptionsSystem"))
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

    internal static void RemapHeader(WorldFileData world, string currentName)
    {
        if (UsedMods.GetValue(world) is IList<string> mods && mods.Contains(PreviousName))
            UsedMods.SetValue(world, mods.Select(name => name == PreviousName ? currentName : name).Distinct().ToList());
        string previousKey = PreviousName + "/WorldOptionsSystem", currentKey = currentName + "/WorldOptionsSystem";
        if (Headers.GetValue(world) is Dictionary<string, TagCompound> headers && headers.TryGetValue(previousKey, out var header))
        {
            if (!headers.ContainsKey(currentKey)) headers[currentKey] = header;
            headers.Remove(previousKey);
        }
        var versions = GeneratedVersions.GetValue(world) as Dictionary<string, Version>;
        if (versions is not null && versions.TryGetValue(PreviousName, out var version))
        {
            if (!versions.ContainsKey(currentName)) versions[currentName] = version;
            versions.Remove(PreviousName);
        }
    }
}
