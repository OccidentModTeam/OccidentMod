using System;

namespace OccidentMod.Gameplay.Attributes;

/// <summary>
///     储存角色的八项基础属性数值。
///     Stores the eight core character attribute values.
/// </summary>
/// <remarks>
///     此类只管理属性数据，不负责属性效果计算。
///     This class only manages attribute data, not gameplay calculations.
/// </remarks>
public sealed class AttributeData
{
    /// <summary>
    ///     生命力。
    ///     Vigor attribute value.
    /// </summary>

    public int Vigor { get; private set; }

    /// <summary>
    ///     精神。
    ///     Mind attribute value.
    /// </summary>

    public int Mind { get; private set; }

    /// <summary>
    ///     耐力。
    ///     Endurance attribute value.
    /// </summary>

    public int Endurance { get; private set; }

    /// <summary>
    ///     力量。
    ///     Strength attribute value.
    /// </summary>

    public int Strength { get; private set; }

    /// <summary>
    ///     敏捷。
    ///     Dexterity attribute value.
    /// </summary>

    public int Dexterity { get; private set; }

    /// <summary>
    ///     智力。
    ///     Intelligence attribute value.
    /// </summary>

    public int Intelligence { get; private set; }

    /// <summary>
    ///     信仰。
    ///     Faith attribute value.
    /// </summary>

    public int Faith { get; private set; }

    /// <summary>
    ///     奥术。
    ///     Arcane attribute value.
    /// </summary>

    public int Arcane { get; private set; }

    /// <summary>
    ///     获取指定属性的数值。
    ///     Gets the value of an attribute.
    /// </summary>
    public int Get(AttributeType type)
    {
        return type switch
        {
            AttributeType.Vigor => Vigor,
            AttributeType.Mind => Mind,
            AttributeType.Endurance => Endurance,
            AttributeType.Strength => Strength,
            AttributeType.Dexterity => Dexterity,
            AttributeType.Intelligence => Intelligence,
            AttributeType.Faith => Faith,
            AttributeType.Arcane => Arcane,
            _ => 0
        };
    }

    /// <summary>
    ///     设置指定属性的数值。
    ///     Sets the value of an attribute.
    /// </summary>
    /// <remarks>
    ///     属性值不能低于零。
    ///     Attribute values cannot be negative.
    /// </remarks>
    public void Set(AttributeType type, int value)
    {
        value = Math.Max(0, value);

        switch (type)
        {
            case AttributeType.Vigor:
                Vigor = value;
                break;

            case AttributeType.Mind:
                Mind = value;
                break;

            case AttributeType.Endurance:
                Endurance = value;
                break;

            case AttributeType.Strength:
                Strength = value;
                break;

            case AttributeType.Dexterity:
                Dexterity = value;
                break;

            case AttributeType.Intelligence:
                Intelligence = value;
                break;

            case AttributeType.Faith:
                Faith = value;
                break;

            case AttributeType.Arcane:
                Arcane = value;
                break;
        }
    }

    /// <summary>
    ///     增加指定属性的数值。
    ///     Adds points to an attribute.
    /// </summary>
    public void Add(AttributeType type, int amount)
    {
        Set(type, (int)Math.Clamp(
            (long)Get(type) + amount,
            0L,
            int.MaxValue
        ));
    }

    /// <summary>
    ///     重置所有属性数值。
    ///     Resets all attribute values.
    /// </summary>
    public void Reset()
    {
        Vigor = 0;
        Mind = 0;
        Endurance = 0;
        Strength = 0;
        Dexterity = 0;
        Intelligence = 0;
        Faith = 0;
        Arcane = 0;
    }
}