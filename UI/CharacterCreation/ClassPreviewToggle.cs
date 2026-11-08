using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     提供職業武器或裝備預覽的開關控制。
///     Provides a toggle control for class weapon or equipment previews.
/// </summary>
/// <remarks>
///     此開關只管理角色建立介面的預覽狀態，
///     不會直接修改玩家裝備、背包或角色存檔。
///     This toggle manages presentation-only state.
///     It does not directly modify player equipment,
///     inventory, or character save data.
/// </remarks>
internal sealed class ClassPreviewToggle : UIPanel
{
    private const string LocalizationPrefix =
        "Mods.OccidentMod.UI.CharacterCreation.";

    private const float ToggleHeight = 46f;

    private static readonly Color DefaultBackground =
        new(24, 33, 55);

    private static readonly Color HoverBackground =
        new(34, 46, 69);

    private static readonly Color DefaultBorder =
        new(63, 82, 125);

    private static readonly Color EnabledStatusColor =
        new(230, 233, 240);

    private static readonly Color DisabledStatusColor =
        new(145, 158, 180);

    private readonly Color accentColor;

    private readonly UIText label;

    private readonly string localizationKey;
    private readonly UIText status;

    private string lastCulture = string.Empty;

    /// <summary>
    ///     建立職業預覽開關。
    ///     Creates a class preview toggle.
    /// </summary>
    /// <param name="localizationKey">
    ///     預覽選項名稱的本地化鍵值。
    ///     Localization key for the preview option name.
    /// </param>
    /// <param name="accent">
    ///     此預覽選項的強調顏色。
    ///     Accent color for this preview option.
    /// </param>
    public ClassPreviewToggle(
        string localizationKey,
        Color accent)
    {
        this.localizationKey = localizationKey;
        accentColor = accent;

        ConfigureLayout();

        label = CreateLabel();
        status = CreateStatus();

        Append(label);
        Append(status);

        OnLeftClick += (_, _) => Toggle();
    }

    /// <summary>
    ///     取得目前預覽開關是否啟用。
    ///     Gets whether the preview toggle is currently enabled.
    /// </summary>

    public bool Enabled { get; private set; }

    /// <summary>
    ///     設定預覽開關的尺寸與內距。
    ///     Configures the preview toggle dimensions and padding.
    /// </summary>
    private void ConfigureLayout()
    {
        Width.Set(0f, 1f);
        Height.Set(ToggleHeight, 0f);

        SetPadding(0f);
    }

    /// <summary>
    ///     建立預覽選項名稱文字。
    ///     Creates the preview option label.
    /// </summary>
    private UIText CreateLabel()
    {
        var text = new UIText(
            Language.GetText(localizationKey),
            0.85f)
        {
            HAlign = 0.5f,
            TextColor = accentColor,
            IgnoresMouseInteraction = true
        };

        text.Top.Set(7f, 0f);

        return text;
    }

    /// <summary>
    ///     建立預覽啟用狀態文字。
    ///     Creates the preview enabled/disabled status text.
    /// </summary>
    private static UIText CreateStatus()
    {
        var text = new UIText(
            Language.GetText(LocalizationPrefix + "PreviewOff"),
            0.65f)
        {
            HAlign = 0.5f,
            IgnoresMouseInteraction = true
        };

        text.Top.Set(27f, 0f);

        return text;
    }

    /// <summary>
    ///     切換預覽狀態並播放原版選單音效。
    ///     Toggles preview state and plays the vanilla menu sound.
    /// </summary>
    private void Toggle()
    {
        Enabled = !Enabled;

        RefreshStatus();

        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    /// <summary>
    ///     檢查語言變更並更新本地化文字。
    ///     Checks for language changes and refreshes localized text.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        if (lastCulture != Language.ActiveCulture.Name) RefreshStatus();

        base.Update(gameTime);
    }

    /// <summary>
    ///     更新預覽狀態及選項名稱的本地化文字。
    ///     Updates localized preview status and option label.
    /// </summary>
    private void RefreshStatus()
    {
        lastCulture = Language.ActiveCulture.Name;

        label.SetText(Language.GetText(localizationKey));

        var statusKey = Enabled
            ? "PreviewOn"
            : "PreviewOff";

        status.SetText(
            Language.GetText(LocalizationPrefix + statusKey));

        Recalculate();
    }

    /// <summary>
    ///     根據啟用及滑鼠懸停狀態更新按鈕外觀。
    ///     Updates button appearance based on enabled and hover states.
    /// </summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        UpdateVisualState();

        base.DrawSelf(spriteBatch);
    }

    /// <summary>
    ///     更新背景、邊框及文字顏色。
    ///     Updates background, border, and text colors.
    /// </summary>
    private void UpdateVisualState()
    {
        var isHovered = IsMouseHovering;

        BackgroundColor = Enabled
            ? Color.Lerp(
                DefaultBackground,
                accentColor,
                0.18f)
            : isHovered
                ? HoverBackground
                : DefaultBackground;

        BorderColor = Enabled
            ? accentColor
            : isHovered
                ? accentColor * 0.8f
                : DefaultBorder;

        label.TextColor = Enabled || isHovered
            ? accentColor
            : accentColor * 0.9f;

        status.TextColor = Enabled
            ? EnabledStatusColor
            : DisabledStatusColor;
    }
}