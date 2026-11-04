// 管理角色建立的外觀步驟、職業步驟及兩側資訊面板。
using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Occident.Common.Classes;
using Occident.Common.Players;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Occident.Common.UI.CharacterCreation;

// 沿用原版建立頁面，從命名返回時保留角色草稿與目前步驟。
internal sealed class CharacterCreationSteps
{
    private readonly UICharacterCreation page;
    private readonly Player player;
    private readonly UIElement root, middle, top;
    private readonly UITextPanel<LocalizedText> back, next;
    private readonly StyleDimension originalWidth;
    private readonly StyleDimension originalLeft;
    private readonly ClassCreationPage classPage;
    private readonly ClassInfoToggleButton infoButton;
    private readonly ClassInfoPanel infoPanel;
    private readonly ClassStartingStatsPanel leftInfoPanel;
    private bool infoOpen;
    private UIElement[] appearanceContent = Array.Empty<UIElement>();
    private (bool ClassStep, bool InfoOpen, string Culture, string Caption) lastCaption;

    public bool IsClassStep { get; private set; }
    internal Player PreviewPlayer => player;
    internal bool WeaponPreviewEnabled => IsClassStep && leftInfoPanel.PreviewWeaponEnabled &&
        ClassCatalog.GetCategory(player.GetModPlayer<ClassPlayer>().CurrentClass) == classPage.SelectedCategory;

    public CharacterCreationSteps(UICharacterCreation page)
    {
        this.page = page;
        player = Field<Player>(page, "_player");
        middle = Field<UIElement>(page, "_middleContainer");
        top = Field<UIElement>(page, "_topContainer");
        classPage = new ClassCreationPage(top, player);
        root = middle.Parent.Parent;
        originalWidth = root.Width;
        originalLeft = root.Left;
        var buttons = root.Children.OfType<UITextPanel<LocalizedText>>().ToArray();
        back = buttons.Single(button => button.HAlign == 0f);
        next = buttons.Single(button => button.HAlign == 1f);
        CasualCreationOptions.Add(page, player, middle, root);
        infoPanel = new ClassInfoPanel(player, () => classPage.SelectedCategory);
        infoPanel.Top.Set(root.Top.Pixels + middle.Parent.Top.Pixels, root.Top.Percent + middle.Parent.Top.Percent);
        leftInfoPanel = new ClassStartingStatsPanel(player, () => classPage.SelectedCategory)
        {
            Width = infoPanel.Width,
            Height = infoPanel.Height,
            Top = infoPanel.Top,
            BackgroundColor = infoPanel.BackgroundColor,
            BorderColor = infoPanel.BorderColor
        };
        infoButton = new ClassInfoToggleButton(() => infoOpen, ToggleInfo)
        {
            Top = next.Top
        };
        next.OnUpdate += _ => RefreshButtons();
        RefreshButtons();
    }

