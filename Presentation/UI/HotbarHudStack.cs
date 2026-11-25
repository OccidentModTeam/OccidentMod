#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VesperMod.Presentation.UI;

internal sealed class HotbarHudStack(Point firstPosition)
{
    private const int Gap = 14;
    private const int MinimumTop = 20;

    private readonly List<Entry> entries = [];

    // The first entry owns the anchor. Each later entry is placed above it.
    public void AddAbove(Point size, Action<SpriteBatch, Vector2> draw, Func<bool>? isVisible = null) =>
        entries.Add(new Entry(size, draw, isVisible));

    public void Draw(SpriteBatch spriteBatch)
    {
        if (entries.Count == 0)
            return;

        int stackHeight = 0;
        foreach (var entry in entries)
        {
            if (entry.Visible)
                stackHeight += entry.Size.Y + Gap;
        }

        int heightAboveFirst = stackHeight - entries[0].Size.Y - Gap;

        int y = Math.Max(MinimumTop, firstPosition.Y - heightAboveFirst);
        int anchorWidth = entries[0].Size.X;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (!entry.Visible)
                continue;

            int x = firstPosition.X + (anchorWidth - entry.Size.X) / 2;
            entry.Draw(spriteBatch, new Vector2(x, y));
            y += entry.Size.Y + Gap;
        }
    }

    private readonly record struct Entry(Point Size, Action<SpriteBatch, Vector2> Draw, Func<bool>? IsVisible)
    {
        public bool Visible => IsVisible?.Invoke() ?? true;
    }
}
