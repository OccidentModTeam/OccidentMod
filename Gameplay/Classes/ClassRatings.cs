namespace OccidentMod.Gameplay.Classes;

/// <summary>
///     定義職業雷達圖的五項展示評分。
///     Defines the five display ratings used by the class radar chart.
/// </summary>
/// <param name="Damage">輸出 / Damage output.</param>
/// <param name="Survival">生存 / Survivability.</param>
/// <param name="Mobility">機動 / Mobility.</param>
/// <param name="Control">控制 / Crowd control.</param>
/// <param name="Support">輔助 / Support capability.</param>
public readonly record struct ClassRating(
    float Damage,
    float Survival,
    float Mobility,
    float Control,
    float Support
);

/// <summary>
///     管理各職業的展示評分，不影響實際戰鬥數值。
///     Manages display ratings without affecting actual combat stats.
/// </summary>
public static class ClassRatings
{
    /// <summary>
    ///     雷達圖單項評分的最大值。
    ///     Maximum value for each radar chart rating.
    /// </summary>
    public const float MaxRating = 5f;

    /// <summary>
    ///     取得指定職業的五項展示評分。
    ///     Gets the five display ratings for the specified class.
    /// </summary>
    public static ClassRating Get(ClassType classId)
    {
        // 順序：輸出、生存、機動、控制、輔助。
        // Order: Damage, Survival, Mobility, Control, Support.

        return classId switch
        {
            // 近戰職業 / Melee classes

            ClassType.Blademaster => new ClassRating(5, 2, 3, 1, 1),
            ClassType.Goliath => new ClassRating(4, 5, 2, 2, 1),
            ClassType.Rogue => new ClassRating(2, 2, 5, 3, 1),
            ClassType.Knight => new ClassRating(5, 2, 2, 1, 3),

            // 魔法職業 / Magic classes

            ClassType.Priest => new ClassRating(5, 2, 2, 4, 2),
            ClassType.Apothecary => new ClassRating(3, 3, 2, 4, 5),
            ClassType.Sage => new ClassRating(3, 2, 5, 2, 1),
            ClassType.Warlock => new ClassRating(4, 2, 3, 3, 4),
            ClassType.Bard => new ClassRating(4, 3, 3, 3, 5),

            // 遠程職業 / Ranged classes

            ClassType.Skirmisher => new ClassRating(4, 2, 5, 3, 1),
            ClassType.Musketeer => new ClassRating(5, 2, 3, 4, 1),
            ClassType.Archer => new ClassRating(5, 2, 2, 3, 1),

            // 召喚職業 / Summoner classes

            ClassType.Cultist => new ClassRating(4, 2, 2, 4, 4),
            ClassType.Evoker => new ClassRating(3, 3, 3, 4, 5),
            ClassType.Druid => new ClassRating(4, 3, 5, 2, 2),

            // 未選擇或無效職業。
            // Unselected or invalid class.

            _ => new ClassRating(0, 0, 0, 0, 0)
        };
    }
}