using OccidentMod.Gameplay.Classes;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     將 Terraria 武器傷害類型轉換為職業類別。
///     Resolves Terraria weapon damage types into class categories.
/// </summary>
/// <remarks>
///     集中管理武器類別辨識規則。
///     不負責職業詞綴抽取或物品資料修改。
///     Centralizes weapon category resolution.
///     Does not roll class prefixes or modify item data.
/// </remarks>
public static class ClassPrefixCategoryResolver
{
    /// <summary>
    ///     取得物品所屬的職業類別。
    ///     Gets the class category associated with an item.
    /// </summary>
    /// <remarks>
    ///     無法辨識或不符合條件時回傳 None。
    ///     Returns None when the item is unsupported or unrecognized.
    /// </remarks>
    public static ClassCategory Resolve(Item item)
    {
        if (item is null || item.IsAir || item.damage <= 0)
            return ClassCategory.None;

        return Resolve(item.DamageType);
    }

    /// <summary>
    ///     將傷害類型轉換為職業類別。
    ///     Resolves a damage class into a class category.
    /// </summary>
    /// <remarks>
    ///     混合傷害類型依照優先順序選擇一個主要類別。
    ///     Hybrid damage classes use a deterministic priority order.
    /// </remarks>
    public static ClassCategory Resolve(DamageClass damageClass)
    {
        if (damageClass is null)
            return ClassCategory.None;

        // 優先辨識明確的原版傷害類型。
        // Prioritize exact vanilla damage classes.
        if (damageClass == DamageClass.Melee)
            return ClassCategory.Melee;

        if (damageClass == DamageClass.Magic)
            return ClassCategory.Magic;

        if (damageClass == DamageClass.Ranged)
            return ClassCategory.Ranged;

        if (damageClass == DamageClass.Summon)
            return ClassCategory.Summon;

        // 支援繼承原版傷害類型的自訂職業。
        // Support custom damage classes inheriting vanilla classes.
        if (damageClass.CountsAsClass(DamageClass.Melee))
            return ClassCategory.Melee;

        if (damageClass.CountsAsClass(DamageClass.Magic))
            return ClassCategory.Magic;

        if (damageClass.CountsAsClass(DamageClass.Ranged))
            return ClassCategory.Ranged;

        if (damageClass.CountsAsClass(DamageClass.Summon))
            return ClassCategory.Summon;

        return ClassCategory.None;
    }
}