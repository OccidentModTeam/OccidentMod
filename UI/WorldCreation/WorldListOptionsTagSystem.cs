using Microsoft.Xna.Framework;
using MonoMod.Cil;
using OccidentMod.World;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.ModLoader;

namespace OccidentMod.UI.WorldCreation;

/// <summary>
///     在世界列表中顯示自訂世界難度與尺寸名稱。
///     Displays custom world difficulty and size labels
///     in the vanilla world selection list.
/// </summary>
/// <remarks>
///     從世界存檔標頭讀取自訂設定，
///     僅修改介面顯示，不更動世界資料。
///     Reads custom settings from world save headers.
///     Only changes UI labels without modifying world data.
/// </remarks>
[Autoload(Side = ModSide.Client)]
public sealed class WorldListOptionsTagSystem : ModSystem
{
    private static readonly Color CustomDifficultyColor =
        new(215, 177, 112);

    /// <summary>
    ///     註冊世界列表的難度與尺寸顯示攔截。
    ///     Registers hooks for difficulty and size
    ///     labels in the world selection list.
    /// </summary>
    public override void Load()
    {
        On_AWorldListItem.GetDifficulty += GetDifficulty;

        IL_UIWorldListItem.DrawSelf += ShowExtraLarge;
    }

    /// <summary>
    ///     移除世界列表相關攔截。
    ///     Removes world selection list hooks.
    /// </summary>
    public override void Unload()
    {
        On_AWorldListItem.GetDifficulty -= GetDifficulty;

        IL_UIWorldListItem.DrawSelf -= ShowExtraLarge;
    }

    /// <summary>
    ///     從世界存檔標頭讀取模組世界設定。
    ///     Reads custom world options from
    ///     the world save header.
    /// </summary>
    private static WorldOptions ReadOptions(WorldFileData data)
    {
        if (!data.TryGetHeaderData<WorldOptionsSystem>(
                out var header))
            return default;

        return WorldOptions.Read(header);
    }

    /// <summary>
    ///     將原版世界難度文字替換為模組自訂難度名稱。
    ///     Replaces the vanilla difficulty label
    ///     with a custom difficulty name when applicable.
    /// </summary>
    private static void GetDifficulty(
        On_AWorldListItem.orig_GetDifficulty orig,
        AWorldListItem item,
        out string text,
        out Color color)
    {
        // 優先保留原版的難度判定與顏色。
        // Preserve vanilla difficulty information
        // unless a custom difficulty is present.

        orig(item, out text, out color);

        var options = ReadOptions(item.Data);

        if (options.Difficulty == OccidentWorldDifficulty.None)
            return;

        text = WorldCreationChoices.Text(
            options.Difficulty.ToString());

        color = CustomDifficultyColor;
    }

    /// <summary>
    ///     攔截原版世界尺寸名稱的繪製來源。
    ///     Intercepts the vanilla world size name
    ///     used during world list rendering.
    /// </summary>
    /// <remarks>
    ///     若找不到預期的 IL 指令，保留原版行為並記錄警告。
    ///     If the expected IL instruction cannot be found,
    ///     preserves vanilla behavior and logs a warning.
    /// </remarks>
    private static void ShowExtraLarge(ILContext il)
    {
        var cursor = new ILCursor(il);

        var found = cursor.TryGotoNext(
            MoveType.Before,
            instruction =>
                instruction.MatchCallvirt<WorldFileData>(
                    "get_WorldSizeName")
                ||
                instruction.MatchCall<WorldFileData>(
                    "get_WorldSizeName"));

        if (!found)
        {
            LogUnavailableSizeHook();
            return;
        }

        // 移除原本取得 WorldSizeName 的呼叫，
        // 改為根據存檔設定解析顯示文字。
        //
        // Replace the original WorldSizeName getter
        // with a custom label resolver.

        cursor.Remove();

        cursor.EmitDelegate(
            ResolveWorldSizeName);
    }

    /// <summary>
    ///     取得世界列表應顯示的尺寸名稱。
    ///     Resolves the world size name
    ///     displayed in the world selection list.
    /// </summary>
    private static string ResolveWorldSizeName(
        WorldFileData data)
    {
        var options = ReadOptions(data);

        if (options.ExtraLarge) return WorldCreationChoices.Text("ExtraLarge");

        return data.WorldSizeName;
    }

    /// <summary>
    ///     記錄世界尺寸標籤攔截失敗的警告。
    ///     Logs a warning when the world size
    ///     label hook cannot be applied.
    /// </summary>
    private static void LogUnavailableSizeHook()
    {
        ModContent
            .GetInstance<WorldListOptionsTagSystem>()
            .Mod.Logger.Warn(
                "World size label hook is unavailable; " +
                "keeping the existing world list label. " +
                "Another mod or tModLoader version may " +
                "have changed UIWorldListItem.DrawSelf.");
    }
}