using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OccidentMod.Presentation.UI;

internal static class BarTextureRegions
{
    internal static int FilledWidth(int totalWidth, int current, int maximum, int pixelSize) =>
        maximum > 0
            ? (int)((long)Math.Clamp(current, 0, maximum) * (totalWidth / pixelSize) / maximum) * pixelSize
            : 0;

    internal static Rectangle HorizontalFill(Texture2D texture, float fraction, int columnWidth)
    {
        int columns = texture.Width / columnWidth;
        int width = (int)(Math.Clamp(fraction, 0f, 1f) * columns) * columnWidth;
        return new Rectangle(0, 0, width, texture.Height);
    }

    internal static bool TryVerticalFrame(Texture2D texture, int height, int frame, out Rectangle source)
    {
        if (height <= 0 || (uint)frame >= (uint)(texture.Height / height))
        {
            source = default;
            return false;
        }

        source = new Rectangle(0, frame * height, texture.Width, height);
        return true;
    }
}
