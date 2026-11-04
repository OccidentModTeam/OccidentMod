// 沿用原版按鈕外觀，顯示職業名稱及選取狀態。
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Occident.Common.UI.CharacterCreation;

internal sealed class ClassNameButton : UIDifficultyButton
{
    private readonly Player selection;
    private readonly byte classId;
    private readonly Asset<Texture2D> border;
    private readonly Asset<Texture2D> panel;
    private readonly UIText label;
    // 以暖銅色區分周圍的藍色區塊，保留原版按鈕形狀。
    private static readonly Color IdleFill = new(120, 76, 60);
    private static readonly Color IdleBorder = new(181, 139, 105);
    private static readonly Color HoverFill = new(148, 100, 73);
    private static readonly Color HoverBorder = new(211, 171, 124);
    private static readonly Color SelectedFill = new(178, 120, 67);
    private static readonly Color SelectedBorder = new(226, 190, 122);
    private static readonly Color SelectedText = new(255, 242, 216);

    public ClassNameButton(Player selection, LocalizedText name, LocalizedText description, byte classId)
        : base(selection, name, description, classId, IdleBorder)
    {
        this.selection = selection;
        this.classId = classId;
        border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelBorder");
        panel = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale");
        label = Children.OfType<UIText>().First();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool selected = selection.difficulty == classId;
        bool hovered = IsMouseHovering;
        label.TextColor = selected || hovered ? SelectedText : new Color(242, 228, 212);
        base.DrawSelf(spriteBatch);

        // 所有互動狀態都套用同一色系，避免未選取時回到原版藍色。
        Color fill = selected ? SelectedFill : hovered ? HoverFill : IdleFill;
        Color edge = selected ? SelectedBorder : hovered ? HoverBorder : IdleBorder;
        var area = GetDimensions().ToRectangle();
        Utils.DrawSplicedPanel(spriteBatch, panel.Value, area.X, area.Y, area.Width, area.Height,
            10, 10, 10, 10, fill);
        Utils.DrawSplicedPanel(spriteBatch, border.Value, area.X, area.Y, area.Width, area.Height,
            10, 10, 10, 10, edge);
    }
}
