// 顯示初始配點及預覽開關，數值來源是 ClassPreviewData。
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Occident.Common.Classes;
using Occident.Common.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Occident.Common.UI.CharacterCreation;

internal sealed class ClassStartingStatsPanel : UIPanel
{
    private const string Prefix = "Mods.OccidentMod.UI.CharacterCreation.";
    private readonly Player player;
    private readonly Func<ClassCategory> getCategory;
    private readonly UIText heading, previewHeading;
    private readonly UIText[] labels = new UIText[ClassPreviewData.StatKeys.Length];
    private readonly UIText[] values = new UIText[ClassPreviewData.StatKeys.Length];
    private readonly ClassPreviewToggle weaponPreview, equipmentPreview;
    private (ClassType Class, ClassCategory Category, string Culture) lastState;

    public IReadOnlyList<UIElement> PreviewOptions { get; }
    public bool PreviewWeaponEnabled => weaponPreview.Enabled;
    public bool PreviewEquipmentEnabled => equipmentPreview.Enabled;

    public ClassStartingStatsPanel(Player player, Func<ClassCategory> getCategory)
    {
        this.player = player;
        this.getCategory = getCategory;
        Width.Set(ClassInfoPanel.PanelWidth, 0f);
        Height.Set(ClassInfoPanel.PanelHeight, 0f);
        BackgroundColor = new Color(33, 43, 79) * 0.8f;
        SetPadding(16f);

        heading = new UIText(Language.GetText(Prefix + "StartingStats"), 0.9f);
        heading.Top.Set(4f, 0f);
        Append(heading);
        AddSeparator(28f);

        for (int i = 0; i < labels.Length; i++)
        {
            var row = new UIElement();
            row.SetPadding(0f);
            row.Width.Set(0f, 1f);
            row.Height.Set(20f, 0f);
            row.Top.Set(40f + i * 22f, 0f);
            Append(row);

            labels[i] = new UIText(Language.GetText(Prefix + "Stats." + ClassPreviewData.StatKeys[i]), 0.9f);
            row.Append(labels[i]);
            values[i] = new UIText("0", 0.9f)
            {
                HAlign = 1f,
                TextColor = new Color(240, 229, 204)
            };
            row.Append(values[i]);
        }

        AddSeparator(230f);
        previewHeading = new UIText(Language.GetText(Prefix + "PreviewHeading"), 0.8f)
        {
            TextColor = new Color(165, 181, 210)
        };
        previewHeading.Top.Set(244f, 0f);
        Append(previewHeading);

        weaponPreview = new ClassPreviewToggle(Prefix + "PreviewWeapon", new Color(231, 190, 119));
        weaponPreview.Top.Set(266f, 0f);
        weaponPreview.Width.Set(-4f, 0.5f);
        equipmentPreview = new ClassPreviewToggle(Prefix + "PreviewEquipment", new Color(139, 207, 211));
        equipmentPreview.Top.Set(266f, 0f);
        equipmentPreview.Left.Set(4f, 0.5f);
        equipmentPreview.Width.Set(-4f, 0.5f);
        weaponPreview.SetSnapPoint("ClassPreviewOption", 0);
        equipmentPreview.SetSnapPoint("ClassPreviewOption", 1);
        Append(weaponPreview);
        Append(equipmentPreview);
        PreviewOptions = new UIElement[] { weaponPreview, equipmentPreview };
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
        if (state == lastState)
            return;
        lastState = state;

        bool selected = current != ClassType.None && ClassCatalog.GetCategory(current) == category;
        ClassStartingStats stats = ClassPreviewData.Get(current).Stats;
        heading.SetText(Language.GetText(Prefix + "StartingStats"));
        previewHeading.SetText(Language.GetText(Prefix + "PreviewHeading"));
        for (int i = 0; i < labels.Length; i++)
        {
            string key = ClassPreviewData.StatKeys[i];
            labels[i].SetText(Language.GetText(Prefix + "Stats." + key));
            values[i].SetText(selected ? stats.GetValue(key)?.ToString() ?? "--" : "0");
        }
        // 立即更新靠右對齊的數值，第一次開啟面板時也需重新排版。
        Recalculate();
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
