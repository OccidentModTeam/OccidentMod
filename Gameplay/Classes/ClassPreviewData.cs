using System;

namespace OccidentMod.Gameplay.Classes;

/// <summary>
///     定義角色建立介面的八項展示配點。
///     Defines the eight preview attributes shown during character creation.
/// </summary>
/// <remarks>
///     null 表示尚未設定，UI 顯示 --；0 表示已設定為零。
///     Null means unset and displays --; zero is an explicitly assigned value.
/// </remarks>
public readonly record struct ClassStartingStats(
    int? Vigor,
    int? Mind,
    int? Endurance,
    int? Strength,
    int? Dexterity,
    int? Intelligence,
    int? Faith,
    int? Arcane)
{
    /// <summary>
    ///     根據屬性識別名稱取得展示數值。
    ///     Gets a preview attribute value by its identifier.
    /// </summary>
    public int? GetValue(string key)
    {
        return key switch
        {
            "Vigor" => Vigor,
            "Mind" => Mind,
            "Endurance" => Endurance,
            "Strength" => Strength,
            "Dexterity" => Dexterity,
            "Intelligence" => Intelligence,
            "Faith" => Faith,
            "Arcane" => Arcane,
            _ => null
        };
    }
}

/// <summary>
///     封裝職業展示配點與操作難度。
///     Encapsulates class preview attributes and difficulty.
/// </summary>
public readonly record struct ClassPreviewInfo(
    ClassStartingStats Stats,
    int Difficulty);

/// <summary>
///     管理職業建立介面的展示配點與操作難度。
///     Manages preview attributes and difficulty for character creation.
/// </summary>
/// <remarks>
///     此資料僅供 UI 展示，不直接修改玩家實際能力。
///     This data is for UI presentation only and does not modify player stats.
/// </remarks>
public static class ClassPreviewData
{
    /// <summary>
    ///     定義展示配點的固定欄位順序。
    ///     Defines the fixed display order of preview attributes.
    /// </summary>
    public static readonly string[] StatKeys =
    {
        "Vigor",
        "Mind",
        "Endurance",
        "Strength",
        "Dexterity",
        "Intelligence",
        "Faith",
        "Arcane"
    };

    /// <summary>
    ///     取得指定職業的展示配點與操作難度。
    ///     Gets the preview attributes and difficulty of a class.
    /// </summary>
    /// <remarks>
    ///     難度 1 至 5 分別為 Easy、Normal、Hard、Expert、Master。
    ///     難度 0 表示尚未評級。
    ///     Difficulty ranges from Easy (1) to Master (5).
    ///     Zero indicates an unrated class.
    /// </remarks>
    public static ClassPreviewInfo Get(ClassType classId)
    {
        return classId switch
        {
            // 近戰職業 / Melee classes

            // 劍豪 / Blademaster
            ClassType.Blademaster => new ClassPreviewInfo(
                new ClassStartingStats(
                    12,
                    8,
                    12,
                    12,
                    18,
                    6,
                    6,
                    6
                ),
                2
            ),

            // 鐵衛 / Goliath
            ClassType.Goliath => new ClassPreviewInfo(
                new ClassStartingStats(
                    16,
                    6,
                    16,
                    18,
                    8,
                    5,
                    5,
                    6
                ),
                2
            ),

            // 盜賊 / Rogue
            ClassType.Rogue => new ClassPreviewInfo(
                new ClassStartingStats(
                    9,
                    9,
                    11,
                    8,
                    18,
                    6,
                    6,
                    13
                ),
                3
            ),

            // 騎士 / Knight
            ClassType.Knight => new ClassPreviewInfo(
                new ClassStartingStats(
                    9,
                    12,
                    6,
                    8,
                    10,
                    3,
                    3,
                    3
                ),
                1
            ),

            // 魔法職業 / Magic classes

            // 祭司 / Priest
            ClassType.Priest => new ClassPreviewInfo(
                new ClassStartingStats(
                    8,
                    18,
                    8,
                    6,
                    8,
                    20,
                    6,
                    6
                ),
                2
            ),

            // 賢者 / Sage
            ClassType.Sage => new ClassPreviewInfo(
                new ClassStartingStats(
                    10,
                    15,
                    9,
                    6,
                    7,
                    12,
                    15,
                    6
                ),
                1
            ),

            // 藥師 / Apothecary
            ClassType.Apothecary => new ClassPreviewInfo(
                new ClassStartingStats(
                    5,
                    5,
                    4,
                    5,
                    10,
                    15,
                    15,
                    6
                ),
                4
            ),

            // 咒術師 / Warlock
            ClassType.Warlock => new ClassPreviewInfo(
                new ClassStartingStats(
                    12,
                    16,
                    8,
                    5,
                    6,
                    18,
                    9,
                    6
                ),
                3
            ),

            // 吟遊詩人 / Bard
            // 初始展示配點尚未設定。
            // Starting preview attributes are not configured yet.
            ClassType.Bard => new ClassPreviewInfo(
                default,
                0
            ),

            // 遠程職業 / Ranged classes

            // 鏢客 / Skirmisher
            ClassType.Skirmisher => new ClassPreviewInfo(
                new ClassStartingStats(
                    10,
                    7,
                    13,
                    9,
                    18,
                    5,
                    6,
                    12
                ),
                2
            ),

            // 銃士 / Musketeer
            ClassType.Musketeer => new ClassPreviewInfo(
                new ClassStartingStats(
                    12,
                    7,
                    14,
                    12,
                    17,
                    6,
                    5,
                    7
                ),
                2
            ),

            // 遊俠 / Archer
            ClassType.Archer => new ClassPreviewInfo(
                new ClassStartingStats(
                    9,
                    8,
                    15,
                    10,
                    20,
                    6,
                    6,
                    6
                ),
                1
            ),

            // 召喚職業 / Summoner classes

            // 異教徒 / Cultist
            ClassType.Cultist => new ClassPreviewInfo(
                new ClassStartingStats(
                    17,
                    12,
                    8,
                    6,
                    6,
                    6,
                    12,
                    13
                ),
                5
            ),

            // 喚靈術士 / Evoker
            ClassType.Evoker => new ClassPreviewInfo(
                new ClassStartingStats(
                    10,
                    16,
                    9,
                    5,
                    6,
                    12,
                    15,
                    7
                ),
                2
            ),

            // 德魯伊 / Druid
            ClassType.Druid => new ClassPreviewInfo(
                new ClassStartingStats(
                    13,
                    8,
                    17,
                    12,
                    15,
                    5,
                    5,
                    5
                ),
                3
            ),

            // 未選擇或無效職業。
            // Unselected or invalid class.
            _ => new ClassPreviewInfo(
                default,
                0
            )
        };
    }

    /// <summary>
    ///     取得指定職業的操作難度名稱。
    ///     Gets the difficulty label for the specified class.
    /// </summary>
    public static string GetDifficultyName(ClassType classId)
    {
        return GetDifficulty(classId) switch
        {
            1 => "Easy",
            2 => "Normal",
            3 => "Hard",
            4 => "Expert",
            5 => "Master",
            _ => "Unrated"
        };
    }

    /// <summary>
    ///     取得指定職業的操作難度數值，限制於 0 至 5。
    ///     Gets the class difficulty value, clamped between 0 and 5.
    /// </summary>
    public static int GetDifficulty(ClassType classId)
    {
        return Math.Clamp(Get(classId).Difficulty, 0, 5);
    }
}