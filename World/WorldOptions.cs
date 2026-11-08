using System;
using Terraria.ModLoader.IO;

namespace OccidentMod.World;

/// <summary>
///     定義輓歌的自訂世界難度。
///     Defines custom world difficulty levels for Occident.
/// </summary>
public enum OccidentWorldDifficulty
{
    None = 0,
    Longentity = 1,
    Elation = 2,
    Finality = 3
}

/// <summary>
///     儲存自訂世界難度與超大型世界設定。
///     Stores custom world difficulty and extra-large world settings.
/// </summary>
/// <remarks>
///     自訂難度與尺寸獨立於原版 GameMode 及實際地圖大小。
///     Custom difficulty and size settings are independent
///     of vanilla GameMode and physical world dimensions.
/// </remarks>
public readonly record struct WorldOptions(
    OccidentWorldDifficulty Difficulty,
    bool ExtraLarge)
{
    private const string DifficultyKey = "Difficulty";
    private const string ExtraLargeKey = "ExtraLarge";

    /// <summary>
    ///     將世界選項寫入模組存檔資料。
    ///     Writes world options into mod save data.
    /// </summary>
    public void Save(TagCompound tag)
    {
        tag[DifficultyKey] = Difficulty.ToString();
        tag[ExtraLargeKey] = ExtraLarge;
    }

    /// <summary>
    ///     從模組存檔資料讀取世界選項。
    ///     Reads world options from mod save data.
    /// </summary>
    /// <remarks>
    ///     相容舊版本難度名稱，並在後續儲存時使用現行名稱。
    ///     Supports legacy difficulty names and writes
    ///     their current names on subsequent saves.
    /// </remarks>
    public static WorldOptions Read(TagCompound tag)
    {
        var storedName = tag.GetString(DifficultyKey);
        var canonicalName = ResolveLegacyName(storedName);

        var difficulty =
            ParseDifficulty(canonicalName);

        var extraLarge = tag.GetBool(ExtraLargeKey);

        return new WorldOptions(difficulty, extraLarge);
    }

    /// <summary>
    ///     將舊版難度名稱轉換為現行名稱。
    ///     Converts legacy difficulty names into current names.
    /// </summary>
    private static string ResolveLegacyName(string name)
    {
        return name switch
        {
            "Omen" => nameof(OccidentWorldDifficulty.Longentity),
            "Madness" => nameof(OccidentWorldDifficulty.Elation),
            "Despair" => nameof(OccidentWorldDifficulty.Finality),
            _ => name
        };
    }

    /// <summary>
    ///     解析並驗證自訂世界難度。
    ///     Parses and validates a custom world difficulty.
    /// </summary>
    private static OccidentWorldDifficulty ParseDifficulty(string name)
    {
        if (!Enum.TryParse(name, out OccidentWorldDifficulty difficulty))
            return OccidentWorldDifficulty.None;

        return Enum.IsDefined(difficulty)
            ? difficulty
            : OccidentWorldDifficulty.None;
    }
}