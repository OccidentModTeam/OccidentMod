namespace OccidentMod.Gameplay.Attributes;

/// <summary>
///     负责计算角色属性所产生的游戏效果。
///     Calculates gameplay effects derived from character attributes.
/// </summary>
/// <remarks>
///     所有计算方法目前只返回基础值。
///     具体加成公式将在属性平衡设计完成后加入。
///     All calculation methods currently return base values.
///     Scaling formulas will be added after attribute balancing.
/// </remarks>
public static class AttributeCalculator
{
    /// <summary>
    ///     计算角色的最大生命值。
    ///     Calculates maximum health.
    /// </summary>
    public static int CalculateMaxHealth(
        AttributeData attributes,
        int baseHealth)
    {
        // 生命力公式尚未确定。
        // Vigor scaling is not yet defined.
        return baseHealth;
    }

    /// <summary>
    ///     计算角色的移动速度倍率。
    ///     Calculates the movement speed multiplier.
    /// </summary>
    public static float CalculateMovementSpeed(
        AttributeData attributes)
    {
        // 敏捷影响移动速度。
        // Dexterity affects movement speed.
        return 1f;
    }

    /// <summary>
    ///     计算武器使用速度倍率。
    ///     Calculates the weapon use speed multiplier.
    /// </summary>
    /// <remarks>
    ///     仅适用于明确受到力量影响的武器。
    ///     Only applies to weapons explicitly affected by Strength.
    /// </remarks>
    public static float CalculateWeaponUseSpeed(
        AttributeData attributes,
        bool scalesWithStrength)
    {
        if (!scalesWithStrength)
            return 1f;

        // 力量影响部分武器的使用速度。
        // Strength affects the use speed of selected weapons.
        return 1f;
    }

    /// <summary>
    ///     计算最大耐力值。
    ///     Calculates maximum stamina.
    /// </summary>
    public static float CalculateMaxStamina(
        AttributeData attributes,
        float baseStamina)
    {
        // 耐力影响耐力条上限。
        // Endurance affects maximum stamina.
        return baseStamina;
    }

    /// <summary>
    ///     计算耐力恢复速度。
    ///     Calculates stamina regeneration rate.
    /// </summary>
    public static float CalculateStaminaRegen(
        AttributeData attributes,
        float baseRegen)
    {
        // 耐力影响耐力恢复速度。
        // Endurance affects stamina regeneration.
        return baseRegen;
    }

    /// <summary>
    ///     计算魔力恢复速度。
    ///     Calculates mana regeneration rate.
    /// </summary>
    public static float CalculateManaRegen(
        AttributeData attributes,
        float baseRegen)
    {
        // 智力影响魔力恢复速度。
        // Intelligence affects mana regeneration.
        return baseRegen;
    }
}