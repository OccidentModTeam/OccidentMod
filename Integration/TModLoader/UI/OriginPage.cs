
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using OccidentMod.Integration.TModLoader.Players;

namespace OccidentMod.Integration.TModLoader.UI;

/// <summary>
///
/// 在原版角色创建界面新增「选择起源」分页。
/// 扩展页面高度并提供可滚动的职业选择内容。
/// 显示职业介绍、潜能充能方式、起源箴言及初始武器。
/// 所有显示文字均使用语言包索引。
///
/// Adds an origin selection tab to vanilla character creation.
/// Expands the layout and provides scrollable class selection.
/// Displays class information, potential charging methods,
/// origin mottos, and starting weapon choices.
/// All visible text is retrieved from localization keys.
///
/// </summary>

public sealed class OriginPage : ModSystem
{
    private const string LocalizationRoot =
        "Mods.OccidentMod.UI.OriginPage.";

    private const string ClassLocalizationRoot =
        "Mods.OccidentMod.Classes.";

    private const float MaximumExtraHeight = 220f;

    private static readonly string[] ClassIds =
    {
        "Warrior",
        "Ranger",
        "Mage",
        "Summoner",
        "Rogue",
        "Thrower",
        "Bard",
        "Artificer"
    };

    private delegate void OrigBuildPage(
        UICharacterCreation page);

    private delegate void BuildPageHook(
        OrigBuildPage orig,
        UICharacterCreation page);

    public override void Load()
    {
        if (Main.dedServ)
            return;

        var method = typeof(UICharacterCreation).GetMethod(
            "BuildPage",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (method is null)
        {
            Mod.Logger.Warn(
                "UICharacterCreation.BuildPage was not found.");

            return;
        }

        MonoModHooks.Add(
            method,
            (BuildPageHook)OnBuildPage);
    }

    private void OnBuildPage(
        OrigBuildPage orig,
        UICharacterCreation page)
    {
        orig(page);

        try
        {
            InsertOriginPage(page);
        }
        catch (Exception exception)
        {
            Mod.Logger.Error(
                "Failed to initialize origin selection.",
                exception);
        }
    }

    private void InsertOriginPage(UICharacterCreation page)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.NonPublic;

        var type = typeof(UICharacterCreation);

        var topField = type.GetField(
            "_topContainer",
            flags);

        var middleField = type.GetField(
            "_middleContainer",
            flags);

        var playerField = type.GetField(
            "_player",
            flags);

        var unselectMethod = type.GetMethod(
            "UnselectAllCategories",
            flags);

        if (topField?.GetValue(page) is not UIElement top ||
            middleField?.GetValue(page) is not UIElement middle ||
            playerField?.GetValue(page) is not Player player ||
            unselectMethod is null)
        {
            Mod.Logger.Warn(
                "Character creation members were not found.");

            return;
        }

        var vanillaButtons = top.Children
            .OfType<UIColoredImageButton>()
            .OrderBy(button => button.Left.Pixels)
            .ToArray();

        if (vanillaButtons.Length < 2)
        {
            Mod.Logger.Warn(
                "Vanilla category buttons were not found.");

            return;
        }

        var originButton = new UIColoredImageButton(
            TextureAssets.Reforge[1]);

        originButton.CopyStyle(vanillaButtons[0]);

        float start = vanillaButtons[0].Left.Pixels;
        float end = vanillaButtons[^1].Left.Pixels;

        float spacing =
            (end - start) / vanillaButtons.Length;

        if (spacing <= 0f)
        {
            Mod.Logger.Warn(
                "Invalid character creation tab spacing.");

            return;
        }

        for (int i = 0; i < vanillaButtons.Length; i++)
        {
            vanillaButtons[i].Left.Set(
                start + (i + 1) * spacing,
                vanillaButtons[i].Left.Percent);
        }

        originButton.Left.Set(
            start,
            vanillaButtons[0].Left.Percent);

        var originPanel = CreateOriginPanel(player, out var contentList);

        var persistentChildren = new HashSet<UIElement>();
        var layoutHeights = new List<(UIElement Element, StyleDimension Height)>();

        for (UIElement? element = middle; element is not null && element != page; element = element.Parent)
        {
            if (element.Height.Percent == 0f && element.Height.Pixels > 0f)
                layoutHeights.Add((element, element.Height));
        }

        float appliedExtraHeight = 0f;

        void ResizeOriginPage()
        {
            if (layoutHeights.Count == 0)
                return;

            var outer = layoutHeights[^1].Element.GetDimensions();
            float availableHeight = page.GetInnerDimensions().Height - (outer.Y + outer.Height) +
                appliedExtraHeight - 12f;
            float vanillaListHeight = contentList.GetInnerDimensions().Height - appliedExtraHeight;
            float requiredHeight = MathF.Ceiling(contentList.GetTotalHeight() - vanillaListHeight);
            float extraHeight = MathHelper.Clamp(
                Math.Min(requiredHeight, availableHeight),
                0f,
                MaximumExtraHeight);

            if (Math.Abs(extraHeight - appliedExtraHeight) < 0.5f)
                return;

            appliedExtraHeight = extraHeight;
            foreach (var (element, height) in layoutHeights)
                element.Height.Set(height.Pixels + extraHeight, height.Percent);

            page.Recalculate();
        }

        void OpenOriginPage()
        {
            unselectMethod.Invoke(page, null);

            if (originPanel.Parent is not null)
                originPanel.Remove();

            persistentChildren.Clear();

            foreach (var child in middle.Children)
                persistentChildren.Add(child);

            middle.Append(originPanel);

            foreach (var button in vanillaButtons)
                button.SetSelected(false);

            originButton.SetSelected(true);

            page.Recalculate();
            ResizeOriginPage();
        }

        void CloseOriginPage()
        {
            if (originPanel.Parent is null)
                return;

            originPanel.Remove();
            originButton.SetSelected(false);

            foreach (var (element, height) in layoutHeights)
                element.Height = height;

            appliedExtraHeight = 0f;
            page.Recalculate();
        }

        // 同时处理鼠标按下与点击，兼容原版分类事件。
        // Handle vanilla category switching.

        foreach (var button in vanillaButtons)
        {
            button.OnLeftMouseDown += (_, _) =>
            {
                CloseOriginPage();
            };

            button.OnLeftClick += (_, _) =>
            {
                CloseOriginPage();
            };
        }

        originButton.OnLeftClick += (_, _) =>
        {
            OpenOriginPage();
        };

        // 检测通过键盘或手柄切换产生的新内容。
        // Detect category changes triggered without mouse clicks.

        page.OnUpdate += _ =>
        {
            if (originPanel.Parent is null)
                return;

            bool vanillaContentOpened = middle.Children.Any(
                child =>
                    !ReferenceEquals(child, originPanel) &&
                    !persistentChildren.Contains(child));

            if (vanillaContentOpened)
                CloseOriginPage();
            else
                ResizeOriginPage();
        };

        top.Append(originButton);

        OpenOriginPage();
    }

