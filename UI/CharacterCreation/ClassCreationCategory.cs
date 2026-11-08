using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using OccidentMod.Gameplay.Classes;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     管理單一職業分類的職業清單、選擇狀態與說明內容。
///     Manages the class list, selection state, and description
///     for a single class category.
/// </summary>
/// <remarks>
///     職業資料由 ClassCatalog 提供，實際選擇結果儲存在 ClassPlayer。
///     UI 僅負責呈現及轉發玩家操作。
///     Class data is provided by ClassCatalog, while the actual
///     selection is stored in ClassPlayer.
///     The UI only presents data and forwards user interactions.
/// </remarks>
internal sealed class ClassCreationCategory : UIElement
{
    private const string LocalizationRoot =
        "Mods.OccidentMod.";

    private const string PromptKey =
        LocalizationRoot + "UI.CharacterCreation.ChooseClass";

    private const float ButtonHeight = 28f;
    private const float ButtonSpacing = 32f;

    private readonly List<UIDifficultyButton> buttons = new();

    private readonly UIText description;
    private readonly UIList descriptionList;

    private readonly Player player;
    private readonly UIScrollbar scrollbar;

    // 僅用於原版難度按鈕的選取視覺狀態。
    // Used only for vanilla difficulty button selection visuals.
    private readonly Player selection = new();

    private (ClassType Class, string Culture) lastState;

    /// <summary>
    ///     建立職業分類內容頁面。
    ///     Creates the class category content page.
    /// </summary>
    public ClassCreationCategory(
        Player player,
        ClassCategory category)
    {
        this.player = player;
        Category = category;

        SetPadding(10f);
        PaddingTop = 0f;
        PaddingBottom = 0f;

        var classes = GetClasses(category);

        var list = CreateClassListContainer(classes.Length);
        var details = CreateDescriptionContainer();

        Append(list);
        Append(details);

        descriptionList = CreateDescriptionList(details);

        scrollbar = new UIScrollbar();
        descriptionList.SetScrollbar(scrollbar);

        description = CreateDescriptionText();
        descriptionList.Add(description);

        CreateClassButtons(list, classes);

        RefreshSelection();
    }

    /// <summary>
    ///     取得此分類中的職業按鈕。
    ///     Gets the class buttons in this category.
    /// </summary>

    public IReadOnlyList<UIDifficultyButton> Buttons => buttons;

    /// <summary>
    ///     取得此頁面對應的職業分類。
    ///     Gets the class category represented by this page.
    /// </summary>

    public ClassCategory Category { get; }

    /// <summary>
    ///     從職業目錄取得指定分類的所有職業。
    ///     Gets all classes belonging to the specified category.
    /// </summary>
    private static ClassType[] GetClasses(ClassCategory category)
    {
        return Enum.GetValues<ClassType>()
            .Where(id =>
                ClassCatalog.IsValid(id) &&
                ClassCatalog.GetCategory(id) == category)
            .ToArray();
    }

    /// <summary>
    ///     建立左側職業清單容器。
    ///     Creates the class list container on the left.
    /// </summary>
    private static UIElement CreateClassListContainer(int count)
    {
        var list = new UIElement();

        list.SetPadding(0f);

        list.Width.Set(-4f, 0.4f);
        list.Height.Set(
            Math.Max(0f, count * ButtonSpacing - 4f),
            0f);

        list.VAlign = 0.5f;

        return list;
    }

    /// <summary>
    ///     建立右側職業說明容器。
    ///     Creates the class description container on the right.
    /// </summary>
    private static UIElement CreateDescriptionContainer()
    {
        var details = new UIElement();

        details.SetPadding(0f);

        details.Left.Set(4f, 0.4f);
        details.Width.Set(-4f, 0.6f);

        // 保留原本的 11 像素頂部內縮。
        // Preserve the original 11-pixel top inset.
        details.Top.Set(11f, 0f);
        details.Height.Set(-11f, 1f);

        return details;
    }

