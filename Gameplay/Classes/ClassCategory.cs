namespace OccidentMod.Gameplay.Classes;

/// <summary>
///     定義職業的大分類，供職業分頁與分類判斷使用。
///     Defines the main class categories used for
///     class selection tabs and category checks.
/// </summary>
public enum ClassCategory
{
    /// <summary>
    ///     無分類。
    ///     No category.
    /// </summary>
    None = 0,

    /// <summary>
    ///     近戰職業。
    ///     Melee classes.
    /// </summary>
    Melee = 1,

    /// <summary>
    ///     魔法職業。
    ///     Magic classes.
    /// </summary>
    Magic = 2,

    /// <summary>
    ///     遠程職業。
    ///     Ranged classes.
    /// </summary>
    Ranged = 3,

    /// <summary>
    ///     召喚職業。
    ///     Summoner classes.
    /// </summary>
    Summon = 4
}