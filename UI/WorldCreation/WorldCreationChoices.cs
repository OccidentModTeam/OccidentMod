#nullable enable
using System;
using System.Collections.Generic;
using OccidentMod.World;
using OccidentMod.World.Generation;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;

namespace OccidentMod.UI.WorldCreation;

/// <summary>
///     管理自訂世界大小、難度分頁與選取狀態。
///     Manages custom world size, difficulty tabs,
///     and selection states.
/// </summary>
/// <remarks>
///     自訂選項透過 WorldCreationAccess 與原版 UI 同步。
///     Custom options are synchronized with vanilla UI
///     through WorldCreationAccess.
/// </remarks>
internal sealed class WorldCreationChoices
{
    private const string LocalizationPrefix =
        "Mods.OccidentMod.UI.WorldCreation.";

    private const float ButtonHeight = 34f;
    private const float ButtonSpacing = -3f;

    private readonly List<NativeWorldOptionButton>
        customButtons = new();

    private readonly UIWorldCreation page;

    /// <summary>
    ///     目前選取的模組世界設定。
    ///     Currently selected mod world options.
    /// </summary>
    public WorldOptions Options;

    private UIText? caption;

    private UIElement difficultyContent = null!;

    private (
        WorldOptions Options,
        int VanillaDifficulty,
        string Culture
        )? lastCaption;

    private bool modPage;

    private WorldDifficultyPreview? preview;

    private UIElement[] vanillaDifficulty = Array.Empty<UIElement>();

    /// <summary>
    ///     建立世界選項管理器。
    ///     Creates the world creation options controller.
    /// </summary>
    public WorldCreationChoices(UIWorldCreation page)
    {
        this.page = page;
    }

    /// <summary>
    ///     包含難度分頁及選項的介面容器。
    ///     UI container for difficulty tabs and options.
    /// </summary>

    public UIElement DifficultyRow { get; private set; } = null!;

    /// <summary>
    ///     取得世界建立介面的本地化文字。
    ///     Gets localized text for the world creation interface.
    /// </summary>
    internal static string Text(string key)
    {
        return Language.GetTextValue(
            LocalizationPrefix + key);
    }

    /// <summary>
    ///     調整原版世界大小按鈕，並加入超大型世界選項。
    ///     Adjusts vanilla world size buttons and adds
    ///     the extra-large world option.
    /// </summary>
    public void AddSize(UIElement parent, float top)
    {
        if (!ExtraLargeWorldSizeSystem.FeatureEnabled)
            return;

        UIElement[] native =
            WorldCreationAccess.Buttons(page, true);

        for (var i = 0; i < native.Length; i++)
        {
            native[i].Width.Set(
                ButtonSpacing,
                0.25f);

            native[i].HAlign = i / 3f;
        }

        var extra = CreateButton(
            "ExtraLarge",
            () => SelectExtraLarge(native),
            () => Options.ExtraLarge);

        extra.Width.Set(ButtonSpacing, 0.25f);
        extra.Height.Set(ButtonHeight, 0f);
        extra.Top.Set(top, 0f);

        extra.HAlign = 1f;

        extra.SetSnapPoint("size", 3);

        extra.OnMouseOver += (_, _) =>
            WorldCreationAccess.Describe(
                page,
                Text("ExtraLargeHelp"));

        extra.OnMouseOut += (_, _) =>
            DescribeSelection();

        parent.Append(extra);
    }

    /// <summary>
    ///     選取超大型世界，並同步原版大型世界選項。
    ///     Selects the extra-large world option while
    ///     synchronizing the vanilla large world setting.
    /// </summary>
    private void SelectExtraLarge(UIElement[] native)
    {
        WorldCreationAccess.Click(
            page,
            true,
            native[2]);

        Options = Options with
        {
            ExtraLarge = true
        };

        Refresh();
        DescribeSelection();
    }

