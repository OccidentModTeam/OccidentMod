using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace VesperMod.Presentation.UI;

internal sealed class FancyClassicHud
{
    private const string Folder = "Assets/UI/HUD/FancyClassic/";
    internal const int SlotsPerRow = 10;
    private const int HeartSpacing = 28;
    private const int StarSpacing = 22;
    internal const int ManaColumnSpacing = 32;
    private const int DiamondStep = 12;
    private const int DiamondRowOffset = 12;
    private const int DiamondsPerHundredStamina = 6;

    private readonly Asset<Texture2D> heartLeft, heartMiddle, heartRight, heartFancy, heartSingle, heartFill, heartGold;
    private readonly Asset<Texture2D> starTop, starMiddle, starBottom, starSingle, starFill, starSilver;
    private readonly Asset<Texture2D> diamond, diamondFill, diamondJade;

    internal FancyClassicHud(Mod mod)
    {
        Asset<Texture2D> Load(string name) => mod.Assets.Request<Texture2D>(Folder + name);
        heartLeft = Load("Heart_Left");
        heartMiddle = Load("Heart_Middle");
        heartRight = Load("Heart_Right");
        heartFancy = Load("Heart_Right_Fancy");
        heartSingle = Load("Heart_Single_Fancy");
        heartFill = Load("Heart_Fill");
        heartGold = Load("Heart_Fill_B");
        starTop = Load("Star_A");
        starMiddle = Load("Star_B");
        starBottom = Load("Star_C");
        starSingle = Load("Star_Single");
        starFill = Load("Star_Fill");
        starSilver = Load("Star_Fill_B");
        diamond = Load("Stamina_A");
        diamondFill = Load("Stamina_Fill");
        diamondJade = Load("Stamina_Fill_B");
    }

    internal Rectangle DrawLife(SpriteBatch batch, int right, int top, int slots, int current, int max, int gold)
    {
        int firstRow = Math.Min(slots, SlotsPerRow);
        int secondRow = slots - firstRow;
        int width = heartFill.Value.Width;
        int filled = BarTextureRegions.FilledWidth(slots * width, current, max, 2);
        var bounds = DrawHeartRow(batch, right, top, firstRow, 0, width, filled, gold, secondRow == 0);
        if (secondRow != 0)
        {
            var lowerRow = DrawHeartRow(batch, right, top + HeartSpacing, secondRow, firstRow, width, filled, gold, true);
            bounds = Rectangle.Union(bounds, lowerRow);
        }
        return bounds;
    }

    internal int LifeBottom(int top, int slots) =>
        top + (slots > SlotsPerRow ? HeartSpacing : 0) + heartFancy.Value.Height - 4;

    internal int ManaColumnBottom(int top, int slots) => slots switch
    {
        0 => top,
        1 => top + starSingle.Value.Height,
        _ => top + starTop.Value.Height + (slots - 2) * starMiddle.Value.Height + starBottom.Value.Height
    };

    private Rectangle DrawHeartRow(SpriteBatch batch, int right, int top, int count, int resourceOffset,
        int fillWidth, int filled, int gold, bool fancyEnd)
    {
        Texture2D last = (count == 1 ? heartSingle : fancyEnd ? heartFancy : heartRight).Value;
        int overlap = count > 1 && fancyEnd ? 8 : 0;
        int frameTop = count == 1 || fancyEnd ? top - 4 : top;
        int end = right - last.Width;
        if (count == 1)
        {
            batch.Draw(last, new Vector2(end, frameTop), Color.White);
            DrawFill(batch, (resourceOffset < gold ? heartGold : heartFill).Value, end + 8, top + 4,
                filled - resourceOffset * fillWidth);
            return new Rectangle(end, frameTop, last.Width, last.Height);
        }

        Texture2D left = heartLeft.Value;
        Texture2D middle = heartMiddle.Value;
        int leftX = end + overlap - (count - 2) * middle.Width - left.Width;
        batch.Draw(left, new Vector2(leftX, top), Color.White);
        for (int i = 0; i < count - 2; i++)
            batch.Draw(middle, new Vector2(leftX + left.Width + i * middle.Width, top), Color.White);
        batch.Draw(last, new Vector2(end, frameTop), Color.White);

        for (int i = 0; i < count; i++)
        {
            int slotFromRight = resourceOffset + count - i - 1;
            DrawFill(batch, (slotFromRight < gold ? heartGold : heartFill).Value,
                leftX + 4 + i * middle.Width, top + 4, filled - slotFromRight * fillWidth);
        }
        return new Rectangle(leftX, frameTop, right - leftX, last.Height);
    }

    internal Rectangle DrawMana(SpriteBatch batch, int right, int top, int slots, int current, int max, bool silver)
    {
        Texture2D fill = (silver ? starSilver : starFill).Value;
        int filled = BarTextureRegions.FilledWidth(slots * fill.Width, current, max, 2);
        Rectangle bounds = Rectangle.Empty;
        for (int offset = 0; offset < slots; offset += SlotsPerRow)
        {
            int count = Math.Min(slots - offset, SlotsPerRow);
            int columnRight = right - offset / SlotsPerRow * ManaColumnSpacing;
            int x = columnRight - starTop.Value.Width;
            int y = top;
            for (int i = 0; i < count; i++)
            {
                Texture2D frame = (count == 1 ? starSingle : i == 0 ? starTop : i == count - 1 ? starBottom : starMiddle).Value;
                batch.Draw(frame, new Vector2(x, y), Color.White);
                y += frame.Height;
            }
            for (int i = 0; i < count; i++)
            {
                int fillY = top + (count == 1 ? 2 : 4) + i * StarSpacing;
                DrawFill(batch, fill, x + 4, fillY, filled - (offset + i) * fill.Width);
            }
            var column = new Rectangle(x, top, columnRight - x, y - top);
            bounds = offset == 0 ? column : Rectangle.Union(bounds, column);
        }
        return bounds;
    }

    internal Rectangle StaminaBounds(int right, int top, int maximum)
    {
        int slots = DiamondSlots(maximum);
        int width = (slots - 1) * DiamondStep + diamond.Value.Width;
        int height = diamond.Value.Height + (slots > 1 ? DiamondRowOffset : 0);
        return new Rectangle(right - width, top, width, height);
    }

    internal void DrawStamina(SpriteBatch batch, Rectangle bounds, int current, int max, bool jade, bool showFrame = true)
    {
        Texture2D frame = diamond.Value;
        Texture2D fill = (jade ? diamondJade : diamondFill).Value;
        int diamonds = DiamondSlots(max);
        int filled = BarTextureRegions.FilledWidth(diamonds * fill.Width, current, max, 2);
        for (int i = 0; i < diamonds; i++)
        {
            int x = bounds.Right - frame.Width - i * DiamondStep;
            int y = bounds.Top + (diamonds > 1 && i % 2 == 0 ? DiamondRowOffset : 0);
            if (showFrame)
                batch.Draw(frame, new Vector2(x, y), Color.White);
            DrawFill(batch, fill, x + 4, y + 4, filled - i * fill.Width);
        }
    }

    private static int DiamondSlots(int maximum) =>
        (int)Math.Clamp(((long)Math.Max(1, maximum) * DiamondsPerHundredStamina + 99) / 100, 1, 20);

    private static void DrawFill(SpriteBatch batch, Texture2D texture, int x, int y, int width)
    {
        int visible = Math.Clamp(width, 0, texture.Width);
        if (visible == 0)
            return;

        int hidden = texture.Width - visible;
        batch.Draw(texture, new Vector2(x + hidden, y), new Rectangle(hidden, 0, visible, texture.Height), Color.White);
    }
}
