using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OccidentMod.Gameplay.Classes;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     顯示職業初始能力數值與武器、裝備預覽選項。
///     Displays starting class stats and weapon/equipment preview options.
/// </summary>
/// <remarks>
///     面板只負責資料呈現及預覽開關的狀態管理。
///     職業初始能力由 ClassPreviewData 提供，
///     實際職業選擇由 ClassPlayer 管理。
///     This panel handles data presentation and preview toggle states.
///     Starting stats are provided by ClassPreviewData,
///     while ClassPlayer manages the actual class selection.
/// </remarks>
internal sealed class ClassStartingStatsPanel : UIPanel
{
    private const string LocalizationPrefix =
        "Mods.OccidentMod.UI.CharacterCreation.";

    private const float PanelPadding = 16f;

    private const float StatRowStart = 40f;
    private const float StatRowSpacing = 22f;
    private const float StatRowHeight = 20f;

    private const float PreviewOptionsTop = 266f;

    private static readonly Color PanelBackground =
        new Color(33, 43, 79) * 0.8f;

    private static readonly Color SeparatorColor =
        new Color(85, 105, 150) * 0.65f;

    private static readonly Color SecondaryTextColor =
        new(165, 181, 210);

    private static readonly Color StatValueColor =
        new(240, 229, 204);

    private static readonly Color WeaponPreviewColor =
        new(231, 190, 119);

    private static readonly Color EquipmentPreviewColor =
        new(139, 207, 211);

    private readonly ClassPreviewToggle equipmentPreview;
    private readonly Func<ClassCategory> getCategory;

    private readonly UIText heading;

    private readonly UIText[] labels =
        new UIText[ClassPreviewData.StatKeys.Length];

    private readonly Player player;
    private readonly UIText previewHeading;

    private readonly UIText[] values =
        new UIText[ClassPreviewData.StatKeys.Length];

    private readonly ClassPreviewToggle weaponPreview;

    private (ClassType Class, ClassCategory Category, string Culture)
        lastState;

    /// <summary>
    ///     建立職業初始能力資訊面板。
    ///     Creates the class starting stats panel.
    /// </summary>
    /// <param name="player">
    ///     角色建立介面的玩家物件。
    ///     Player instance used by character creation.
    /// </param>
    /// <param name="getCategory">
    ///     取得目前顯示的職業分類。
    ///     Gets the currently displayed class category.
    /// </param>
    public ClassStartingStatsPanel(
        Player player,
        Func<ClassCategory> getCategory)
    {
        this.player = player;
        this.getCategory = getCategory;

        ConfigurePanel();

        heading = CreateHeading();
        Append(heading);

        AddSeparator(28f);

        CreateStatRows();

        AddSeparator(230f);

        previewHeading = CreatePreviewHeading();
        Append(previewHeading);

        weaponPreview = CreatePreviewToggle(
            "PreviewWeapon",
            WeaponPreviewColor,
            0);

        equipmentPreview = CreatePreviewToggle(
            "PreviewEquipment",
            EquipmentPreviewColor,
            1);

        Append(weaponPreview);
        Append(equipmentPreview);

        PreviewOptions = new UIElement[]
        {
            weaponPreview,
            equipmentPreview
        };
    }

    /// <summary>
    ///     取得所有預覽選項元件，供外部導覽系統使用。
    ///     Gets preview option elements for external navigation.
    /// </summary>

    public IReadOnlyList<UIElement> PreviewOptions { get; }

    /// <summary>
    ///     是否啟用初始武器預覽。
    ///     Whether starting weapon preview is enabled.
    /// </summary>

    public bool PreviewWeaponEnabled => weaponPreview.Enabled;

    /// <summary>
    ///     是否啟用初始裝備預覽。
    ///     Whether starting equipment preview is enabled.
    /// </summary>

    public bool PreviewEquipmentEnabled => equipmentPreview.Enabled;

    /// <summary>
    ///     設定面板尺寸、背景與內距。
    ///     Configures panel dimensions, background, and padding.
    /// </summary>
    private void ConfigurePanel()
    {
        Width.Set(ClassInfoPanel.PanelWidth, 0f);
        Height.Set(ClassInfoPanel.PanelHeight, 0f);

        BackgroundColor = PanelBackground;

        SetPadding(PanelPadding);
    }

    /// <summary>
    ///     建立初始能力標題。
    ///     Creates the starting stats heading.
    /// </summary>
    private static UIText CreateHeading()
    {
        var text = new UIText(
            Language.GetText(LocalizationPrefix + "StartingStats"),
            0.9f);

        text.Top.Set(4f, 0f);

        return text;
    }

