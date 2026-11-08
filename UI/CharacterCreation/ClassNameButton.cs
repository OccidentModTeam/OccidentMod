using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     使用原版角色難度按鈕的外觀顯示職業名稱。
///     Displays class names using the vanilla character difficulty button style.
/// </summary>
/// <remarks>
///     保留原版按鈕結構，使用暖銅色區分職業選擇區域。
///     選取狀態由外部提供的顯示用 Player 決定，
///     不直接修改實際玩家的難度。
///     Preserves the vanilla button structure with a warm copper palette.
///     Selection is determined by a separate presentation-only Player,
///     without modifying the actual player's difficulty.
/// </remarks>
internal sealed class ClassNameButton : UIDifficultyButton
{
    private const string BorderTexturePath =
        "Images/UI/CharCreation/CategoryPanelBorder";

    private const string PanelTexturePath =
        "Images/UI/CharCreation/PanelGrayscale";

    private const int CornerSize = 10;

    private static readonly Color IdleFill =
        new(120, 76, 60);

    private static readonly Color IdleBorder =
        new(181, 139, 105);

    private static readonly Color HoverFill =
        new(148, 100, 73);

    private static readonly Color HoverBorder =
        new(211, 171, 124);

    private static readonly Color SelectedFill =
        new(178, 120, 67);

    private static readonly Color SelectedBorder =
        new(226, 190, 122);

    private static readonly Color SelectedText =
        new(255, 242, 216);

    private static readonly Color NormalText =
        new(242, 228, 212);

    private readonly Asset<Texture2D> borderTexture;
    private readonly byte classId;

    private readonly UIText label;
    private readonly Asset<Texture2D> panelTexture;

    private readonly Player selectionPlayer;

    /// <summary>
    ///     建立職業名稱按鈕。
    ///     Creates a class name button.
    /// </summary>
    /// <param name="selectionPlayer">
    ///     僅用於選取效果的玩家物件。
    ///     Player instance used only for selection visuals.
    /// </param>
    /// <param name="name">
    ///     職業名稱。
    ///     Localized class name.
    /// </param>
    /// <param name="description">
    ///     職業說明。
    ///     Localized class description.
    /// </param>
    /// <param name="classId">
    ///     職業識別值。
    ///     Class identifier.
    /// </param>
    public ClassNameButton(
        Player selectionPlayer,
        LocalizedText name,
        LocalizedText description,
        byte classId)
        : base(
            selectionPlayer,
            name,
            description,
            classId,
            IdleBorder)
    {
        this.selectionPlayer = selectionPlayer;
        this.classId = classId;

        borderTexture = Main.Assets.Request<Texture2D>(
            BorderTexturePath);

        panelTexture = Main.Assets.Request<Texture2D>(
            PanelTexturePath);

        label = Children.OfType<UIText>().First();
    }

    /// <summary>
    ///     繪製職業按鈕及其互動狀態。
    ///     Draws the class button and its interaction state.
    /// </summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var isSelected = selectionPlayer.difficulty == classId;
        var isHovered = IsMouseHovering;

        UpdateTextColor(isSelected, isHovered);

        // 保留原版按鈕繪製行為。
        // Preserve vanilla button drawing behavior.
        base.DrawSelf(spriteBatch);

        DrawCopperPanel(spriteBatch, isSelected, isHovered);
    }

    /// <summary>
    ///     根據選取及滑鼠懸停狀態更新文字顏色。
    ///     Updates text color based on selection and hover state.
    /// </summary>
    private void UpdateTextColor(
        bool isSelected,
        bool isHovered)
    {
        label.TextColor = isSelected || isHovered
            ? SelectedText
            : NormalText;
    }

    /// <summary>
    ///     使用原版九宮格貼圖繪製暖銅色按鈕。
    ///     Draws the copper button using vanilla nine-slice textures.
    /// </summary>
    private void DrawCopperPanel(
        SpriteBatch spriteBatch,
        bool isSelected,
        bool isHovered)
    {
        var fillColor = GetFillColor(
            isSelected,
            isHovered);

        var borderColor = GetBorderColor(
            isSelected,
            isHovered);

        var bounds = GetDimensions().ToRectangle();

        Utils.DrawSplicedPanel(
            spriteBatch,
            panelTexture.Value,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            CornerSize,
            CornerSize,
            CornerSize,
            CornerSize,
            fillColor);

        Utils.DrawSplicedPanel(
            spriteBatch,
            borderTexture.Value,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            CornerSize,
            CornerSize,
            CornerSize,
            CornerSize,
            borderColor);
    }

    /// <summary>
    ///     取得目前互動狀態的按鈕填充顏色。
    ///     Gets the fill color for the current interaction state.
    /// </summary>
    private static Color GetFillColor(
        bool isSelected,
        bool isHovered)
    {
        if (isSelected)
            return SelectedFill;

        return isHovered
            ? HoverFill
            : IdleFill;
    }

    /// <summary>
    ///     取得目前互動狀態的按鈕邊框顏色。
    ///     Gets the border color for the current interaction state.
    /// </summary>
    private static Color GetBorderColor(
        bool isSelected,
        bool isHovered)
    {
        if (isSelected)
            return SelectedBorder;

        return isHovered
            ? HoverBorder
            : IdleBorder;
    }
}