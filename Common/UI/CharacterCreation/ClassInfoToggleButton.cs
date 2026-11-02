using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace Occident.Common.UI.CharacterCreation;

internal sealed class ClassInfoToggleButton : UIPanel
{
    private readonly Asset<Texture2D> icon = ModContent.Request<Texture2D>("OccidentMod/Assets/UI/CharacterCreation/ClassInfoToggle");
    private readonly Func<bool> isOpen;

    public ClassInfoToggleButton(Func<bool> isOpen, Action toggle)
    {
        this.isOpen = isOpen;
        Width.Set(50f, 0f);
        Height.Set(50f, 0f);
        HAlign = VAlign = 1f;
        SetPadding(0f);
        SetSnapPoint("ClassInfoToggle", 0);
        OnLeftClick += (_, _) => toggle();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool active = isOpen() || IsMouseHovering;
        BackgroundColor = active ? new Color(63, 82, 151) : new Color(33, 43, 79) * 0.8f;
        BorderColor = active ? new Color(150, 165, 205) : Color.Black;
        base.DrawSelf(spriteBatch);

        // Bestiary's two horizontal frames: normal book and highlighted book.
        var texture = icon.Value;
        int frameWidth = texture.Width / 2;
        var source = new Rectangle(active ? frameWidth : 0, 0, frameWidth, texture.Height);
        spriteBatch.Draw(texture, GetDimensions().Center(), source, Color.White, 0f,
            new Vector2(frameWidth, texture.Height) * 0.5f, 1f, SpriteEffects.None, 0f);
    }
}
