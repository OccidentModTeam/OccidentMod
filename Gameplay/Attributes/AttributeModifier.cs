using System;

namespace OccidentMod.Gameplay.Attributes;

/// <summary>
///     储存角色八项属性的额外加成。
///     Stores additional bonuses for the eight core attributes.
/// </summary>
/// <remarks>
///     此类不修改玩家永久投入的属性点。
///     可用于装备、Buff、职业或其他临时效果。
///     Does not modify permanently allocated attributes.
///     Can be used for equipment, buffs, classes and other effects.
/// </remarks>
public sealed class AttributeModifier
{
    private readonly int[] bonuses = new int[8];

    /// <summary>
    ///     获取指定属性的额外加成。
    ///     Gets the bonus for an attribute.
    /// </summary>
    public int Get(AttributeType type)
    {
        var index = (int)type;

        if (index < 0 || index >= bonuses.Length)
            return 0;

        return bonuses[index];
    }

    /// <summary>
    ///     设置指定属性的额外加成。
    ///     Sets the bonus for an attribute.
    /// </summary>
    public void Set(AttributeType type, int value)
    {
        var index = (int)type;

        if (index < 0 || index >= bonuses.Length)
            return;

        bonuses[index] = value;
    }

    /// <summary>
    ///     累加指定属性的额外加成。
    ///     Adds a bonus to an attribute.
    /// </summary>
    public void Add(AttributeType type, int amount)
    {
        var index = (int)type;

        if (index < 0 || index >= bonuses.Length)
            return;

        bonuses[index] = (int)Math.Clamp(
            (long)bonuses[index] + amount,
            int.MinValue,
            int.MaxValue
        );
    }

    /// <summary>
    ///     取得基础属性与额外加成的合计值。
    ///     Gets the combined base attribute and bonus value.
    /// </summary>
    public int GetTotal(AttributeData attributes, AttributeType type)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        return (int)Math.Clamp(
            (long)attributes.Get(type) + Get(type),
            0L,
            int.MaxValue
        );
    }

    /// <summary>
    ///     清除所有额外属性加成。
    ///     Clears all additional attribute bonuses.
    /// </summary>
    public void Reset()
    {
        Array.Clear(bonuses, 0, bonuses.Length);
    }
}