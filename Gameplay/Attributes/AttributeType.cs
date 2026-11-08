namespace OccidentMod.Gameplay.Attributes;

/// <summary>
///     定义角色的八种基础属性。
///     Defines the eight core character attributes.
/// </summary>
/// <remarks>
///     仅负责识别属性类型，不处理属性数值与计算。
///     Only identifies attribute types without handling values or calculations.
/// </remarks>
public enum AttributeType
{
    Vigor = 0, // 生命力 / Vigor
    Mind = 1, // 精神 / Mind
    Endurance = 2, // 耐力 / Endurance
    Strength = 3, // 力量 / Strength
    Dexterity = 4, // 敏捷 / Dexterity
    Intelligence = 5, // 智力 / Intelligence
    Faith = 6, // 信仰 / Faith
    Arcane = 7 // 奥术 / Arcane
}