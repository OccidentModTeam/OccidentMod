#nullable enable
using System;
using System.Collections.Generic;
using OccidentMod.Content.Items.Global;
using OccidentMod.Gameplay.Classes;
using Terraria;
using Terraria.Utilities;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     提供物品職業詞綴的生成與重抽功能。
///     Provides item class prefix generation and rerolling.
/// </summary>
/// <remarks>
///     只負責執行抽取並將結果寫入物品。
///     不決定生成機率、來源或道具消耗。
///     Rolls and assigns class prefixes to items.
///     Does not determine generation chances, sources
///     or item consumption.
/// </remarks>
public static class ClassPrefixGeneration
{
    /// <summary>
    ///     嘗試自然生成職業詞綴。
    ///     Attempts to generate a natural class prefix.
    /// </summary>
    public static bool TryGenerateNatural(
        Item item,
        UnifiedRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (!ClassPrefixGlobalItem.CanHaveClassPrefix(item))
            return false;

        var data =
            ClassPrefixGlobalItem.Get(item);

        if (data.HasClassPrefix)
            return false;

        var category =
            ClassPrefixCategoryResolver.Resolve(item);

        var result =
            ClassPrefixRoller.RollNatural(random, category);

        return data.TrySetClassPrefix(item, result);
    }

    /// <summary>
    ///     從指定職業類別重新抽取詞綴。
    ///     Rerolls a prefix from selected class categories.
    /// </summary>
    public static bool TryRerollByCategories(
        Item item,
        UnifiedRandom random,
        IReadOnlyCollection<ClassCategory> categories,
        Func<ClassType, int>? weightSelector = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(categories);

        if (!ClassPrefixGlobalItem.CanHaveClassPrefix(item))
            return false;

        var result =
            ClassPrefixRoller.RollByCategories(
                random,
                categories,
                weightSelector
            );

        return ClassPrefixGlobalItem
            .Get(item)
            .TrySetClassPrefix(item, result);
    }

    /// <summary>
    ///     從自訂職業候選池重新抽取詞綴。
    ///     Rerolls a prefix from a custom class pool.
    /// </summary>
    public static bool TryRerollCustom(
        Item item,
        UnifiedRandom random,
        IEnumerable<ClassType> candidates,
        Func<ClassType, int>? weightSelector = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(candidates);

        if (!ClassPrefixGlobalItem.CanHaveClassPrefix(item))
            return false;

        var result =
            ClassPrefixRoller.RollCustom(
                random,
                candidates,
                weightSelector
            );

        return ClassPrefixGlobalItem
            .Get(item)
            .TrySetClassPrefix(item, result);
    }

    /// <summary>
    ///     嘗試清除物品的職業詞綴。
    ///     Attempts to clear an item's class prefix.
    /// </summary>
    public static bool TryClear(Item item)
    {
        if (!ClassPrefixGlobalItem.CanHaveClassPrefix(item))
            return false;

        var data =
            ClassPrefixGlobalItem.Get(item);

        if (!data.HasClassPrefix)
            return false;

        data.ClearClassPrefix();

        return true;
    }
}