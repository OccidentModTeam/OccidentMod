using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OccidentMod.Gameplay.Classes;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     管理角色建立介面的職業分類分頁。
///     Manages class category tabs in the character creation interface.
/// </summary>
/// <remarks>
///     保留原版分類按鈕的視覺設計，
///     並將分類設定、分頁建立與選取邏輯分離。
///     Preserves vanilla category button visuals while separating
///     category definitions, tab creation, and selection logic.
/// </remarks>
internal sealed class ClassCreationPage
{
    private const float TabSize = 44f;
    private const float TabSpacing = 48f;

    private readonly List<UIColoredImageButton> buttons = new();
    private readonly List<ClassCreationCategory> contents = new();

    /// <summary>
    ///     建立職業分類頁面。
    ///     Creates the class category page.
    /// </summary>
    public ClassCreationPage(
        UIElement appearanceHeader,
        Player player)
    {
        Header = CreateHeader(appearanceHeader);

        InitializeContent();

        RegisterDefaultCategories(player);

        AddSeparator();
    }

    /// <summary>
    ///     職業分類分頁的頂部容器。
    ///     Header container for class category tabs.
    /// </summary>

    public UIElement Header { get; }

    /// <summary>
    ///     目前職業分類的內容容器。
    ///     Content container for the selected class category.
    /// </summary>

    public UIElement Content { get; } = new();

    /// <summary>
    ///     取得所有職業分類按鈕。
    ///     Gets all class category buttons.
    /// </summary>

    public IReadOnlyList<UIColoredImageButton> Buttons => buttons;

    /// <summary>
    ///     取得目前分類中的職業選擇按鈕。
    ///     Gets class selection buttons in the active category.
    /// </summary>

    public IReadOnlyList<UIDifficultyButton> ClassButtons =>
        SelectedTab >= 0
            ? contents[SelectedTab].Buttons
            : Array.Empty<UIDifficultyButton>();

    /// <summary>
    ///     目前選取的分類分頁索引。
    ///     Gets the selected category tab index.
    /// </summary>

    public int SelectedTab { get; private set; } = -1;

    /// <summary>
    ///     目前選取的職業分類。
    ///     Gets the currently selected class category.
    /// </summary>

    public ClassCategory SelectedCategory =>
        SelectedTab >= 0
            ? contents[SelectedTab].Category
            : ClassCategory.None;

    /// <summary>
    ///     建立與原版外觀分類相同尺寸及位置的標頭。
    ///     Creates a header matching vanilla appearance category layout.
    /// </summary>
    private static UIElement CreateHeader(UIElement original)
    {
        var header = new UIElement
        {
            Width = original.Width,
            Height = original.Height,
            Left = original.Left,
            Top = original.Top,
            HAlign = original.HAlign,
            VAlign = original.VAlign
        };

        header.SetPadding(0f);

        header.PaddingTop = original.PaddingTop;
        header.PaddingBottom = original.PaddingBottom;
        header.PaddingLeft = original.PaddingLeft;
        header.PaddingRight = original.PaddingRight;

        return header;
    }

    /// <summary>
    ///     初始化職業分類內容容器。
    ///     Initializes the class category content container.
    /// </summary>
    private void InitializeContent()
    {
        Content.SetPadding(0f);
        Content.Width.Set(0f, 1f);
        Content.Height.Set(0f, 1f);
    }

    /// <summary>
    ///     註冊預設的四種職業分類。
    ///     Registers the four default class categories.
    /// </summary>
    private void RegisterDefaultCategories(Player player)
    {
        AddCategory(
            ClassCategory.Melee,
            ItemID.IronBroadsword,
            player);

        AddCategory(
            ClassCategory.Ranged,
            ItemID.WoodenBow,
            player);

        AddCategory(
            ClassCategory.Magic,
            ItemID.WandofSparking,
            player);

        AddCategory(
            ClassCategory.Summon,
            ItemID.BabyBirdStaff,
            player);
    }

    /// <summary>
    ///     註冊職業分類及其對應的圖示。
    ///     Registers a class category and its icon.
    /// </summary>
    private void AddCategory(
        ClassCategory category,
        int iconItemType,
        Player player)
    {
        var content = new ClassCreationCategory(
            player,
            category);

        AddTab(iconItemType, content);
    }

    /// <summary>
    ///     新增職業分類分頁。
    ///     Adds a class category tab.
    /// </summary>
    internal void AddTab(
        int itemType,
        ClassCreationCategory content)
    {
        var index = buttons.Count;

        Main.instance.LoadItem(itemType);

        var button = CreateTabButton(itemType, index);

        button.OnLeftClick += (_, _) =>
        {
            SelectTab(index);

            SoundEngine.PlaySound(SoundID.MenuTick);
        };

        ConfigureCategoryContent(content);

        buttons.Add(button);
        contents.Add(content);

        Header.Append(button);

        RefreshTabPositions();

        if (SelectedTab < 0)
            SelectTab(index);
        else
            RefreshSelectionColors();

        Header.Recalculate();
    }

    /// <summary>
    ///     建立使用原版物品圖示的分類按鈕。
    ///     Creates a category button using a vanilla item icon.
    /// </summary>
    private static UIColoredImageButton CreateTabButton(
        int itemType,
        int index)
    {
        var button = new UIColoredImageButton(
            TextureAssets.Item[itemType]);

        button.Top.Set(0f, 0f);
        button.Width.Set(TabSize, 0f);
        button.Height.Set(TabSize, 0f);

        button.SetSnapPoint(
            "ClassCreationTab",
            index);

        return button;
    }

    /// <summary>
    ///     設定分類內容的尺寸。
    ///     Configures the dimensions of category content.
    /// </summary>
    private static void ConfigureCategoryContent(
        ClassCreationCategory content)
    {
        content.Width.Set(0f, 1f);
        content.Height.Set(0f, 1f);
    }

    /// <summary>
    ///     重新計算分類按鈕的水平排列。
    ///     Recalculates horizontal positions of category buttons.
    /// </summary>
    private void RefreshTabPositions()
    {
        var startOffset =
            -buttons.Count * TabSpacing / 2f;

        for (var i = 0; i < buttons.Count; i++)
            buttons[i].Left.Set(
                startOffset + i * TabSpacing,
                0.5f);
    }

    /// <summary>
    ///     切換目前顯示的職業分類。
    ///     Switches the currently displayed class category.
    /// </summary>
    private void SelectTab(int index)
    {
        if (index < 0 || index >= contents.Count)
            return;

        SelectedTab = index;

        Content.RemoveAllChildren();
        Content.Append(contents[index]);

        RefreshSelectionColors();

        Content.Recalculate();
    }

    /// <summary>
    ///     更新分類按鈕的選取狀態。
    ///     Updates the selected state of category buttons.
    /// </summary>
    private void RefreshSelectionColors()
    {
        for (var i = 0; i < buttons.Count; i++)
            buttons[i].SetSelected(
                i == SelectedTab);
    }

    /// <summary>
    ///     建立與原版角色建立頁面一致的分隔線。
    ///     Creates a separator matching vanilla character creation UI.
    /// </summary>
    private void AddSeparator()
    {
        var separator = new UIHorizontalSeparator
        {
            HAlign = 0.5f,
            VAlign = 1f,

            Color = Color.Lerp(
                Color.White,
                new Color(63, 65, 151, 255),
                0.85f) * 0.9f,

            IgnoresMouseInteraction = true
        };

        separator.Width.Set(-20f, 1f);
        separator.Top.Set(6f, 0f);

        Header.Append(separator);
    }
}