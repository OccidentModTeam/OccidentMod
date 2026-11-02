using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Occident.Common.UI.CharacterCreation;

internal sealed class ClassDifficultyText : UIText
{
    private const float Scale = 0.9f;
    private static readonly Color[] UnknownColors =
    {
        new(255, 110, 110), new(255, 192, 90), new(130, 225, 160),
        new(115, 210, 255), new(190, 145, 255), new(245, 125, 210)
    };
    private string prefix = "", value = "", suffix = "";
    private int difficulty;
    private ReLogic.Graphics.DynamicSpriteFont measuredFont;
    private float prefixWidth, valueWidth;

    public ClassDifficultyText() : base("", Scale) { }

    public void SetDifficulty(string level, int difficulty)
    {
        this.difficulty = difficulty;
        measuredFont = null;
        value = level;
        string fullText = Language.GetTextValue("Mods.OccidentMod.UI.CharacterCreation.Difficulty", level);
        int index = fullText.IndexOf(level, StringComparison.Ordinal);
        prefix = index >= 0 ? fullText.Substring(0, index) : fullText;
        suffix = index >= 0 ? fullText.Substring(index + level.Length) : "";
        if (index < 0)
            value = "";
        SetText(fullText);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var font = FontAssets.MouseText.Value;
        if (!ReferenceEquals(measuredFont, font))
        {
            prefixWidth = font.MeasureString(prefix).X * Scale;
            valueWidth = font.MeasureString(value).X * Scale;
            measuredFont = font;
        }
        Vector2 position = GetInnerDimensions().Position() - new Vector2(0f, 2f * Scale);
        Utils.DrawBorderString(spriteBatch, prefix, position, Color.White, Scale);
        position.X += prefixWidth;

        Color color = difficulty switch
        {
            1 => new Color(135, 206, 250),
            2 => new Color(255, 183, 77),
            3 => new Color(241, 91, 91),
            4 => new Color(195, 140, 255),
            5 => new Color(245, 195, 85),
            _ => new Color(185, 190, 205)
        };
        if (difficulty == 0 && value == "???")
            color = UnknownColors[(int)(Main.GlobalTimeWrappedHourly * 12f) % UnknownColors.Length];
        if (difficulty == 5)
            Utils.DrawBorderStringFourWay(spriteBatch, font, value, position.X, position.Y,
                color, new Color(125, 25, 40), Vector2.Zero, Scale);
        else
            Utils.DrawBorderString(spriteBatch, value, position, color, Scale);

        position.X += valueWidth;
        Utils.DrawBorderString(spriteBatch, suffix, position, Color.White, Scale);
    }
}