    /// <summary>
    ///     建立職業說明的可捲動清單。
    ///     Creates the scrollable class description list.
    /// </summary>
    private static UIList CreateDescriptionList(UIElement parent)
    {
        var list = new UIList();

        list.Left.Set(8f, 0f);
        list.Top.Set(8f, 0f);

        list.Width.Set(-16f, 1f);
        list.Height.Set(-16f, 1f);

        list.SetPadding(0f);

        parent.Append(list);

        return list;
    }

    /// <summary>
    ///     建立支援自動換行的職業說明文字。
    ///     Creates wrapped class description text.
    /// </summary>
    private static UIText CreateDescriptionText()
    {
        var text = new UIText(
            Language.GetText(PromptKey))
        {
            IsWrapped = true,

            // 為中文字符保留上下邊界。
            // Reserve vertical space for CJK glyphs.
            PaddingTop = 8f,
            PaddingBottom = 4f
        };

        text.Width.Set(0f, 1f);

        return text;
    }

    /// <summary>
    ///     根據職業目錄建立所有職業選擇按鈕。
    ///     Creates all class selection buttons from the catalog.
    /// </summary>
    private void CreateClassButtons(
        UIElement container,
        IReadOnlyList<ClassType> classes)
    {
        foreach (var classType in classes)
        {
            var button = CreateClassButton(classType);

            buttons.Add(button);
            container.Append(button);
        }
    }

    /// <summary>
    ///     建立單一職業選擇按鈕。
    ///     Creates a single class selection button.
    /// </summary>
    private ClassNameButton CreateClassButton(ClassType classType)
    {
        var prefix = GetClassLocalizationPrefix(classType);

        var button = new ClassNameButton(
            selection,
            Language.GetText(prefix + "DisplayName"),
            Language.GetText(prefix + "Description"),
            (byte)classType);

        var index = buttons.Count;

        button.Width.Set(0f, 1f);
        button.Height.Set(ButtonHeight, 0f);

        button.Top.Set(index * ButtonSpacing, 0f);

        button.SetSnapPoint(
            "ClassCreationChoice",
            index);

        button.OnLeftMouseDown += (_, _) =>
            ToggleClassSelection(classType);

        return button;
    }

    /// <summary>
    ///     切換玩家選擇的職業，再更新介面狀態。
    ///     Toggles the player's selected class and refreshes the UI.
    /// </summary>
    private void ToggleClassSelection(ClassType classType)
    {
        var classPlayer = player.GetModPlayer<ClassPlayer>();

        var nextClass =
            classPlayer.CurrentClass == classType
                ? ClassType.None
                : classType;

        classPlayer.EditClass(nextClass);

        RefreshSelection();
    }

    /// <summary>
    ///     更新職業選擇狀態。
    ///     Updates the class selection state.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        RefreshSelection();

        base.Update(gameTime);
    }

    /// <summary>
    ///     同步職業按鈕的視覺狀態與職業說明。
    ///     Synchronizes button visuals and class description.
    /// </summary>
    private void RefreshSelection()
    {
        var current =
            player.GetModPlayer<ClassPlayer>().CurrentClass;

        // 沿用原版難度按鈕的選取效果。
        // Preserve vanilla difficulty button selection visuals.
        selection.difficulty = (byte)current;

        var state = (
            Class: current,
            Culture: Language.ActiveCulture.Name);

        if (lastState == state)
            return;

        lastState = state;

        UpdateDescription(current);
    }

    /// <summary>
    ///     根據目前職業更新右側說明。
    ///     Updates the description for the current class.
    /// </summary>
    private void UpdateDescription(ClassType current)
    {
        var localizationKey =
            current != ClassType.None &&
            ClassCatalog.GetCategory(current) == Category
                ? GetClassLocalizationPrefix(current) + "Description"
                : PromptKey;

        description.SetText(Language.GetText(localizationKey));

        descriptionList.Recalculate();

        // 職業變更後回到說明頂部。
        // Reset the description scroll position after selection changes.
        scrollbar.ViewPosition = 0f;
    }

    /// <summary>
    ///     取得指定職業的本地化路徑。
    ///     Gets the localization prefix for the specified class.
    /// </summary>
    private static string GetClassLocalizationPrefix(ClassType classType)
    {
        return $"{LocalizationRoot}Classes.{classType}.";
    }
}