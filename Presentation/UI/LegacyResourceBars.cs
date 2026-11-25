using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace VesperMod.Presentation.UI;

internal sealed class LegacyResourceBars
{
    private const string Folder = "Assets/UI/HUD/HorizontalBars/";
    private const int PixelSize = 2;
    private const int StaminaPerSegment = 10;

    internal Row Life { get; }
    internal Row Mana { get; }
    internal Row Stamina { get; }

    internal LegacyResourceBars(Mod mod)
    {
        Asset<Texture2D> Load(string name) => mod.Assets.Request<Texture2D>(Folder + name);
        var sharedLeft = Load("Panel_Left");

        Life = new Row(sharedLeft, Load("HP_Panel_Middle"), Load("HP_Panel_Right"),
            Load("HP_Fill"), Load("HP_Fill_Honey"), RightOverlap: 12, FrameTop: 10, FillTop: 6);
        Mana = new Row(sharedLeft, Load("MP_Panel_Middle"), Load("MP_Panel_Right"),
            Load("MP_Fill"), Load("MP_Fill_Silver"), RightOverlap: 16, FrameTop: 6, FillTop: 6);
        Stamina = new Row(Load("Small_Panel_Left"), Load("SP_Panel_Middle"), Load("Small_Panel_Right"),
            Load("SP_Fill"), Load("SP_Fill_Jade"), RightOverlap: 0, FrameTop: 0, FillTop: 4);
    }

    internal static int StaminaSegments(int maximum) =>
        Math.Clamp((Math.Max(1, maximum) - 1) / StaminaPerSegment + 1, 1, 20);

    internal readonly record struct Row(Asset<Texture2D> Left, Asset<Texture2D> Segment, Asset<Texture2D> Right,
        Asset<Texture2D> Fill, Asset<Texture2D> UpgradedFill, int RightOverlap, int FrameTop, int FillTop)
    {
        internal int Height => Math.Max(Right.Value.Height, FrameTop + Segment.Value.Height);

        internal Rectangle GetBounds(int right, int top, int segments)
        {
            int width = Left.Value.Width + Segment.Value.Width * segments + Right.Value.Width - RightOverlap;
            return new Rectangle(right - width, top, width, Height);
        }

        internal void Draw(SpriteBatch batch, Rectangle bounds, int segments, int current, int maximum,
            int upgradedSegments)
        {
            Texture2D segment = Segment.Value;
            int filledWidth = BarTextureRegions.FilledWidth(segment.Width * segments, current, maximum, PixelSize);

            int frameY = bounds.Y + FrameTop;
            Texture2D left = Left.Value;
            batch.Draw(left, new Vector2(bounds.Left, frameY), Color.White);

            int x = bounds.Left + left.Width;
            for (int i = 0; i < segments; i++, x += segment.Width)
            {
                batch.Draw(segment, new Vector2(x, frameY), Color.White);
                int segmentsToRight = segments - 1 - i;
                int visibleWidth = Math.Clamp(filledWidth - segmentsToRight * segment.Width, 0, segment.Width);
                if (visibleWidth == 0)
                    continue;

                Texture2D fill = (segmentsToRight < upgradedSegments ? UpgradedFill : Fill).Value;
                int hiddenWidth = fill.Width - visibleWidth;
                var clip = new Rectangle(hiddenWidth, 0, visibleWidth, fill.Height);
                batch.Draw(fill, new Vector2(x + hiddenWidth, frameY + FillTop), clip, Color.White);
            }

            // The authored right panel includes the badge and surrounding trim.
            Texture2D right = Right.Value;
            batch.Draw(right, new Vector2(bounds.Right - right.Width, bounds.Y), Color.White);
        }
    }
}
