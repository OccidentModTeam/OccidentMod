using System;
using Terraria.Utilities;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     管理自然生成職業詞綴的共用機率。
///     Manages the shared natural class prefix generation chance.
/// </summary>
/// <remarks>
///     此機率作為新來源規則的預設值。
///     不直接處理物品生成事件。
///     This chance is the default for new source rules.
///     Does not directly handle item spawning.
/// </remarks>
public static class ClassPrefixGenerationConfig
{
    /// <summary>
    ///     預設自然生成機率，暫定為百分之十。
    ///     Default natural generation chance, provisionally ten percent.
    /// </summary>
    public const double DefaultNaturalChance = 0.10;

    /// <summary>
    ///     目前共用自然生成機率。
    ///     Current shared natural generation chance.
    /// </summary>
    private static double _naturalChance = DefaultNaturalChance;

    /// <summary>
    ///     取得目前自然生成機率。
    ///     Gets the current natural generation chance.
    /// </summary>

    public static double NaturalChance => NaturalChance;

    /// <summary>
    ///     嘗試設定自然生成機率。
    ///     Attempts to set the natural generation chance.
    /// </summary>
    public static bool TrySetNaturalChance(double chance)
    {
        if (!double.IsFinite(chance))
            return false;

        _naturalChance = Math.Clamp(chance, 0.0, 1.0);

        return true;
    }

    /// <summary>
    ///     進行一次自然生成機率檢定。
    ///     Performs one natural generation chance check.
    /// </summary>
    public static bool ShouldGenerateNaturally(
        UnifiedRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (_naturalChance <= 0.0)
            return false;

        if (_naturalChance >= 1.0)
            return true;

        return random.NextDouble() < _naturalChance;
    }

    /// <summary>
    ///     恢復預設自然生成機率。
    ///     Restores the default natural generation chance.
    /// </summary>
    public static void Reset()
    {
        _naturalChance = DefaultNaturalChance;
    }
}