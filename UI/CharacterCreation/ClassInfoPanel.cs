using System;
using Microsoft.Xna.Framework;
using OccidentMod.Gameplay.Classes;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     顯示角色建立介面的職業資訊、能力雷達圖與操作難度。
///     Displays class information, ability ratings, and difficulty
///     in the character creation interface.
/// </summary>
/// <remarks>
///     此類別僅負責職業資訊面板的排版及資料呈現。
///     職業評分由 ClassRatings 提供，
///     難度資訊由 ClassPreviewData 提供，
///     實際玩家職業由 ClassPlayer 管理。
///     This class only handles layout and presentation.
///     ClassRatings provides ability ratings,
///     ClassPreviewData provides difficulty information,
///     and ClassPlayer manages the actual player class.
/// </remarks>
internal sealed class ClassInfoPanel : UIPanel
{
    public const float PanelWidth = 300f;
    public const float PanelHeight = 360f;
    public const float PanelGap = 12f;

    private const float PanelPadding = 16f;

    private const string LocalizationRoot =
        "Mods.OccidentMod.";

    private const string TextPrefix =
        LocalizationRoot + "UI.CharacterCreation.";

    private static readonly Color PanelBackground =
        new Color(33, 43, 79) * 0.8f;

    private static readonly Color SecondaryTextColor =
        new(165, 181, 210);

    private static readonly Color SeparatorColor =
        new Color(85, 105, 150) * 0.65f;

    private readonly UIText categoryText;
    private readonly ClassRadarChart chart;

    private readonly ClassDifficultyText difficultyText;
    private readonly Func<ClassCategory> getCategory;

    private readonly Player player;
    private readonly UIText ratingsTitle;

    private (ClassType Class, ClassCategory Category, string Culture)
        lastState;

    /// <summary>
    ///     建立職業資訊面板。
    ///     Creates the class information panel.
    /// </summary>
    /// <param name="player">
    ///     角色建立介面使用的玩家物件。
    ///     Player instance used by the character creation interface.
    /// </param>
    /// <param name="getCategory">
    ///     取得目前顯示的職業分類。
    ///     Provides the currently displayed class category.
    /// </param>
    public ClassInfoPanel(
        Player player,
        Func<ClassCategory> getCategory)
    {
        this.player = player;
        this.getCategory = getCategory;

        ConfigurePanel();

        categoryText = CreateCategoryText();
        Append(categoryText);

        AddSeparator(28f);

        ratingsTitle = CreateRatingsTitle();
        Append(ratingsTitle);

        chart = CreateRadarChart();
        Append(chart);

        difficultyText = CreateDifficultyText();
        Append(difficultyText);

        AddFooterSeparator();
    }

    /// <summary>
    ///     設定面板的原始尺寸、位置與背景顏色。
    ///     Configures the original panel dimensions, position,
    ///     and background color.
    /// </summary>
    private void ConfigurePanel()
    {
        Width.Set(PanelWidth, 0f);
        Height.Set(PanelHeight, 0f);

        Left.Set(PanelGap, 1f);

        BackgroundColor = PanelBackground;

        SetPadding(PanelPadding);
    }

    /// <summary>
    ///     建立目前職業分類的標題文字。
    ///     Creates the current class category heading.
    /// </summary>
    private static UIText CreateCategoryText()
    {
        var text = new UIText(string.Empty, 0.9f);

        text.Top.Set(4f, 0f);

        return text;
    }

    /// <summary>
    ///     建立能力評分標題。
    ///     Creates the ability ratings heading.
    /// </summary>
    private static UIText CreateRatingsTitle()
    {
        var text = new UIText(
            Language.GetText(TextPrefix + "RatingsTitle"),
            0.8f)
        {
            TextColor = SecondaryTextColor
        };

        text.Top.Set(42f, 0f);

        return text;
    }

    /// <summary>
    ///     建立職業能力雷達圖。
    ///     Creates the class ability radar chart.
    /// </summary>
    private static ClassRadarChart CreateRadarChart()
    {
        var radarChart = new ClassRadarChart();

        radarChart.Top.Set(62f, 0f);
        radarChart.Height.Set(-138f, 1f);

        return radarChart;
    }

