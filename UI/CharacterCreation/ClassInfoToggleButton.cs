using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     控制角色建立介面職業資訊側欄的書本圖示按鈕。
///     Provides a book-icon button for toggling
///     class information side panels.
/// </summary>
/// <remarks>
///     此元件僅負責顯示狀態與觸發切換操作。
///     實際面板的顯示與隱藏由外部控制器管理。
///     This component only displays the current state
///     and invokes the toggle action.
///     The owning controller manages panel visibility.
/// </remarks>
internal sealed class ClassInfoToggleButton : UIPanel
{
    private const float ButtonSize = 50f;

    private const string IconAssetPath =
        "OccidentMod/Assets/UI/CharacterCreation/ClassInfoToggle";

    private static readonly Color ActiveBackground =
        new(63, 82, 151);

    private static readonly Color InactiveBackground =
        new Color(33, 43, 79) * 0.8f;

    private static readonly Color ActiveBorder =
        new(150, 165, 205);

    private readonly Asset<Texture2D> icon;

    private readonly Func<bool> isOpen;
    private readonly Action toggle;

    /// <summary>
    ///     建立職業資訊面板切換按鈕。
    ///     Creates the class information toggle button.
    /// </summary>
    /// <param name="isOpen">
    ///     取得資訊面板目前是否展開。
    ///     Gets whether the information panels are currently open.
    /// </param>
    /// <param name="toggle">
    ///     切換資訊面板顯示狀態的回呼。
    ///     Callback that toggles information panel visibility.
    /// </param>
    public ClassInfoToggleButton(
        Func<bool> isOpen,
        Action toggle)
    {
        this.isOpen = isOpen;
        this.toggle = toggle;

        icon = ModContent.Request<Texture2D>(IconAssetPath);

        ConfigureLayout();

        OnLeftClick += (_, _) => Toggle();
    }

    /// <summary>
    ///     設定按鈕的原始尺寸、對齊與手把導覽位置。
    ///     Configures the original dimensions, alignment,
    ///     and gamepad navigation point.
    /// </summary>
    private void ConfigureLayout()
    {
        Width.Set(ButtonSize, 0f);
        Height.Set(ButtonSize, 0f);

        HAlign = 1f;
        VAlign = 1f;

        SetPadding(0f);

        SetSnapPoint("ClassInfoToggle", 0);
    }

    /// <summary>
    ///     通知外部控制器切換資訊面板。
    ///     Notifies the owning controller to toggle the panels.
    /// </summary>
    private void Toggle()
    {
        toggle();
    }

    /// <summary>
    ///     根據面板展開或滑鼠懸停狀態繪製按鈕。
    ///     Draws the button based on panel visibility
    ///     and mouse hover state.
    /// </summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var active = isOpen() || IsMouseHovering;

        UpdateVisualState(active);

        base.DrawSelf(spriteBatch);

        DrawIcon(spriteBatch, active);
    }

    /// <summary>
    ///     更新按鈕背景與邊框顏色。
    ///     Updates the button background and border colors.
    /// </summary>
    private void UpdateVisualState(bool active)
    {
        BackgroundColor = active
            ? ActiveBackground
            : InactiveBackground;

        BorderColor = active
            ? ActiveBorder
            : Color.Black;
    }

    /// <summary>
    ///     繪製書本圖示，並依照狀態選擇對應影格。
    ///     Draws the book icon using the frame
    ///     corresponding to the current state.
    /// </summary>
    private void DrawIcon(
        SpriteBatch spriteBatch,
        bool active)
    {
        var texture = icon.Value;

        // 原始素材水平排列兩個影格：
        // 左側為一般狀態，右側為反白狀態。
        //
        // The texture contains two horizontal frames:
        // normal on the left, highlighted on the right.

        var frameWidth = texture.Width / 2;

        Rectangle source = new(
            active ? frameWidth : 0,
            0,
            frameWidth,
            texture.Height);

        Vector2 origin = new(
            frameWidth * 0.5f,
            texture.Height * 0.5f);

        spriteBatch.Draw(
            texture,
            GetDimensions().Center(),
            source,
            Color.White,
            0f,
            origin,
            1f,
            SpriteEffects.None,
            0f);
    }
}