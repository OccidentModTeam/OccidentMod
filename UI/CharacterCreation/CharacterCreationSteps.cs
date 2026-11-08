using System;
using System.Linq;
using System.Reflection;
using OccidentMod.Gameplay.Classes;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     管理角色建立的外觀步驟、職業步驟與資訊面板。
///     Manages appearance, class selection, and information panels.
/// </summary>
/// <remarks>
///     保留原版角色建立頁面及玩家草稿，
///     切換步驟時不重新建立角色資料。
///     Preserves the vanilla character creation page and player draft
///     without recreating player data when switching steps.
/// </remarks>
internal sealed class CharacterCreationSteps
{
    private const string TextPrefix =
        "Mods.OccidentMod.UI.CharacterCreation.";

    private const float MinimumButtonWidth = 240f;
    private const float ButtonSpacing = 20f;
    private const float InfoButtonSpace = 62f;

    private const int BackPointId = 3000;
    private const int NextPointId = 3001;
    private const int CategoryPointStart = 3002;
    private const int ClassPointStart = 3020;
    private const int PreviewPointStart = 3030;
    private const int InfoPointId = 3010;

    private static readonly BindingFlags ReflectionFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly UITextPanel<LocalizedText> back;

    private readonly ClassCreationPage classPage;
    private readonly ClassInfoToggleButton infoButton;
    private readonly ClassInfoPanel infoPanel;
    private readonly ClassStartingStatsPanel leftInfoPanel;
    private readonly UIElement middle;
    private readonly UITextPanel<LocalizedText> next;
    private readonly StyleDimension originalLeft;

    private readonly StyleDimension originalWidth;

    private readonly UICharacterCreation page;

    private readonly UIElement root;
    private readonly UIElement top;

    private UIElement[] appearanceContent = Array.Empty<UIElement>();

    private bool infoOpen;

    private (
        bool ClassStep,
        bool InfoOpen,
        string Culture,
        string Caption
        ) lastCaption;

    /// <summary>
    ///     建立角色建立步驟控制器。
    ///     Creates the character creation step controller.
    /// </summary>
    public CharacterCreationSteps(UICharacterCreation page)
    {
        this.page = page;

        PreviewPlayer = GetVanillaField<Player>(page, "_player");
        middle = GetVanillaField<UIElement>(page, "_middleContainer");
        top = GetVanillaField<UIElement>(page, "_topContainer");

        root = middle.Parent.Parent;

        originalWidth = root.Width;
        originalLeft = root.Left;

        var buttons = root.Children
            .OfType<UITextPanel<LocalizedText>>()
            .ToArray();

        back = buttons.Single(button => button.HAlign == 0f);
        next = buttons.Single(button => button.HAlign == 1f);

        classPage = new ClassCreationPage(top, PreviewPlayer);

        InitializeAppearanceOptions();

        infoPanel = new ClassInfoPanel(
            PreviewPlayer,
            () => classPage.SelectedCategory);

        infoPanel.Top.Set(
            root.Top.Pixels + middle.Parent.Top.Pixels,
            root.Top.Percent + middle.Parent.Top.Percent);

        leftInfoPanel = new ClassStartingStatsPanel(
            PreviewPlayer,
            () => classPage.SelectedCategory)
        {
            Width = infoPanel.Width,
            Height = infoPanel.Height,
            Top = infoPanel.Top,
            BackgroundColor = infoPanel.BackgroundColor,
            BorderColor = infoPanel.BorderColor
        };

        infoButton = new ClassInfoToggleButton(
            () => infoOpen,
            ToggleInfo)
        {
            Top = next.Top
        };

        next.OnUpdate += _ => RefreshButtons();

        RefreshButtons();
    }

    /// <summary>
    ///     是否正在顯示職業選擇步驟。
    ///     Whether the class selection step is active.
    /// </summary>

    public bool IsClassStep { get; private set; }

    /// <summary>
    ///     取得角色建立頁面使用的玩家草稿。
    ///     Gets the player draft used by character creation.
    /// </summary>

    internal Player PreviewPlayer { get; }

    /// <summary>
    ///     是否允許顯示目前職業的起始武器。
    ///     Whether the selected class weapon can be previewed.
    /// </summary>

    internal bool WeaponPreviewEnabled =>
        IsClassStep &&
        leftInfoPanel.PreviewWeaponEnabled &&
        ClassCatalog.GetCategory(GetSelectedClass()) ==
        classPage.SelectedCategory;

    /// <summary>
    ///     初始化外觀建立頁面的額外選項。
    ///     Initializes additional appearance creation options.
    /// </summary>
    private void InitializeAppearanceOptions()
    {
        CasualCreationOptions.Add(
            page,
            PreviewPlayer,
            middle,
            root);
    }

    /// <summary>
    ///     取得玩家目前選擇的職業。
    ///     Gets the currently selected player class.
    /// </summary>
    private ClassType GetSelectedClass()
    {
        return PreviewPlayer.GetModPlayer<ClassPlayer>().CurrentClass;
    }

