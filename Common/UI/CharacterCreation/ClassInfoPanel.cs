// 顯示所選職業的名稱、說明與能力雷達圖。
using System;
using Microsoft.Xna.Framework;
using Occident.Common.Classes;
using Occident.Common.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Occident.Common.UI.CharacterCreation;

internal sealed class ClassInfoPanel : UIPanel
{
    public const float PanelWidth = 300f;
    public const float PanelHeight = 360f;
    public const float PanelGap = 12f;
    private const string Prefix = "Mods.OccidentMod.UI.CharacterCreation.";
    private readonly Player player;
    private readonly Func<ClassCategory> getCategory;
    private readonly UIText categoryText, ratingsTitle;
    private readonly ClassDifficultyText difficultyText;
    private readonly ClassRadarChart chart;
    private (ClassType Class, ClassCategory Category, string Culture) lastState;

    public ClassInfoPanel(Player player, Func<ClassCategory> getCategory)
    {
        this.player = player;
        this.getCategory = getCategory;
        Width.Set(PanelWidth, 0f);
        Height.Set(PanelHeight, 0f);
        Left.Set(PanelGap, 1f);
        BackgroundColor = new Color(33, 43, 79) * 0.8f;
        SetPadding(16f);

        categoryText = new UIText("", 0.9f);
        categoryText.Top.Set(4f, 0f);
        Append(categoryText);

        AddSeparator(28f);
        ratingsTitle = new UIText(Language.GetText(Prefix + "RatingsTitle"), 0.8f)
        {
            TextColor = new Color(165, 181, 210)
        };
        ratingsTitle.Top.Set(42f, 0f);
        Append(ratingsTitle);

        chart = new ClassRadarChart();
        chart.Top.Set(62f, 0f);
        chart.Height.Set(-138f, 1f);
        Append(chart);

        difficultyText = new ClassDifficultyText { VAlign = 1f };
        difficultyText.Top.Set(-6f, 0f);
        Append(difficultyText);

        var footerLine = new UIHorizontalSeparator(2, true)
        {
            VAlign = 1f,
            Color = new Color(85, 105, 150) * 0.65f,
            IgnoresMouseInteraction = true
        };
        footerLine.Width.Set(0f, 1f);
        footerLine.Top.Set(-44f, 0f);
        Append(footerLine);
    }

    public override void Update(GameTime gameTime)
    {
        RefreshContent();
        base.Update(gameTime);
    }

    public void RefreshContent()
    {
        ClassType current = player.GetModPlayer<ClassPlayer>().CurrentClass;
        ClassCategory category = getCategory();
        var state = (current, category, Language.ActiveCulture.Name);
        if (lastState != state)
        {
            lastState = state;
            categoryText.SetText(Language.GetTextValue(Prefix + "ClassType",
                Language.GetTextValue($"Mods.OccidentMod.ClassCategories.{category}")));
            ratingsTitle.SetText(Language.GetText(Prefix + "RatingsTitle"));
            bool selected = current != ClassType.None && ClassCatalog.GetCategory(current) == category;
            ClassType displayed = selected ? current : ClassType.None;
            chart.HasSelection = selected;
            chart.SetRatings(ClassRatings.Get(displayed));
            string level = Language.GetTextValue(Prefix + "DifficultyLevels." +
                (selected ? ClassPreviewData.GetDifficultyName(current) : "NoSelection"));
            difficultyText.SetDifficulty(level, selected ? ClassPreviewData.GetDifficulty(current) : 0);
            // SetText 會更新元件最小尺寸；換字後重新排版，再進行繪製。
            Recalculate();
        }
    }

    private void AddSeparator(float top)
    {
        var separator = new UIHorizontalSeparator(2, true)
        {
            Color = new Color(85, 105, 150) * 0.65f,
            IgnoresMouseInteraction = true
        };
        separator.Width.Set(0f, 1f);
        separator.Top.Set(top, 0f);
        Append(separator);
    }
}