    /// <summary>
    ///     根據職業資料定義建立所有能力數值列。
    ///     Creates all stat rows from the class data definitions.
    /// </summary>
    private void CreateStatRows()
    {
        for (var i = 0; i < labels.Length; i++)
        {
            var row = CreateStatRow(i);

            labels[i] = CreateStatLabel(i);
            values[i] = CreateStatValue();

            row.Append(labels[i]);
            row.Append(values[i]);

            Append(row);
        }
    }

    /// <summary>
    ///     建立指定索引的能力數值容器。
    ///     Creates a stat row container at the specified index.
    /// </summary>
    private static UIElement CreateStatRow(int index)
    {
        var row = new UIElement();

        row.SetPadding(0f);

        row.Width.Set(0f, 1f);
        row.Height.Set(StatRowHeight, 0f);

        row.Top.Set(
            StatRowStart + index * StatRowSpacing,
            0f);

        return row;
    }

    /// <summary>
    ///     建立能力名稱文字。
    ///     Creates the stat name text.
    /// </summary>
    private static UIText CreateStatLabel(int index)
    {
        var key = ClassPreviewData.StatKeys[index];

        return new UIText(
            Language.GetText(LocalizationPrefix + "Stats." + key),
            0.9f);
    }

    /// <summary>
    ///     建立靠右對齊的能力數值文字。
    ///     Creates a right-aligned stat value text.
    /// </summary>
    private static UIText CreateStatValue()
    {
        return new UIText("0", 0.9f)
        {
            HAlign = 1f,
            TextColor = StatValueColor
        };
    }

    /// <summary>
    ///     建立預覽選項區域的標題。
    ///     Creates the preview options heading.
    /// </summary>
    private static UIText CreatePreviewHeading()
    {
        var text = new UIText(
            Language.GetText(LocalizationPrefix + "PreviewHeading"),
            0.8f)
        {
            TextColor = SecondaryTextColor
        };

        text.Top.Set(244f, 0f);

        return text;
    }

    /// <summary>
    ///     建立武器或裝備預覽開關。
    ///     Creates a weapon or equipment preview toggle.
    /// </summary>
    private static ClassPreviewToggle CreatePreviewToggle(
        string localizationKey,
        Color color,
        int index)
    {
        var toggle = new ClassPreviewToggle(
            LocalizationPrefix + localizationKey,
            color);

        toggle.Top.Set(PreviewOptionsTop, 0f);
        toggle.Width.Set(-4f, 0.5f);

        if (index == 1) toggle.Left.Set(4f, 0.5f);

        toggle.SetSnapPoint(
            "ClassPreviewOption",
            index);

        return toggle;
    }

    /// <summary>
    ///     建立面板內的水平分隔線。
    ///     Creates a horizontal separator inside the panel.
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
    ///     每次更新時檢查是否需要刷新職業資料。
    ///     Checks whether class information needs refreshing.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        RefreshContent();

        base.Update(gameTime);
    }

    /// <summary>
    ///     當職業、分類或語言變更時更新初始能力。
    ///     Updates starting stats when the class, category,
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

        var hasSelection =
            currentClass != ClassType.None &&
            ClassCatalog.GetCategory(currentClass) == currentCategory;

        var stats =
            ClassPreviewData.Get(currentClass).Stats;

        UpdateHeadings();
        UpdateStatValues(stats, hasSelection);

        // 立即重新計算靠右對齊的數值位置。
        // Recalculate right-aligned value positions immediately.
        Recalculate();
    }

    /// <summary>
    ///     更新面板標題的本地化內容。
    ///     Updates localized panel headings.
    /// </summary>
    private void UpdateHeadings()
    {
        heading.SetText(
            Language.GetText(LocalizationPrefix + "StartingStats"));

        previewHeading.SetText(
            Language.GetText(LocalizationPrefix + "PreviewHeading"));
    }

    /// <summary>
    ///     更新所有初始能力的名稱與數值。
    ///     Updates the names and values of all starting stats.
    /// </summary>
    private void UpdateStatValues(
        ClassStartingStats stats,
        bool hasSelection)
    {
        for (var i = 0; i < labels.Length; i++)
        {
            var key = ClassPreviewData.StatKeys[i];

            labels[i].SetText(
                Language.GetText(LocalizationPrefix + "Stats." + key));

            var value = hasSelection
                ? stats.GetValue(key)?.ToString() ?? "--"
                : "0";

            values[i].SetText(value);
        }
    }
}