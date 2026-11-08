namespace OccidentMod.Gameplay.Classes;

/// <summary>
///     定義所有職業的固定識別代號。
///     Defines fixed identifiers for all player classes.
/// </summary>
/// <remarks>
///     職業數值用於存檔與網路封包，修改時必須考慮相容性。
///     Class values are used in save data and network packets.
///     Changes must account for compatibility.
/// </remarks>
public enum ClassType
{
    /// <summary>未選擇職業 / No class selected.</summary>
    None = 0,

    // 近戰職業 / Melee classes

    /// <summary>劍豪 / Blademaster.</summary>
    Blademaster = 1,

    /// <summary>鐵衛 / Goliath.</summary>
    Goliath = 2,

    /// <summary>盜賊 / Rogue.</summary>
    Rogue = 3,

    /// <summary>騎士 / Knight.</summary>
    Knight = 4,

    // 魔法職業 / Magic classes

    /// <summary>祭司 / Priest.</summary>
    Priest = 5,

    /// <summary>藥師 / Apothecary.</summary>
    Apothecary = 6,

    /// <summary>賢者 / Sage.</summary>
    Sage = 7,

    /// <summary>咒術師 / Warlock.</summary>
    Warlock = 8,

    /// <summary>吟遊詩人 / Bard.</summary>
    Bard = 9,

    // 遠程職業 / Ranged classes

    /// <summary>鏢客 / Skirmisher.</summary>
    Skirmisher = 10,

    /// <summary>銃士 / Musketeer.</summary>
    Musketeer = 11,

    /// <summary>遊俠 / Archer.</summary>
    Archer = 12,

    // 召喚職業 / Summoner classes

    /// <summary>異教徒 / Cultist.</summary>
    Cultist = 13,

    /// <summary>喚靈術士 / Evoker.</summary>
    Evoker = 14,

    /// <summary>德魯伊 / Druid.</summary>
    Druid = 15
}