#nullable enable
using System;
using System.Collections.Generic;
using OccidentMod.Gameplay.Classes;
using Terraria.Utilities;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     提供職業詞綴的共用隨機抽取功能。
///     Provides shared random rolling for class prefixes.
/// </summary>
/// <remarks>
///     支援自然生成、職業類別篩選與自訂候選池。
///     不負責物品生成時機或鍛造石消耗。
///     Supports natural rolls, category filtering and custom pools.
///     Does not handle item spawning or forging stone consumption.
/// </remarks>
public static class ClassPrefixRoller
{
    /// <summary>
    ///     自然生成時使用的全部職業。
    ///     All classes available for natural generation.
    /// </summary>
    private static readonly ClassType[] AllClasses =
    {
        ClassType.Blademaster,
        ClassType.Goliath,
        ClassType.Rogue,
        ClassType.Knight,
        ClassType.Priest,
        ClassType.Apothecary,
        ClassType.Sage,
        ClassType.Warlock,
        ClassType.Bard,
        ClassType.Skirmisher,
        ClassType.Musketeer,
        ClassType.Archer,
        ClassType.Cultist,
        ClassType.Evoker,
        ClassType.Druid
    };

    /// <summary>
    ///     依武器類別權重自然抽取職業。
    ///     Rolls a class using natural weapon category weights.
    /// </summary>
    public static ClassType RollNatural(
        UnifiedRandom random,
        ClassCategory weaponCategory)
    {
        return RollCustom(
            random,
            AllClasses,
            classType => ClassPrefixWeights.GetWeight(
                classType,
                weaponCategory
            )
        );
    }

    /// <summary>
    ///     從一個或多個職業類別中抽取。
    ///     Rolls from one or more class categories.
    /// </summary>
    public static ClassType RollByCategories(
        UnifiedRandom random,
        params ClassCategory[] categories)
    {
        return RollByCategories(
            random,
            categories,
            null
        );
    }

    /// <summary>
    ///     從指定類別中依自訂權重抽取。
    ///     Rolls from selected categories using custom weights.
    /// </summary>
    public static ClassType RollByCategories(
        UnifiedRandom random,
        IReadOnlyCollection<ClassCategory> categories,
        Func<ClassType, int>? weightSelector)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(categories);

        HashSet<ClassCategory> allowed = new(categories);
        List<ClassType> candidates = new();

        foreach (var classType in AllClasses)
        {
            var category =
                ClassCatalog.GetCategory(classType);

            if (allowed.Contains(category))
                candidates.Add(classType);
        }

        return RollCustom(
            random,
            candidates,
            weightSelector
        );
    }

    /// <summary>
    ///     從自訂職業候選池中加權抽取。
    ///     Performs a weighted roll from a custom class pool.
    /// </summary>
    /// <remarks>
    ///     未指定權重時，各候選職業機率相同。
    ///     無效、重複或非正數權重的候選項會被排除。
    ///     Without weights, all candidates have equal odds.
    ///     Invalid, duplicate and non-positive-weight entries
    ///     are excluded.
    /// </remarks>
    public static ClassType RollCustom(
        UnifiedRandom random,
        IEnumerable<ClassType> candidates,
        Func<ClassType, int>? weightSelector = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(candidates);

        List<(ClassType Class, int Weight)> pool = new();
        HashSet<ClassType> seen = new();

        long totalWeight = 0;

        foreach (var classType in candidates)
        {
            if (!ClassCatalog.IsValid(classType))
                continue;

            if (!seen.Add(classType))
                continue;

            var weight = weightSelector?.Invoke(classType) ?? 1;

            if (weight <= 0)
                continue;

            pool.Add((classType, weight));
            totalWeight += weight;
        }

        if (totalWeight <= 0)
            return ClassType.None;

        // 使用 Terraria 的隨機來源。
        // Use Terraria's random number generator.
        var roll = random.NextDouble() * totalWeight;

        foreach (var (classType, weight) in pool)
        {
            if (roll < weight)
                return classType;

            roll -= weight;
        }

        return pool[^1].Class;
    }
}