    /// <summary>
    ///     建立原版與模組世界難度分頁。
    ///     Creates vanilla and mod difficulty tabs.
    /// </summary>
    public void AddDifficulty(UIElement parent, float top)
    {
        vanillaDifficulty =
            WorldCreationAccess.Buttons(page, false);

        foreach (var button in vanillaDifficulty) button.Remove();

        CreateDifficultyRow(parent, top);
        CreateDifficultyCaption();

        AddTab("Vanilla", false, -200f);
        AddTab("ModTab", true, -98f);

        CreateDifficultyContent();
        CreateCustomDifficultyButtons();

        ShowPage();
    }

    /// <summary>
    ///     建立難度選項的主要容器。
    ///     Creates the main difficulty options container.
    /// </summary>
    private void CreateDifficultyRow(
        UIElement parent,
        float top)
    {
        DifficultyRow = new UIElement();

        DifficultyRow.SetPadding(0f);

        DifficultyRow.Width.Set(0f, 1f);
        DifficultyRow.Height.Set(80f, 0f);
        DifficultyRow.Top.Set(top, 0f);

        parent.Append(DifficultyRow);
    }

    /// <summary>
    ///     建立目前選取難度的說明標題。
    ///     Creates the currently selected difficulty caption.
    /// </summary>
    private void CreateDifficultyCaption()
    {
        caption = new UIText("", 0.75f)
        {
            IgnoresMouseInteraction = true
        };

        caption.Top.Set(5f, 0f);

        DifficultyRow.Append(caption);
    }

    /// <summary>
    ///     建立難度按鈕的內容容器。
    ///     Creates the difficulty button content container.
    /// </summary>
    private void CreateDifficultyContent()
    {
        difficultyContent = new UIElement();

        difficultyContent.SetPadding(0f);

        difficultyContent.Top.Set(40f, 0f);
        difficultyContent.Width.Set(0f, 1f);
        difficultyContent.Height.Set(40f, 0f);

        DifficultyRow.Append(difficultyContent);
    }

    /// <summary>
    ///     建立三個自訂世界難度選項。
    ///     Creates the three custom world difficulty options.
    /// </summary>
    private void CreateCustomDifficultyButtons()
    {
        for (var i = 1; i <= 3; i++)
        {
            var difficulty =
                (OccidentWorldDifficulty)i;

            var button = CreateButton(
                difficulty.ToString(),
                () => SelectCustomDifficulty(difficulty),
                () => Options.Difficulty == difficulty);

            button.Width.Set(
                ButtonSpacing,
                1f / 3f);

            button.Height.Set(ButtonHeight, 0f);

            button.HAlign = (i - 1) / 2f;

            button.SetSnapPoint(
                "difficulty",
                i - 1);

            button.OnMouseOver += (_, _) =>
                WorldCreationAccess.Describe(
                    page,
                    Text(difficulty + "Description"));

            button.OnMouseOut += (_, _) =>
                DescribeSelection();

            customButtons.Add(button);
        }
    }

    /// <summary>
    ///     選取模組難度，並將原版難度保持為 Master。
    ///     Selects a mod difficulty while keeping the
    ///     underlying vanilla difficulty set to Master.
    /// </summary>
    private void SelectCustomDifficulty(
        OccidentWorldDifficulty difficulty)
    {
        // 不將自訂列舉直接傳入原版世界生成器。
        // Never pass the custom difficulty enum
        // directly to the vanilla world generator.

        WorldCreationAccess.Click(
            page,
            false,
            vanillaDifficulty[3]);

        Options = Options with
        {
            Difficulty = difficulty
        };

        Refresh();
        DescribeSelection();
    }

    /// <summary>
    ///     加入原版或模組難度分頁按鈕。
    ///     Adds a vanilla or mod difficulty tab button.
    /// </summary>
    private void AddTab(
        string text,
        bool isMod,
        float left)
    {
        var tab = CreateButton(
            text,
            () => SwitchPage(isMod),
            () => modPage == isMod);

        tab.Width.Set(98f, 0f);

        // 保留原版選取框與九宮格邊角需要的高度。
        // Preserve the height required by vanilla
        // selection borders and nine-slice corners.

        tab.Height.Set(ButtonHeight, 0f);
        tab.Left.Set(left, 1f);

        DifficultyRow.Append(tab);
    }

