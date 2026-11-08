#nullable enable
using System;
using System.Collections.Generic;
using Terraria.Utilities;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     集中管理各物品取得來源的生成規則。
///     Centrally manages generation rules for acquisition sources.
/// </summary>
/// <remarks>
///     所有來源共用規則管理機制。
///     不預設特定來源的品質階級。
///     All sources share the same rule management system.
///     No source-specific quality tiers are predefined.
/// </remarks>
public static class ClassPrefixSourceRegistry
{
    /// <summary>
    ///     已註冊的來源規則。
    ///     Registered acquisition source rules.
    /// </summary>
    private static readonly Dictionary<
        ClassPrefixSource,
        ClassPrefixSourceRule> Rules = new();

    /// <summary>
    ///     嘗試取得已註冊的來源規則。
    ///     Attempts to retrieve a registered source rule.
    /// </summary>
    public static bool TryGetRule(
        ClassPrefixSource source,
        out ClassPrefixSourceRule? rule)
    {
        return Rules.TryGetValue(source, out rule);
    }

    /// <summary>
    ///     註冊或替換來源規則。
    ///     Registers or replaces a source rule.
    /// </summary>
    public static void Register(ClassPrefixSourceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (!Enum.IsDefined(typeof(ClassPrefixSource), rule.Source))
            throw new ArgumentOutOfRangeException(nameof(rule));

        Rules[rule.Source] = rule;
    }

    /// <summary>
    ///     取得來源規則，不存在時使用預設機率建立。
    ///     Gets a source rule, creating one with the default chance if absent.
    /// </summary>
    public static ClassPrefixSourceRule GetOrCreate(
        ClassPrefixSource source)
    {
        if (!Enum.IsDefined(typeof(ClassPrefixSource), source))
            throw new ArgumentOutOfRangeException(nameof(source));

        if (Rules.TryGetValue(source, out var rule))
            return rule;

        rule = new ClassPrefixSourceRule(
            source,
            ClassPrefixGenerationConfig.NaturalChance
        );

        Rules.Add(source, rule);

        return rule;
    }

    /// <summary>
    ///     依指定來源進行一次生成機率檢定。
    ///     Performs one generation chance check for a source.
    /// </summary>
    public static bool ShouldGenerate(
        ClassPrefixSource source,
        UnifiedRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (source == ClassPrefixSource.Unknown)
            return false;

        return GetOrCreate(source).ShouldGenerate(random);
    }

    /// <summary>
    ///     取得指定來源的品質權重倍率。
    ///     Gets the quality weight multiplier for a source.
    /// </summary>
    public static double GetQualityWeightMultiplier(
        ClassPrefixSource source)
    {
        return GetOrCreate(source).QualityWeightMultiplier;
    }

    /// <summary>
    ///     移除指定來源規則。
    ///     Removes a registered source rule.
    /// </summary>
    public static bool Remove(ClassPrefixSource source)
    {
        return Rules.Remove(source);
    }

    /// <summary>
    ///     清除所有來源規則。
    ///     Clears all source rules.
    /// </summary>
    public static void Clear()
    {
        Rules.Clear();
    }
}