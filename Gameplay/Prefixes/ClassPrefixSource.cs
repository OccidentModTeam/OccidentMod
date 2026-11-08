using System;
using Terraria.Utilities;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     定義物品的取得來源。
///     Defines item acquisition sources.
/// </summary>
public enum ClassPrefixSource
{
    Unknown = 0,
    EnemyDrop = 1,
    Chest = 2,
    Crafting = 3,
    Shop = 4,
    OtherLoot = 5
}

/// <summary>
///     儲存指定取得來源的職業詞綴生成規則。
///     Stores class prefix generation rules for an acquisition source.
/// </summary>
/// <remarks>
///     生成機率與品質權重獨立設定。
///     不限制任何來源的最高品質。
///     Generation chance and quality weighting are configured separately.
///     No source has a predefined maximum quality.
/// </remarks>
public sealed class ClassPrefixSourceRule
{
    /// <summary>
    ///     建立物品取得來源規則。
    ///     Creates an acquisition source rule.
    /// </summary>
    public ClassPrefixSourceRule(
        ClassPrefixSource source,
        double generationChance)
    {
        Source = source;
        SetGenerationChance(generationChance);
    }

    /// <summary>
    ///     此規則對應的物品取得來源。
    ///     Acquisition source associated with this rule.
    /// </summary>

    public ClassPrefixSource Source { get; }

    /// <summary>
    ///     是否允許生成職業詞綴。
    ///     Whether class prefix generation is enabled.
    /// </summary>

    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     目前的自然生成機率。
    ///     Current natural generation probability.
    /// </summary>

    public double GenerationChance { get; private set; }

    /// <summary>
    ///     預留給未來品質系統的權重倍率。
    ///     Weight multiplier reserved for the future quality system.
    /// </summary>

    public double QualityWeightMultiplier { get; private set; } = 1.0;

    /// <summary>
    ///     設定自然生成機率。
    ///     Sets the natural generation probability.
    /// </summary>
    public void SetGenerationChance(double chance)
    {
        if (!double.IsFinite(chance))
            throw new ArgumentOutOfRangeException(nameof(chance));

        GenerationChance = Math.Clamp(chance, 0.0, 1.0);
    }

    /// <summary>
    ///     設定品質權重倍率。
    ///     Sets the quality weight multiplier.
    /// </summary>
    public void SetQualityWeightMultiplier(double multiplier)
    {
        if (!double.IsFinite(multiplier) || multiplier < 0.0)
            throw new ArgumentOutOfRangeException(nameof(multiplier));

        QualityWeightMultiplier = multiplier;
    }

    /// <summary>
    ///     判斷是否通過來源生成機率檢定。
    ///     Checks whether source generation succeeds.
    /// </summary>
    public bool ShouldGenerate(UnifiedRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (!Enabled)
            return false;

        if (GenerationChance <= 0.0)
            return false;

        if (GenerationChance >= 1.0)
            return true;

        return random.NextDouble() < GenerationChance;
    }
}