using System;

namespace OccidentMod.Gameplay.Classes;

/// <summary>
///     集中管理職業分類、有效性檢查與舊存檔名稱對照。
///     Centralizes class categorization, validation,
///     and legacy save identifier mappings.
/// </summary>
public static class ClassCatalog
{
    /// <summary>
    ///     取得指定職業所屬的大分類。
    ///     Gets the main category of the specified class.
    /// </summary>
    public static ClassCategory GetCategory(ClassType classId)
    {
        return classId switch
        {
            // 近戰職業 / Melee classes

            ClassType.Blademaster
                or ClassType.Goliath
                or ClassType.Rogue
                or ClassType.Knight
                => ClassCategory.Melee,

            // 魔法職業 / Magic classes

            ClassType.Priest
                or ClassType.Apothecary
                or ClassType.Sage
                or ClassType.Warlock
                or ClassType.Bard
                => ClassCategory.Magic,

            // 遠程職業 / Ranged classes

            ClassType.Skirmisher
                or ClassType.Musketeer
                or ClassType.Archer
                => ClassCategory.Ranged,

            // 召喚職業 / Summoner classes

            ClassType.Cultist
                or ClassType.Evoker
                or ClassType.Druid
                => ClassCategory.Summon,

            _ => ClassCategory.None
        };
    }

    /// <summary>
    ///     解析職業識別字串，並相容舊版職業名稱。
    ///     Resolves a class identifier while supporting
    ///     legacy class names from older saves.
    /// </summary>
    public static bool TryResolveClassId(
        string classId,
        out ClassType id)
    {
        // 將舊職業名稱轉換成目前的正式名稱。
        // Convert legacy identifiers to canonical class names.

        var canonicalId = classId?.ToLowerInvariant() switch
        {
            "swordmaster" => nameof(ClassType.Blademaster),
            "titanborn" => nameof(ClassType.Goliath),
            "shadow" => nameof(ClassType.Rogue),
            "monk" => nameof(ClassType.Knight),

            "cleric" => nameof(ClassType.Priest),
            "exorcist" => nameof(ClassType.Priest),
            "scholar" => nameof(ClassType.Sage),
            "caster" => nameof(ClassType.Warlock),

            "night_owl" => nameof(ClassType.Skirmisher),
            "enforcer" => nameof(ClassType.Musketeer),
            "sharpshooter" => nameof(ClassType.Archer),

            "binder" => nameof(ClassType.Evoker),
            "outrider" => nameof(ClassType.Druid),

            _ => classId
        };

        return Enum.TryParse(canonicalId, true, out id)
               && IsValid(id);
    }

    /// <summary>
    ///     檢查指定職業是否為有效的可選職業。
    ///     Checks whether the specified class is valid and selectable.
    /// </summary>
    public static bool IsValid(ClassType classId)
    {
        return GetCategory(classId) != ClassCategory.None;
    }
}