    // 暫存外觀元件再換成職業頁；返回時沿用同一批元件與玩家草稿。
    public void ShowClassStep()
    {
        if (IsClassStep)
            return;
        appearanceContent = middle.Children.ToArray();
        middle.RemoveAllChildren();
        top.Remove();
        middle.Parent.Append(classPage.Header);
        middle.Append(classPage.Content);
        IsClassStep = true;
        root.Append(infoButton);
        RefreshButtons();
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    public void ShowAppearanceStep()
    {
        if (!IsClassStep)
            return;
        middle.RemoveAllChildren();
        foreach (var element in appearanceContent)
            middle.Append(element);
        classPage.Header.Remove();
        middle.Parent.Append(top);
        IsClassStep = false;
        infoOpen = false;
        infoPanel.Remove();
        leftInfoPanel.Remove();
        infoButton.Remove();
        RefreshButtons();
        SoundEngine.PlaySound(SoundID.MenuClose);
    }

    // 只在步驟、側欄或語言改變時更新文字與尺寸，避免每刻重排。
    private void RefreshButtons()
    {
        bool hasClass = player.GetModPlayer<ClassPlayer>().CurrentClass != ClassType.None;
        string caption = !IsClassStep ? "Mods.OccidentMod.UI.CharacterCreation.Next"
            : hasClass ? "UI.Create" : "Mods.OccidentMod.UI.CharacterCreation.ContinueWithoutClass";
        var signature = (IsClassStep, infoOpen, Language.ActiveCulture.Name, caption);
        if (signature == lastCaption)
            return;
        lastCaption = signature;

        back.SetText(Language.GetText(IsClassStep
            ? "Mods.OccidentMod.UI.CharacterCreation.Previous" : "UI.Back"), 0.7f, true);
        next.SetText(Language.GetText(caption), 0.7f, true);

        // 維持原版大字按鈕，依文字寬度擴張頁面，避免縮小標題。
        float rightWidth = Math.Max(240f,
            FontAssets.DeathText.Value.MeasureString(Language.GetTextValue(caption)).X * 0.7f + 36f);
        float leftWidth = Math.Max(240f,
            FontAssets.DeathText.Value.MeasureString(back.Text).X * 0.7f + 36f);
        const float infoSpace = 62f;
        float width = IsClassStep ? Math.Max(originalWidth.Pixels, leftWidth + 20f + rightWidth + infoSpace)
            : originalWidth.Pixels;
        root.Width.Set(width, originalWidth.Percent);
        // 建立頁面保持原位，資訊面板展開在兩側。
        root.Left.Set(originalLeft.Pixels, originalLeft.Percent);
        // 側欄掛在全螢幕頁面上，讓主容器範圍外的按鈕仍可接收輸入。
        infoPanel.Left.Set(width * 0.5f + root.Left.Pixels + ClassInfoPanel.PanelGap, 0.5f + originalLeft.Percent);
        leftInfoPanel.Left.Set(-width * 0.5f + root.Left.Pixels - ClassInfoPanel.PanelGap - ClassInfoPanel.PanelWidth,
            0.5f + originalLeft.Percent);
        if (IsClassStep)
        {
            back.Width.Set(leftWidth, 0f);
            next.Width.Set(width - leftWidth - 20f - infoSpace, 0f);
            next.Left.Set(-infoSpace, 0f);
        }
        else
        {
            back.Width.Set(-10f, 0.5f);
            next.Width.Set(-10f, 0.5f);
            next.Left.Set(0f, 0f);
        }
        page.Recalculate();
    }

    private void ToggleInfo()
    {
        if (!IsClassStep)
            return;
        infoOpen = !infoOpen;
        if (infoOpen)
        {
            page.Append(infoPanel);
            page.Append(leftInfoPanel);
        }
        else
        {
            infoPanel.Remove();
            leftInfoPanel.Remove();
        }
        RefreshButtons();
        if (infoOpen)
        {
            infoPanel.RefreshContent();
            leftInfoPanel.RefreshContent();
        }
        SoundEngine.PlaySound(infoOpen ? SoundID.MenuOpen : SoundID.MenuClose);
    }

    public void SetupGamepad()
    {
        // 第二步使用自己的職業分頁，需重新連接手把導覽，不沿用外觀分類的連結。
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 1;
        var left = UILinkPointNavigator.Points[3000];
        var right = UILinkPointNavigator.Points[3001];
        left.Unlink();
        right.Unlink();
        left.Right = right.ID;
        right.Left = left.ID;
        UILinkPointNavigator.SetPosition(left.ID, back.GetDimensions().Center() * Main.UIScale);
        UILinkPointNavigator.SetPosition(right.ID, next.GetDimensions().Center() * Main.UIScale);
        var info = UILinkPointNavigator.Points[3010];
        info.Unlink();
        info.Left = right.ID;
        right.Right = info.ID;
        UILinkPointNavigator.SetPosition(info.ID, infoButton.GetDimensions().Center() * Main.UIScale);
        for (int i = 0; i < classPage.Buttons.Count; i++)
        {
            var point = UILinkPointNavigator.Points[3002 + i];
            point.Unlink();
            point.Down = classPage.ClassButtons.Count > 0 ? 3020 : left.ID;
            if (i > 0)
                point.Left = point.ID - 1;
            if (i + 1 < classPage.Buttons.Count)
                point.Right = point.ID + 1;
            UILinkPointNavigator.SetPosition(point.ID, classPage.Buttons[i].GetDimensions().Center() * Main.UIScale);
        }
        for (int i = 0; i < classPage.ClassButtons.Count; i++)
        {
            var point = UILinkPointNavigator.Points[3020 + i];
            point.Unlink();
            point.Up = i == 0 ? 3002 + classPage.SelectedTab : point.ID - 1;
            point.Down = i + 1 < classPage.ClassButtons.Count ? point.ID + 1 : left.ID;
            point.Right = right.ID;
            UILinkPointNavigator.SetPosition(point.ID, classPage.ClassButtons[i].GetDimensions().Center() * Main.UIScale);
        }
        left.Up = right.Up = classPage.ClassButtons.Count > 0
            ? 3020 + classPage.ClassButtons.Count - 1 : 3002 + classPage.SelectedTab;
        info.Up = right.Up;
        if (infoOpen)
        {
            left.Left = 3030;
            for (int i = 0; i < leftInfoPanel.PreviewOptions.Count; i++)
            {
                var option = UILinkPointNavigator.Points[3030 + i];
                option.Unlink();
                option.Up = 3002;
                option.Down = left.ID;
                if (i > 0)
                    option.Left = option.ID - 1;
                option.Right = i + 1 < leftInfoPanel.PreviewOptions.Count ? option.ID + 1 : left.ID;
                UILinkPointNavigator.SetPosition(option.ID,
                    leftInfoPanel.PreviewOptions[i].GetDimensions().Center() * Main.UIScale);
            }
        }
        int current = UILinkPointNavigator.CurrentPoint;
        bool onHeaderOrActions = current >= 3000 && current < 3002 + classPage.Buttons.Count;
        bool onClass = current >= 3020 && current < 3020 + classPage.ClassButtons.Count;
        bool onPreview = infoOpen && current >= 3030 && current < 3030 + leftInfoPanel.PreviewOptions.Count;
        if (!onHeaderOrActions && !onClass && !onPreview && current != info.ID)
            UILinkPointNavigator.ChangePoint(left.ID);
    }

    private static T Field<T>(UICharacterCreation page, string name) =>
        (T)(typeof(UICharacterCreation).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(page) ?? throw new MissingFieldException(typeof(UICharacterCreation).FullName, name));
}
