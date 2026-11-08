using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     顯示職業操作難度，並依照難度等級繪製對應的文字顏色。
///     Displays class difficulty with level-specific text colors.
/// </summary>
/// <remarks>
///     保留原本的難度文字配色、特殊描邊及未知難度動態效果。
///     文字內容由本地化系統提供，繪製與資料設定彼此分離。
///     Preserves the original difficulty colors, special outlines,
///     and animated unknown-difficulty effect.
///     Text content is localized, while rendering and state updates
///     remain separate.
/// </remarks>
internal sealed class ClassDifficultyText : UIText
{
    private const float TextScale = 0.9f;
    private const float VerticalOffset = 2f;
    private const float UnknownColorSpeed = 12f;

    private const string DifficultyLocalizationKey =
        "Mods.OccidentMod.UI.CharacterCreation.Difficulty";

    private const string UnknownDifficultyText = "???";

    private static readonly Color[] UnknownColors =
    {
        new(255, 110, 110),
        new(255, 192, 90),
        new(130, 225, 160),
        new(115, 210, 255),
        new(190, 145, 255),
        new(245, 125, 210)
    };

    private static readonly Color DefaultColor =
        new(185, 190, 205);

    private static readonly Color MaximumDifficultyOutline =
        new(125, 25, 40);

    private int difficulty;

    private DynamicSpriteFont measuredFont;

    private string prefix = string.Empty;

    private float prefixWidth;
    private string suffix = string.Empty;
    private string value = string.Empty;
    private float valueWidth;

    /// <summary>
    ///     建立職業操作難度文字元件。
    ///     Creates a class difficulty text element.
    /// </summary>
    public ClassDifficultyText() : base(string.Empty, TextScale)
    {
    }

    /// <summary>
    ///     設定難度名稱及對應等級。
    ///     Sets the localized difficulty name and numeric level.
    /// </summary>
    /// <param name="level">
    ///     已本地化的難度名稱。
    ///     Localized difficulty name.
    /// </param>
    /// <param name="difficulty">
    ///     難度等級。
    ///     Numeric difficulty level.
    /// </param>
    public void SetDifficulty(string level, int difficulty)
    {
        this.difficulty = difficulty;

        var fullText = Language.GetTextValue(
            DifficultyLocalizationKey,
            level);

        SplitLocalizedText(fullText, level);

        // 文字變更後，下一次繪製時重新測量寬度。
        // Recalculate text widths after the content changes.
        measuredFont = null;

        SetText(fullText);
    }

    /// <summary>
    ///     將本地化文字拆分為前綴、難度名稱與後綴。
    ///     Splits localized text into prefix, value, and suffix.
    /// </summary>
    private void SplitLocalizedText(
        string fullText,
        string level)
    {
        var index = fullText.IndexOf(
            level,
            StringComparison.Ordinal);

        if (index < 0)
        {
            prefix = fullText;
            value = string.Empty;
            suffix = string.Empty;
            return;
        }

        prefix = fullText[..index];
        value = level;
        suffix = fullText[(index + level.Length)..];
    }

    /// <summary>
    ///     繪製不同顏色的職業操作難度文字。
    ///     Draws class difficulty text using level-specific colors.
    /// </summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var font = FontAssets.MouseText.Value;

        UpdateTextMeasurements(font);

        var position =
            GetInnerDimensions().Position() -
            new Vector2(0f, VerticalOffset * TextScale);

        DrawNormalText(
            spriteBatch,
            prefix,
            position);

        position.X += prefixWidth;

        DrawDifficultyValue(
            spriteBatch,
            font,
            position);

        position.X += valueWidth;

        DrawNormalText(
            spriteBatch,
            suffix,
            position);
    }

    /// <summary>
    ///     更新文字區段的寬度快取。
    ///     Updates cached widths for text segments.
    /// </summary>
    private void UpdateTextMeasurements(DynamicSpriteFont font)
    {
        if (ReferenceEquals(measuredFont, font))
            return;

        prefixWidth =
            font.MeasureString(prefix).X * TextScale;

        valueWidth =
            font.MeasureString(value).X * TextScale;

        measuredFont = font;
    }

    /// <summary>
    ///     使用一般白色文字繪製前綴或後綴。
    ///     Draws prefix or suffix text in white.
    /// </summary>
    private static void DrawNormalText(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Utils.DrawBorderString(
            spriteBatch,
            text,
            position,
            Color.White,
            TextScale);
    }

    /// <summary>
    ///     繪製難度名稱，最高難度使用特殊描邊。
    ///     Draws the difficulty value with a special outline
    ///     for the highest difficulty level.
    /// </summary>
    private void DrawDifficultyValue(
        SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        Vector2 position)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var color = GetDifficultyColor();

        if (difficulty == 5)
        {
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                font,
                value,
                position.X,
                position.Y,
                color,
                MaximumDifficultyOutline,
                Vector2.Zero,
                TextScale);

            return;
        }

        Utils.DrawBorderString(
            spriteBatch,
            value,
            position,
            color,
            TextScale);
    }

    /// <summary>
    ///     取得目前難度對應的文字顏色。
    ///     Gets the text color for the current difficulty level.
    /// </summary>
    private Color GetDifficultyColor()
    {
        if (difficulty == 0 &&
            value == UnknownDifficultyText)
            return GetAnimatedUnknownColor();

        return difficulty switch
        {
            1 => new Color(135, 206, 250),
            2 => new Color(255, 183, 77),
            3 => new Color(241, 91, 91),
            4 => new Color(195, 140, 255),
            5 => new Color(245, 195, 85),
            _ => DefaultColor
        };
    }

    /// <summary>
    ///     取得未知難度的動態文字顏色。
    ///     Gets the animated color for unknown difficulty.
    /// </summary>
    private static Color GetAnimatedUnknownColor()
    {
        var index =
            (int)(Main.GlobalTimeWrappedHourly * UnknownColorSpeed)
            % UnknownColors.Length;

        return UnknownColors[index];
    }
}