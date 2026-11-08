using OccidentMod.Gameplay.Classes;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     定义职业词缀的随机生成权重。
///     Defines random generation weights for class prefixes.
/// </summary>
/// <remarks>
///     所有职业都可能出现在任何武器类型上。
///     与武器类型相符的职业拥有较高权重。
///     Every class can appear on any weapon category.
///     Matching classes receive higher weights.
/// </remarks>
public static class ClassPrefixWeights
{
    /// <summary>
    ///     相同武器类型的职业权重。
    ///     Weight for a matching weapon category.
    /// </summary>
    public const int MatchingWeight = 10;

    /// <summary>
    ///     不同武器类型的职业权重。
    ///     Weight for a non-matching weapon category.
    /// </summary>
    public const int OtherWeight = 1;

    /// <summary>
    ///     获取指定职业在某种武器类型下的抽取权重。
    ///     Gets the roll weight for a class and weapon category.
    /// </summary>
    public static int GetWeight(
        ClassType classType,
        ClassCategory weaponCategory)
    {
        if (!ClassCatalog.IsValid(classType))
            return 0;

        if (weaponCategory == ClassCategory.None)
            return OtherWeight;

        var classCategory =
            ClassCatalog.GetCategory(classType);

        return classCategory == weaponCategory
            ? MatchingWeight
            : OtherWeight;
    }
}