    private static UIElement CreateOriginPanel(Player player, out UIList contentList)
    {
        var panel = new UIElement();

        panel.Width.Set(0f, 1f);
        panel.Height.Set(0f, 1f);
        panel.SetPadding(0f);

        var classPlayer =
            player.GetModPlayer<OccidentModPlayer>();

        var weaponPlayer =
            player.GetModPlayer<OriginWeaponChoicePlayer>();

        // 可滚动内容区。
        // Scrollable content area.

        var list = new UIList
        {
            ListPadding = 6f,
            ManualSortMethod = _ => { }
        };

        list.Left.Set(8f, 0f);
        list.Width.Set(-16f, 1f);
        list.Height.Set(0f, 1f);

        // Keep wheel navigation in short windows without drawing a scrollbar.
        list.SetScrollbar(new UIScrollbar());

        panel.Append(list);

        int selectedClassIndex = Array.IndexOf(ClassIds, classPlayer.SelectedClassId);
        var classPicker = new OriginClassPicker(selectedClassIndex);
        var classButtons = new UIPanel[ClassIds.Length];

        for (int i = 0; i < ClassIds.Length; i++)
        {
            string classId = ClassIds[i];

            var button = new UIPanel();

            button.SetPadding(0f);

            var texture = ModContent.Request<Texture2D>(
                $"OccidentMod/Assets/UI/OriginPage/{classId}",
                ReLogic.Content.AssetRequestMode.ImmediateLoad);

            var icon = new UIImage(texture)
            {
                HAlign = 0.5f,
                RemoveFloatingPointsFromDrawPosition = true,
                IgnoresMouseInteraction = true
            };

            icon.Top.Set(5f, 0f);

            button.Append(icon);

            var name = new UIText(
                ClassName(classId),
                0.65f)
            {
                HAlign = 0.5f,
                IgnoresMouseInteraction = true
            };

            name.Top.Set(39f, 0f);

            button.Append(name);

            AddCardHover(button);
            classButtons[i] = button;
            classPicker.AddCard(button, i);
        }

        var selectedName = CreateText(Text("NotSelected"), 0.82f, 0f, 18f);
        var roleText = CreateText(Text("SelectHint"), 0.66f, 0f, 14f);
        var descriptionText = CreateText(Text("DescriptionHint"), 0.67f, 0f, 14f);
        var chargeTitle = CreateText(Text("ChargeTitle"), 0.70f, 0f, 15f);
        var chargeText = CreateText(Text("PotentialHint"), 0.66f, 0f, 14f);
        var mottoTitle = CreateText(Text("MottoTitle"), 0.70f, 0f, 15f);
        var mottoText = CreateText(string.Empty, 0.66f, 0f, 14f);

        roleText.TextColor = new Color(183, 197, 219);
        chargeTitle.TextColor = mottoTitle.TextColor = SectionAccent;
        mottoText.TextColor = new Color(208, 215, 232);

        var information = new OriginDetails(
            new[] { selectedName, roleText, descriptionText },
            new[] { chargeTitle, chargeText, mottoTitle, mottoText });

        // 初始武器标题。
        // Starting weapon section heading.

        var weaponHeading = new UIElement();

        weaponHeading.Width.Set(0f, 1f);
        weaponHeading.Height.Set(37f, 0f);

        var weaponTitle = CreateText(
            Text("WeaponTitle"),
            0.78f,
            0f,
            18f);

        weaponTitle.TextColor = SectionAccent;
        weaponHeading.Append(weaponTitle);

        var weaponHint = CreateText(
            Text("WeaponHint"),
            0.64f,
            19f,
            17f);

        weaponHeading.Append(weaponHint);

        // 初始武器二选一卡片。
        // Two starting weapon selection cards.

        var weaponArea = new UIElement();

        weaponArea.Width.Set(0f, 1f);
        weaponArea.Height.Set(48f, 0f);

        var weaponButtons = new UIPanel[2];

        for (int i = 0; i < weaponButtons.Length; i++)
        {
            int choice = i;

            var button = new UIPanel();

            button.Width.Set(-6f, 0.5f);
            button.Height.Set(48f, 0f);

            button.Left.Set(
                3f,
                i * 0.5f);

            button.SetPadding(0f);

            var name = CreateText(
                Text(
                    i == 0
                        ? "WeaponOne"
                        : "WeaponTwo"),
                0.72f,
                6f,
                16f);

            button.Append(name);

            var description = CreateText(
                Text("WeaponUnassigned"),
                0.63f,
                25f,
                16f);
            description.TextColor = new Color(
                185,
                195,
                215);

            button.Append(description);

            button.OnLeftClick += (_, _) =>
            {
                if (!IsKnownClass(classPlayer.SelectedClassId))
                    return;

                weaponPlayer.SetChoice(choice);

                RefreshSelection(
                    classPlayer,
                    weaponPlayer,
                    classButtons,
                    weaponButtons,
                    selectedName,
                    roleText,
                    descriptionText,
                    chargeText,
                    mottoText);

                list.Recalculate();
            };

            AddCardHover(button);
            weaponButtons[i] = button;
            weaponArea.Append(button);
        }

        // 注册职业选择事件。
        // Register class selection events.

        for (int i = 0; i < classButtons.Length; i++)
        {
            string classId = ClassIds[i];

            classButtons[i].OnLeftClick += (_, _) =>
            {
                if (classPlayer.SelectedClassId != classId)
                {
                    classPlayer.SetClass(classId);
                    weaponPlayer.SetChoice(-1);
                }

                RefreshSelection(
                    classPlayer,
                    weaponPlayer,
                    classButtons,
                    weaponButtons,
                    selectedName,
                    roleText,
                    descriptionText,
                    chargeText,
                    mottoText);

                list.Recalculate();
            };
        }

        list.AddRange(
            new[]
            {
                classPicker,
                weaponHeading,
                weaponArea,
                information
            });

        RefreshSelection(
            classPlayer,
            weaponPlayer,
            classButtons,
            weaponButtons,
            selectedName,
            roleText,
            descriptionText,
            chargeText,
            mottoText);

        contentList = list;
        return panel;
    }