    /// <summary>
    ///     從外觀建立步驟進入職業選擇步驟。
    ///     Switches from appearance creation to class selection.
    /// </summary>
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

    /// <summary>
    ///     返回角色外觀建立步驟。
    ///     Returns to the appearance creation step.
    /// </summary>
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

        CloseInfoPanels();

        infoButton.Remove();

        RefreshButtons();

        SoundEngine.PlaySound(SoundID.MenuClose);
    }

    /// <summary>
    ///     更新返回與繼續按鈕的文字及排版。
    ///     Updates navigation button text and layout.
    /// </summary>
    private void RefreshButtons()
    {
        var hasClass = GetSelectedClass() != ClassType.None;

        var caption = !IsClassStep
            ? TextPrefix + "Next"
            : hasClass
                ? "UI.Create"
                : TextPrefix + "ContinueWithoutClass";

        var signature = (
            IsClassStep,
            infoOpen: infoOpen,
            Language.ActiveCulture.Name,
            caption);

        if (signature == lastCaption)
            return;

        lastCaption = signature;

        var backKey = IsClassStep
            ? TextPrefix + "Previous"
            : "UI.Back";

        back.SetText(Language.GetText(backKey), 0.7f, true);
        next.SetText(Language.GetText(caption), 0.7f, true);

        RefreshNavigationLayout(caption);

        page.Recalculate();
    }

    /// <summary>
    ///     依照目前步驟調整導覽按鈕及資訊面板位置。
    ///     Updates navigation button and information panel layout.
    /// </summary>
    private void RefreshNavigationLayout(string caption)
    {
        var rightWidth = Math.Max(
            MinimumButtonWidth,
            FontAssets.DeathText.Value
                .MeasureString(Language.GetTextValue(caption)).X
            * 0.7f + 36f);

        var leftWidth = Math.Max(
            MinimumButtonWidth,
            FontAssets.DeathText.Value
                .MeasureString(back.Text).X
            * 0.7f + 36f);

        var width = IsClassStep
            ? Math.Max(
                originalWidth.Pixels,
                leftWidth + ButtonSpacing +
                rightWidth + InfoButtonSpace)
            : originalWidth.Pixels;

        root.Width.Set(width, originalWidth.Percent);
        root.Left.Set(originalLeft.Pixels, originalLeft.Percent);

        UpdateInfoPanelPositions(width);

        if (IsClassStep)
        {
            back.Width.Set(leftWidth, 0f);

            next.Width.Set(
                width - leftWidth -
                ButtonSpacing - InfoButtonSpace,
                0f);

            next.Left.Set(-InfoButtonSpace, 0f);
        }
        else
        {
            back.Width.Set(-10f, 0.5f);
            next.Width.Set(-10f, 0.5f);
            next.Left.Set(0f, 0f);
        }
    }

    /// <summary>
    ///     將資訊面板定位於角色建立介面的左右兩側。
    ///     Positions information panels beside the creation interface.
    /// </summary>
    private void UpdateInfoPanelPositions(float width)
    {
        var centerOffset = width * 0.5f + root.Left.Pixels;
        var centerPercent = 0.5f + originalLeft.Percent;

        infoPanel.Left.Set(
            centerOffset + ClassInfoPanel.PanelGap,
            centerPercent);

        leftInfoPanel.Left.Set(
            -centerOffset -
            ClassInfoPanel.PanelGap -
            ClassInfoPanel.PanelWidth +
            root.Left.Pixels * 2f,
            centerPercent);
    }

    /// <summary>
    ///     切換職業資訊面板的顯示狀態。
    ///     Toggles the class information panels.
    /// </summary>
    private void ToggleInfo()
    {
        if (!IsClassStep)
            return;

        infoOpen = !infoOpen;

        if (infoOpen)
        {
            page.Append(infoPanel);
            page.Append(leftInfoPanel);

            infoPanel.RefreshContent();
            leftInfoPanel.RefreshContent();
        }
        else
        {
            infoPanel.Remove();
            leftInfoPanel.Remove();
        }

        RefreshButtons();

        SoundEngine.PlaySound(
            infoOpen ? SoundID.MenuOpen : SoundID.MenuClose);
    }

    /// <summary>
    ///     關閉職業資訊面板並重設開啟狀態。
    ///     Closes information panels and resets their visibility state.
    /// </summary>
    private void CloseInfoPanels()
    {
        infoOpen = false;

        infoPanel.Remove();
        leftInfoPanel.Remove();
    }

    /// <summary>
    ///     設定職業選擇步驟的手把導覽。
    ///     Configures gamepad navigation for class selection.
    /// </summary>
    public void SetupGamepad()
    {
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 1;

        SetupNavigationActions();
        SetupCategoryNavigation();
        SetupClassNavigation();

        if (infoOpen)
            SetupPreviewNavigation();

        ValidateCurrentNavigationPoint();
    }

    /// <summary>
    ///     建立返回、繼續與資訊按鈕的手把連結。
    ///     Links back, continue, and information buttons.
    /// </summary>
    private void SetupNavigationActions()
    {
        var left = UILinkPointNavigator.Points[BackPointId];
        var right = UILinkPointNavigator.Points[NextPointId];
        var info = UILinkPointNavigator.Points[InfoPointId];

        left.Unlink();
        right.Unlink();
        info.Unlink();

        left.Right = right.ID;
        right.Left = left.ID;

        info.Left = right.ID;
        right.Right = info.ID;

        SetNavigationPosition(left.ID, back);
        SetNavigationPosition(right.ID, next);
        SetNavigationPosition(info.ID, infoButton);
    }

    /// <summary>
    ///     建立職業分類分頁的手把連結。
    ///     Links class category tabs for gamepad navigation.
    /// </summary>
    private void SetupCategoryNavigation()
    {
        var classCount = classPage.ClassButtons.Count;

        for (var i = 0; i < classPage.Buttons.Count; i++)
        {
            var point =
                UILinkPointNavigator.Points[CategoryPointStart + i];

            point.Unlink();

            point.Down = classCount > 0
                ? ClassPointStart
                : BackPointId;

            if (i > 0)
                point.Left = point.ID - 1;

            if (i + 1 < classPage.Buttons.Count)
                point.Right = point.ID + 1;

            SetNavigationPosition(
                point.ID,
                classPage.Buttons[i]);
        }
    }

    /// <summary>
    ///     建立職業清單與底部按鈕的手把連結。
    ///     Links class choices and bottom navigation buttons.
    /// </summary>
    private void SetupClassNavigation()
    {
        var count = classPage.ClassButtons.Count;

        for (var i = 0; i < count; i++)
        {
            var point =
                UILinkPointNavigator.Points[ClassPointStart + i];

            point.Unlink();

            point.Up = i == 0
                ? CategoryPointStart + classPage.SelectedTab
                : point.ID - 1;

            point.Down = i + 1 < count
                ? point.ID + 1
                : BackPointId;

            point.Right = NextPointId;

            SetNavigationPosition(
                point.ID,
                classPage.ClassButtons[i]);
        }

        var lastPoint = count > 0
            ? ClassPointStart + count - 1
            : CategoryPointStart + classPage.SelectedTab;

        var left = UILinkPointNavigator.Points[BackPointId];
        var right = UILinkPointNavigator.Points[NextPointId];
        var info = UILinkPointNavigator.Points[InfoPointId];

        left.Up = lastPoint;
        right.Up = lastPoint;
        info.Up = lastPoint;
    }

    /// <summary>
    ///     建立預覽選項的手把連結。
    ///     Links preview options for gamepad navigation.
    /// </summary>
    private void SetupPreviewNavigation()
    {
        var left = UILinkPointNavigator.Points[BackPointId];

        left.Left = PreviewPointStart;

        var count = leftInfoPanel.PreviewOptions.Count;

        for (var i = 0; i < count; i++)
        {
            var point =
                UILinkPointNavigator.Points[PreviewPointStart + i];

            point.Unlink();

            point.Up = CategoryPointStart;
            point.Down = BackPointId;

            if (i > 0)
                point.Left = point.ID - 1;

            point.Right = i + 1 < count
                ? point.ID + 1
                : BackPointId;

            SetNavigationPosition(
                point.ID,
                leftInfoPanel.PreviewOptions[i]);
        }
    }

    /// <summary>
    ///     確保目前手把焦點位於有效的介面元件。
    ///     Ensures the current gamepad focus is valid.
    /// </summary>
    private void ValidateCurrentNavigationPoint()
    {
        var current = UILinkPointNavigator.CurrentPoint;

        var onHeaderOrActions =
            current >= BackPointId &&
            current < CategoryPointStart + classPage.Buttons.Count;

        var onClass =
            current >= ClassPointStart &&
            current < ClassPointStart + classPage.ClassButtons.Count;

        var onPreview =
            infoOpen &&
            current >= PreviewPointStart &&
            current < PreviewPointStart +
            leftInfoPanel.PreviewOptions.Count;

        if (!onHeaderOrActions &&
            !onClass &&
            !onPreview &&
            current != InfoPointId)
            UILinkPointNavigator.ChangePoint(BackPointId);
    }

    /// <summary>
    ///     更新手把導覽點的位置。
    ///     Updates a gamepad navigation point position.
    /// </summary>
    private static void SetNavigationPosition(
        int pointId,
        UIElement element)
    {
        UILinkPointNavigator.SetPosition(
            pointId,
            element.GetDimensions().Center() * Main.UIScale);
    }

    /// <summary>
    ///     取得原版角色建立頁面的私有欄位。
    ///     Retrieves a private field from the vanilla creation page.
    /// </summary>
    private static T GetVanillaField<T>(
        UICharacterCreation page,
        string name)
    {
        var field =
            typeof(UICharacterCreation).GetField(
                name,
                ReflectionFlags)
            ?? throw new MissingFieldException(
                typeof(UICharacterCreation).FullName,
                name);

        return (T)(field.GetValue(page)
                   ?? throw new InvalidOperationException(
                       $"Vanilla UI field '{name}' is null."));
    }
}