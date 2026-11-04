// 建立角色介面的展示配點與操作難度資料，不直接修改實際能力。
using System;

namespace Occident.Common.Classes;

// null：尚未設定，UI 顯示 --。0：已設定為零，UI 顯示 0。
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
    public int? GetValue(string key) => key switch
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

public readonly record struct ClassPreviewInfo(
    ClassStartingStats Stats,
    int Difficulty);

// UI 的初始配點與難度總設定區；只控制展示，不會更改角色屬性。
public static class ClassPreviewData
{
    public static readonly string[] StatKeys =
    {
        "Vigor", "Mind", "Endurance", "Strength",
        "Dexterity", "Intelligence", "Faith", "Arcane"
    };

    // 各職業的展示配點可在此獨立調整，不限定總和，也不影響角色實際能力。
    // 難度：1 Easy、2 Normal、3 Hard、4 Expert、5 Master；0 尚未評級。
    public static ClassPreviewInfo Get(ClassType classId) => classId switch
    {
        // 劍豪
        ClassType.Blademaster => new(
            Stats: new(
                Vigor: 12,        // 生命力
                Mind: 8,         // 集中力
                Endurance: 12,    // 耐力
                Strength: 12,     // 力量
                Dexterity: 18,    // 靈巧
                Intelligence: 6, // 智力
                Faith: 6,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 2
        ),

        // 鐵衛
        ClassType.Goliath => new(
            Stats: new(
                Vigor: 16,        // 生命力
                Mind: 6,         // 集中力
                Endurance: 16,    // 耐力
                Strength: 18,     // 力量
                Dexterity: 8,    // 靈巧
                Intelligence: 5, // 智力
                Faith: 5,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 2
        ),

        // 盜賊
        ClassType.Rogue => new(
            Stats: new(
                Vigor: 9,        // 生命力
                Mind: 9,         // 集中力
                Endurance: 11,    // 耐力
                Strength: 8,     // 力量
                Dexterity: 18,    // 靈巧
                Intelligence: 6, // 智力
                Faith: 6,        // 信仰
                Arcane: 13        // 感應
            ),
            Difficulty: 3
        ),

        // 騎士
        ClassType.Knight => new(
            Stats: new(
                Vigor: 9,        // 生命力
                Mind: 12,         // 集中力
                Endurance: 6,    // 耐力
                Strength: 8,     // 力量
                Dexterity: 10,    // 靈巧
                Intelligence: 3, // 智力
                Faith: 3,        // 信仰
                Arcane: 3        // 感應
            ),
            Difficulty: 1
        ),

        // 祭司
        ClassType.Priest => new(
            Stats: new(
                Vigor: 8,        // 生命力
                Mind: 18,         // 集中力
                Endurance: 8,    // 耐力
                Strength: 6,     // 力量
                Dexterity: 8,    // 靈巧
                Intelligence: 20, // 智力
                Faith: 6,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 2
        ),

        // 賢者
        ClassType.Sage => new(
            Stats: new(
                Vigor: 10,        // 生命力
                Mind: 15,         // 集中力
                Endurance: 9,    // 耐力
                Strength: 6,     // 力量
                Dexterity: 7,    // 靈巧
                Intelligence: 12, // 智力
                Faith: 15,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 1
        ),

        // 藥師
        ClassType.Apothecary => new(
            Stats: new(
                Vigor: 5,        // 生命力
                Mind: 5,         // 集中力
                Endurance: 4,    // 耐力
                Strength: 5,     // 力量
                Dexterity: 10,    // 靈巧
                Intelligence: 15, // 智力
                Faith: 15,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 4
        ),

        // 咒術師
        ClassType.Warlock => new(
            Stats: new(
                Vigor: 12,        // 生命力
                Mind: 16,         // 集中力
                Endurance: 8,    // 耐力
                Strength: 5,     // 力量
                Dexterity: 6,    // 靈巧
                Intelligence: 18, // 智力
                Faith: 9,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 3
        ),

        // 鏢客
        ClassType.Skirmisher => new(
            Stats: new(
                Vigor: 10,        // 生命力
                Mind: 7,         // 集中力
                Endurance: 13,    // 耐力
                Strength: 9,     // 力量
                Dexterity: 18,    // 靈巧
                Intelligence: 5, // 智力
                Faith: 6,        // 信仰
                Arcane: 12        // 感應
            ),
            Difficulty: 2
        ),

        // 銃士
        ClassType.Musketeer  => new(
            Stats: new(
                Vigor: 12,        // 生命力
                Mind: 7,         // 集中力
                Endurance: 14,    // 耐力
                Strength: 12,     // 力量
                Dexterity: 17,    // 靈巧
                Intelligence: 6, // 智力
                Faith: 5,        // 信仰
                Arcane: 7        // 感應
            ),
            Difficulty: 2
        ),

        // 遊俠
        ClassType.Archer => new(
            Stats: new(
                Vigor: 9,        // 生命力
                Mind: 8,         // 集中力
                Endurance: 15,    // 耐力
                Strength: 10,     // 力量
                Dexterity: 20,    // 靈巧
                Intelligence: 6, // 智力
                Faith: 6,        // 信仰
                Arcane: 6        // 感應
            ),
            Difficulty: 1
        ),

        // 異教徒
        ClassType.Cultist => new(
            Stats: new(
                Vigor: 17,        // 生命力
                Mind: 12,         // 集中力
                Endurance: 8,    // 耐力
                Strength: 6,     // 力量
                Dexterity: 6,    // 靈巧
                Intelligence: 6, // 智力
                Faith: 12,        // 信仰
                Arcane: 13        // 感應
            ),
            Difficulty: 5
        ),

        // 喚靈術士
        ClassType.Evoker => new(
            Stats: new(
                Vigor: 10,        // 生命力
                Mind: 16,         // 集中力
                Endurance: 9,    // 耐力
                Strength: 5,     // 力量
                Dexterity: 6,    // 靈巧
                Intelligence: 12, // 智力
                Faith: 15,        // 信仰
                Arcane: 7        // 感應
            ),
            Difficulty: 2
        ),

        // 德魯伊
        ClassType.Druid => new(
            Stats: new(
                Vigor: 13,        // 生命力
                Mind: 8,         // 集中力
                Endurance: 17,    // 耐力
                Strength: 12,     // 力量
                Dexterity: 15,    // 靈巧
                Intelligence: 5, // 智力
                Faith: 5,        // 信仰
                Arcane: 5        // 感應
            ),
            Difficulty: 3
        ),

        _ => new(Stats: default, Difficulty: 0)
    };

    // 畫面顯示的難度名稱，由內部評分映射而來。
    public static string GetDifficultyName(ClassType classId) =>
        GetDifficulty(classId) switch
        {
            1 => "Easy",
            2 => "Normal",
            3 => "Hard",
            4 => "Expert",
            5 => "Master",
            _ => "Unrated"
        };
    // 保留既有 UI 可呼叫的方法。
    public static int GetDifficulty(ClassType classId) =>
        Math.Clamp(Get(classId).Difficulty, 0, 5);
}