    private static UIText CreateText(
        string value,
        float scale,
        float top,
        float height)
    {
        var text = new UIText(value, scale)
        {
            TextOriginX = 0f,
            WrappedTextBottomPadding = 0f,
            IgnoresMouseInteraction = true
        };

        text.Width.Set(-20f, 1f);
        text.Height.Set(height, 0f);
        text.Left.Set(10f, 0f);
        text.Top.Set(top, 0f);

        text.IsWrapped = true;

        return text;
    }

    private static void RefreshSelection(
        OccidentModPlayer classPlayer,
        OriginWeaponChoicePlayer weaponPlayer,
        UIPanel[] classButtons,
        UIPanel[] weaponButtons,
        UIText selectedName,
        UIText roleText,
        UIText descriptionText,
        UIText chargeText,
        UIText mottoText)
    {
        int selectedIndex = Array.IndexOf(
            ClassIds,
            classPlayer.SelectedClassId);

        selectedName.TextColor = selectedIndex >= 0 ? new Color(255, 122, 80) : Color.White;

        // 更新职业图标卡片。
        // Refresh class selection cards.

        for (int i = 0; i < classButtons.Length; i++)
        {
            bool selected = i == selectedIndex;

            SetCardSelection(
                classButtons[i],
                selected);
        }

        // 分开读取潜能充能方式与起源箴言。
        // Load charging methods and origin mottos separately.

        if (selectedIndex >= 0)
        {
            string classId = ClassIds[selectedIndex];

            selectedName.SetText(
                ClassName(classId));

            roleText.SetText(
                ClassText(classId, "Role"));

            descriptionText.SetText(
                ClassText(classId, "Description"));

            chargeText.SetText(
                ClassText(classId, "Charge"));

            mottoText.SetText(
                ClassText(classId, "Motto"));
        }
        else
        {
            selectedName.SetText(
                Text("NotSelected"));

            roleText.SetText(
                Text("SelectHint"));

            descriptionText.SetText(
                Text("DescriptionHint"));

            chargeText.SetText(
                Text("PotentialHint"));

            mottoText.SetText(string.Empty);
        }

        // 更新初始武器选择卡片。
        // Refresh starting weapon selection cards.

        for (int i = 0; i < weaponButtons.Length; i++)
        {
            bool selected =
                selectedIndex >= 0 &&
                weaponPlayer.SelectedChoice == i;

            SetCardSelection(
                weaponButtons[i],
                selected);
        }
    }

