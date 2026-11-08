using OccidentMod.Gameplay.Classes;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     储存单件物品的职业词缀数据。
///     Stores class prefix data for an individual item.
/// </summary>
/// <remarks>
///     职业词缀独立于 Terraria 原版词缀。
///     所有职业词缀均可出现在任何符合条件的武器上。
///     Class prefixes are independent of vanilla prefixes.
///     Any class prefix can appear on any eligible weapon.
/// </remarks>
public sealed class ClassPrefixData
{
    /// <summary>
    ///     当前物品拥有的职业词缀。
    ///     Class prefix assigned to this item.
    /// </summary>

    public ClassType Class { get; private set; } = ClassType.None;

    /// <summary>
    ///     物品是否拥有有效的职业词缀。
    ///     Whether this item has a valid class prefix.
    /// </summary>

    public bool HasPrefix => ClassCatalog.IsValid(Class);

    /// <summary>
    ///     设置物品的职业词缀。
    ///     Assigns a class prefix to this item.
    /// </summary>
    /// <remarks>
    ///     传入无效职业时会清除现有词缀。
    ///     Invalid class values clear the current prefix.
    /// </remarks>
    public void Set(ClassType classType)
    {
        Class = ClassCatalog.IsValid(classType)
            ? classType
            : ClassType.None;
    }

    /// <summary>
    ///     清除物品的职业词缀。
    ///     Removes the class prefix from this item.
    /// </summary>
    public void Clear()
    {
        Class = ClassType.None;
    }

    /// <summary>
    ///     创建当前职业词缀数据的独立副本。
    ///     Creates an independent copy of this prefix data.
    /// </summary>
    public ClassPrefixData Clone()
    {
        ClassPrefixData copy = new();
        copy.Set(Class);

        return copy;
    }
}