    /// <summary>
    ///     切換目前顯示的難度分頁。
    ///     Switches the currently displayed difficulty tab.
    /// </summary>
    private void SwitchPage(bool isMod)
    {
        modPage = isMod;

        ShowPage();
    }

    /// <summary>
    ///     顯示目前分頁的難度選項。
    ///     Displays the difficulty options for the active tab.
    /// </summary>
    private void ShowPage()
    {
        difficultyContent.RemoveAllChildren();

        if (modPage)
            foreach (var button in customButtons)
                difficultyContent.Append(button);
        else
            foreach (var button in vanillaDifficulty)
            {
                button.Top.Set(0f, 0f);

                difficultyContent.Append(button);
            }

        Refresh();

        DifficultyRow.Recalculate();
    }

    /// <summary>
    ///     同步難度預覽、標題與原版按鈕高亮狀態。
    ///     Synchronizes the difficulty preview, caption,
    ///     and vanilla button highlight states.
    /// </summary>
    public void Refresh()
    {
        if (caption is null)
            return;

        preview ??= new WorldDifficultyPreview(
            WorldCreationAccess.Preview(page));

        preview.SetDifficulty(Options.Difficulty);

        var vanillaDifficulty =
            WorldCreationAccess.Selected(page, false);

        var state = (
            Options,
            VanillaDifficulty: vanillaDifficulty,
            Culture: Language.ActiveCulture.Name);

        if (lastCaption != state)
        {
            caption.SetText(
                Text("SelectedDifficulty") +
                ResolveDifficultyName(vanillaDifficulty));

            lastCaption = state;
        }

        // 自訂難度啟用時不顯示原版難度選取框。
        // Hide vanilla difficulty highlights
        // when a custom difficulty is active.

        if (Options.Difficulty != OccidentWorldDifficulty.None)
            WorldCreationAccess.ClearHighlight(
                page,
                false);

        // 超大型世界使用原版 Large 生成，
        // 但不應同時高亮 Large 按鈕。
        //
        // Extra-large worlds use vanilla Large generation,
        // but the Large button should not remain highlighted.

        if (Options.ExtraLarge)
            WorldCreationAccess.ClearHighlight(
                page,
                true);
    }

    /// <summary>
    ///     取得目前選取難度的顯示名稱。
    ///     Resolves the display name of the selected difficulty.
    /// </summary>
    private string ResolveDifficultyName(int vanillaDifficulty)
    {
        if (Options.Difficulty != OccidentWorldDifficulty.None) return Text(Options.Difficulty.ToString());

        var key = vanillaDifficulty switch
        {
            1 => "UI.Expert",
            2 => "UI.Master",
            3 => "UI.Creative",
            _ => "UI.Normal"
        };

        return Language.GetTextValue(key);
    }

    /// <summary>
    ///     更新原版世界建立介面的選項說明文字。
    ///     Updates the option description in the
    ///     vanilla world creation interface.
    /// </summary>
    public void DescribeSelection()
    {
        string description;

        if (Options.Difficulty != OccidentWorldDifficulty.None)
            description = Text(
                Options.Difficulty + "Description");
        else if (Options.ExtraLarge)
            description = Text("ExtraLargeHelp");
        else
            description = string.Empty;

        WorldCreationAccess.Describe(
            page,
            description);
    }

    /// <summary>
    ///     建立具有點擊回呼及選取狀態的原生風格按鈕。
    ///     Creates a native-style button with a click callback
    ///     and selection state provider.
    /// </summary>
    private static NativeWorldOptionButton CreateButton(
        string text,
        Action click,
        Func<bool> selected)
    {
        return new NativeWorldOptionButton(
            text,
            click,
            selected);
    }
}