    internal static readonly Color SectionAccent = new(228, 206, 162);
    internal static readonly Color CardBorder = new(54, 64, 95);

    internal static void AddCardHover(UIPanel card)
    {
        card.OnMouseOver += (_, _) =>
        {
            if (card.BorderColor == CardBorder)
                card.BorderColor = new Color(115, 134, 175);
        };

        card.OnMouseOut += (_, _) =>
        {
            if (card.BorderColor != SectionAccent)
                card.BorderColor = CardBorder;
        };
    }

    private static void SetCardSelection(
        UIPanel card,
        bool selected)
    {
        card.BackgroundColor = selected
            ? new Color(57, 77, 117)
            : new Color(36, 45, 74);

        card.BorderColor = selected
            ? SectionAccent
            : CardBorder;
    }

    private static bool IsKnownClass(string classId)
    {
        return Array.IndexOf(
            ClassIds,
            classId) >= 0;
    }

    private static string ClassName(string classId)
    {
        return Language.GetTextValue(
            ClassLocalizationRoot +
            classId +
            ".DisplayName");
    }

    private static string ClassText(
        string classId,
        string key)
    {
        return Language.GetTextValue(
            LocalizationRoot +
            "Classes." +
            classId +
            "." +
            key);
    }

    private static string Text(string key)
    {
        return Language.GetTextValue(
            LocalizationRoot + key);
    }
}
