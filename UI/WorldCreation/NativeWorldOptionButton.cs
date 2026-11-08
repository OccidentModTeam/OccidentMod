using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OccidentMod.UI.WorldCreation;

/// <summary>
///     使用原版 GroupOptionButton 呈現自訂世界選項。
///     Displays custom world options using the vanilla
///     GroupOptionButton appearance and behavior.
/// </summary>
/// <remarks>
///     支援自訂圖示、文字顏色、滑鼠互動與選取狀態同步。
///     Supports custom icons, text colors, mouse interaction,
///     and selection state synchronization.
///     保留原版按鈕的九宮格背景與高亮效果。
///     Preserves vanilla button backgrounds and highlights.
/// </remarks>
internal sealed class NativeWorldOptionButton : GroupOptionButton<int>
{
    private const string LocalizationPrefix =
        "Mods.OccidentMod.UI.WorldCreation.";

    private const string AssetFolder =
        "OccidentMod/Assets/UI/WorldCreation/";

    private const float IconAreaWidth = 32f;
    private const float IconLeftOffset = 1f;

    private const float InactiveIconOpacity = 0.7f;

    private readonly Func<bool> isSelected;

    private readonly Asset<Texture2D> optionIcon;

    /// <summary>
    ///     建立原版風格的自訂世界選項按鈕。
    ///     Creates a custom world option button
    ///     with vanilla styling.
    /// </summary>
    /// <param name="key">
    ///     選項的本地化識別名稱。
    ///     Localization key identifying the option.
    /// </param>
    /// <param name="click">
    ///     按鈕被點擊時執行的動作。
    ///     Action invoked when the button is clicked.
    /// </param>
    /// <param name="selected">
    ///     提供目前選取狀態的回呼。
    ///     Callback providing the current selection state.
    /// </param>
    public NativeWorldOptionButton(
        string key,
        Action click,
        Func<bool> selected)
        : base(
            1,
            Language.GetText(LocalizationPrefix + key),
            GetDescription(key),
            GetTextColor(key),
            null,
            IsTab(key) ? 0.75f : 1f,
            IsTab(key) ? 0.5f : 1f,
            IsTab(key) ? 0f : 16f)
    {
        isSelected = selected;

        var assetName = GetIconAssetName(key);

        if (assetName is not null)
            optionIcon = ModContent.Request<Texture2D>(
                AssetFolder + assetName);

        OnLeftMouseDown += (_, _) => click();

        SetCurrentOption(
            isSelected() ? 1 : 0);
    }

    /// <summary>
    ///     每幀同步按鈕的選取狀態。
    ///     Synchronizes the button's selection state
    ///     on every update.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        SetCurrentOption(
            isSelected() ? 1 : 0);

        base.Update(gameTime);
    }

    /// <summary>
    ///     繪製原版按鈕與可選的自訂圖示。
    ///     Draws the vanilla button and its optional custom icon.
    /// </summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (optionIcon is null)
            return;

        DrawOptionIcon(spriteBatch, optionIcon.Value);
    }

    /// <summary>
    ///     將自訂圖示以原始尺寸繪製於原版圖示區域中央。
    ///     Draws the custom icon at its original size,
    ///     centered within the vanilla icon area.
    /// </summary>
    private void DrawOptionIcon(
        SpriteBatch spriteBatch,
        Texture2D texture)
    {
        var area = GetDimensions();

        var x =
            area.X +
            IconLeftOffset +
            (IconAreaWidth - texture.Width) * 0.5f;

        var y =
            area.Y +
            (area.Height - texture.Height) * 0.5f;

        Vector2 position = new(x, y);

        var color = IsSelected || IsMouseHovering
            ? Color.White
            : Color.White * InactiveIconOpacity;

        spriteBatch.Draw(
            texture,
            position,
            color);
    }

    /// <summary>
    ///     判斷選項是否為難度分頁按鈕。
    ///     Determines whether the option is a difficulty tab.
    /// </summary>
    private static bool IsTab(string key)
    {
        return key is "Vanilla" or "ModTab";
    }

    /// <summary>
    ///     取得選項對應的自訂圖示路徑。
    ///     Gets the custom icon asset path for an option.
    /// </summary>
    private static string GetIconAssetName(string key)
    {
        return key switch
        {
            "Longentity" => "Difficulty/Icons/Longentity",
            "Elation" => "Difficulty/Icons/Elation",
            "Finality" => "Difficulty/Icons/Finality",
            "ExtraLarge" => "Size/GiantWorld",
            _ => null
        };
    }

    /// <summary>
    ///     取得選項對應的本地化說明文字。
    ///     Gets the localized description for an option.
    /// </summary>
    private static LocalizedText GetDescription(string key)
    {
        return key switch
        {
            "ExtraLarge" =>
                Language.GetText(
                    LocalizationPrefix + "ExtraLargeHelp"),

            "Longentity" or "Elation" or "Finality" =>
                Language.GetText(
                    LocalizationPrefix +
                    key +
                    "Description"),

            _ => LocalizedText.Empty
        };
    }

    /// <summary>
    ///     取得不同世界選項的文字顏色。
    ///     Gets the text color for each world option.
    /// </summary>
    private static Color GetTextColor(string key)
    {
        return key switch
        {
            "ExtraLarge" => Color.LimeGreen,
            "Elation" => Main.mcColor,
            "Finality" => Main.hcColor,
            _ => Color.White
        };
    }
}