namespace Occident.Common.Classes;
public readonly record struct ClassRating(
    float Damage,    // 輸出
    float Survival,  // 生存
    float Mobility,  // 機動
    float Control,   // 控制
    float Support    // 輔助
);

public static class ClassRatings
{
    public const float MaxRating = 5f;

    public static ClassRating Get(ClassType classId)
    {
        // 所有評分皆為示範，可依職業設定調整。
        //                           輸出 生存 機動 控制 輔助
        return classId switch
        {
            // 近戰
            ClassType.Blademaster    => new(5, 2, 3, 1, 1),
            ClassType.Goliath      => new(4, 5, 2, 2, 1),
            ClassType.Rogue         => new(2, 2, 5, 3, 1),
            ClassType.Knight           => new(5, 2, 2, 1, 3),

            // 魔法
            ClassType.Priest         => new(5, 2, 2, 4, 2),
            ClassType.Apothecary     => new(3, 3, 2, 4, 5),
            ClassType.Sage        => new(3, 2, 5, 2, 1),
            ClassType.Warlock         => new(4, 2, 3, 3, 4),

            // 遠程
            ClassType.Skirmisher      => new(4, 2, 5, 3, 1),
            ClassType.Musketeer       => new(5, 2, 3, 4, 1),
            ClassType.Archer   => new(5, 2, 2, 3, 1),

            // 召喚
            ClassType.Cultist         => new(4, 2, 2, 4, 4),
            ClassType.Evoker          => new(3, 3, 3, 4, 5),
            ClassType.Druid        => new(4, 3, 5, 2, 2),

            // 尚未選擇，或不認得的職業。
            _ => new(0, 0, 0, 0, 0)
        };
    }
}