    /// <summary>
    ///     建立職業操作難度文字。
    ///     Creates the class difficulty text element.
    /// </summary>
    private static ClassDifficultyText CreateDifficultyText()
    {
        var text = new ClassDifficultyText
        {
            VAlign = 1f
        };

        text.Top.Set(-6f, 0f);

        return text;
    }

    /// <summary>
    ///     建立面板底部的分隔線。
    ///     Creates the footer separator.
    /// </summary>
    private void AddFooterSeparator()
    {
        var separator = new UIHorizontalSeparator
        {
            VAlign = 1f,
            Color = SeparatorColor,
            IgnoresMouseInteraction = true
        };

        separator.Width.Set(0f, 1f);
        separator.Top.Set(-44f, 0f);

        Append(separator);
    }

    /// <summary>
    ///     建立一般水平分隔線。
    ///     Creates a standard horizontal separator.
    /// </summary>
    private void AddSeparator(float top)
    {
        var separator = new UIHorizontalSeparator
        {
            Color = SeparatorColor,
            IgnoresMouseInteraction = true
        };

        separator.Width.Set(0f, 1f);
        separator.Top.Set(top, 0f);

        Append(separator);
    }

    /// <summary>
    ///     每次更新時檢查職業、分類及語言是否變更。
    ///     Checks for class, category, and language changes
    ///     during each update.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        RefreshContent();

        base.Update(gameTime);
    }

    /// <summary>
    ///     當職業、分類或語言變更時刷新面板內容。
    ///     Refreshes panel content when the class, category,
    ///     or language changes.
    /// </summary>
    public void RefreshContent()
    {
        var currentClass =
            player.GetModPlayer<ClassPlayer>().CurrentClass;

        var currentCategory = getCategory();

        var state = (
            Class: currentClass,
            Category: currentCategory,
            Culture: Language.ActiveCulture.Name);

        if (lastState == state)
            return;

        lastState = state;

        UpdateCategoryText(currentCategory);
        UpdateRatingsTitle();

        var hasSelection =
            currentClass != ClassType.None &&
            ClassCatalog.GetCategory(currentClass) == currentCategory;

        var displayedClass = hasSelection
            ? currentClass
            : ClassType.None;

        UpdateRadarChart(displayedClass, hasSelection);
        UpdateDifficulty(displayedClass, hasSelection);

        // 本地化文字變更後重新計算排版。
        // Recalculate layout after localized text changes.
        Recalculate();
    }

    /// <summary>
    ///     更新目前職業分類的本地化名稱。
    ///     Updates the localized class category name.
    /// </summary>
    private void UpdateCategoryText(ClassCategory category)
    {
        var categoryName = Language.GetTextValue(
            $"{LocalizationRoot}ClassCategories.{category}");

        var heading = Language.GetTextValue(
            TextPrefix + "ClassType",
            categoryName);

        categoryText.SetText(heading);
    }

    /// <summary>
    ///     更新能力評分標題的本地化內容。
    ///     Updates the localized ability ratings heading.
    /// </summary>
    private void UpdateRatingsTitle()
    {
        ratingsTitle.SetText(
            Language.GetText(TextPrefix + "RatingsTitle"));
    }

    /// <summary>
    ///     更新雷達圖的職業選取狀態及能力評分。
    ///     Updates radar chart selection state and ability ratings.
    /// </summary>
    private void UpdateRadarChart(
        ClassType displayedClass,
        bool hasSelection)
    {
        chart.HasSelection = hasSelection;

        chart.SetRatings(
            ClassRatings.Get(displayedClass));
    }

    /// <summary>
    ///     更新職業操作難度的名稱及等級。
    ///     Updates the class difficulty name and numeric level.
    /// </summary>
    private void UpdateDifficulty(
        ClassType displayedClass,
        bool hasSelection)
    {
        var difficultyName = hasSelection
            ? ClassPreviewData.GetDifficultyName(displayedClass)
            : "NoSelection";

        var localizedLevel = Language.GetTextValue(
            TextPrefix + "DifficultyLevels." + difficultyName);

        var difficultyLevel = hasSelection
            ? ClassPreviewData.GetDifficulty(displayedClass)
            : 0;

        difficultyText.SetDifficulty(
            localizedLevel,
            difficultyLevel);
    }
}