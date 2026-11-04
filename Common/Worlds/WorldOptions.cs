// 自訂世界難度與特大尺寸的儲存格式，並保留舊名稱讀取對照。
using System;
using Terraria.ModLoader.IO;

namespace Occident.Common.Worlds;

public enum OccidentWorldDifficulty { None = 0, Longentity = 1, Elation = 2, Finality = 3 }

// 與原版 GameMode、實際地圖尺寸分開紀錄，之後可接上各自的規則與生成器。
public readonly record struct WorldOptions(OccidentWorldDifficulty Difficulty, bool ExtraLarge)
{
    public void Save(TagCompound tag)
    {
        tag["Difficulty"] = Difficulty.ToString();
        tag["ExtraLarge"] = ExtraLarge;
    }

    public static WorldOptions Read(TagCompound tag)
    {
        // 讀取世界與標頭時接受舊難度名稱，新存檔統一使用現名。
        string storedName = tag.GetString("Difficulty") switch
        {
            "Omen" => nameof(OccidentWorldDifficulty.Longentity),
            "Madness" => nameof(OccidentWorldDifficulty.Elation),
            "Despair" => nameof(OccidentWorldDifficulty.Finality),
            var name => name
        };
        var difficulty = Enum.TryParse<OccidentWorldDifficulty>(storedName, out var value)
            && Enum.IsDefined(value) ? value : OccidentWorldDifficulty.None;
        return new(difficulty, tag.GetBool("ExtraLarge"));
    }
}

