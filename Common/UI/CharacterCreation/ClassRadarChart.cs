using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;
using ReLogic.Graphics;
using Occident.Common.Classes;

namespace Occident.Common.UI.CharacterCreation;

internal sealed class ClassRadarChart : UIElement
{
    private static readonly Rectangle PixelSource = new(0, 0, 1, 1);
    private static readonly string[] AxisKeys =
        { "Damage", "Survival", "Mobility", "Control", "Support" };
    private readonly float[] values = new float[5];
    private readonly float[] displayedValues = new float[5];
    private float animationProgress = 1f;
    private const float ExpandDuration = 0.7f;
    private readonly Vector2[] points = new Vector2[5];
    private readonly float[] intersections = new float[5];
    private readonly string[] captions = new string[5];
    private readonly float[] captionScales = new float[5];
    private DynamicSpriteFont measuredFont;
    private static readonly Vector2[] Directions = CreateDirections();
    public bool HasSelection { get; set; }
    public Color FillColor { get; set; } = new Color(178, 120, 67) * 0.3f;

    public ClassRadarChart()
    {
        Width.Set(0f, 1f);
        Height.Set(300f, 0f);
        IgnoresMouseInteraction = true;
    }

    public void SetRatings(ClassRating value)
    {
        measuredFont = null;
        animationProgress = HasSelection ? 0f : 1f;
        Array.Clear(displayedValues, 0, displayedValues.Length);
        values[0] = value.Damage;
        values[1] = value.Survival;
        values[2] = value.Mobility;
        values[3] = value.Control;
        values[4] = value.Support;
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = float.IsFinite(values[i]) ? MathHelper.Clamp(values[i], 0f, ClassRatings.MaxRating) : 0f;
            string key = $"Mods.Occident.UI.CharacterCreation.Ratings.{AxisKeys[i]}";
            string label = Language.GetTextValue(key);
            captions[i] = (label == key ? AxisKeys[i] : label) + (HasSelection ? $" {values[i]:0.#}" : "");
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (animationProgress >= 1f)
        {
            base.Update(gameTime);
            return;
        }
        animationProgress = MathHelper.Clamp(animationProgress +
            (float)gameTime.ElapsedGameTime.TotalSeconds / ExpandDuration, 0f, 1f);
        float remaining = 1f - animationProgress;
        float amount = 1f - remaining * remaining * remaining;
        for (int i = 0; i < values.Length; i++)
            displayedValues[i] = values[i] * amount;
        base.Update(gameTime);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        var area = GetDimensions();
        Vector2 center = new(area.X + area.Width / 2f, area.Y + area.Height / 2f);
        float radius = Math.Min(area.Width / 2f - 66f, area.Height / 2f - 28f);
        if (radius <= 0f || ClassRatings.MaxRating <= 0f)
            return;

        var font = FontAssets.MouseText.Value;
        if (!ReferenceEquals(measuredFont, font))
        {
            for (int i = 0; i < captions.Length; i++)
                captionScales[i] = Math.Min(0.7f, 86f / Math.Max(1f,
                    font.MeasureString(captions[i] ?? AxisKeys[i]).X));
            measuredFont = font;
        }

        for (int i = 0; i < 5; i++)
            points[i] = Point(center, radius * displayedValues[i] / ClassRatings.MaxRating, i);

        if (HasSelection)
            Fill(spriteBatch, points, FillColor);
        Color grid = new(85, 103, 145);
        Color gold = new(226, 190, 122);
        for (int layer = 1; layer <= 5; layer++)
            for (int i = 0; i < 5; i++)
                Line(spriteBatch, Point(center, radius * layer / 5f, i),
                    Point(center, radius * layer / 5f, (i + 1) % 5), grid, 1f);

        for (int i = 0; i < 5; i++)
            Line(spriteBatch, center, Point(center, radius, i), grid, 1f);

        for (int i = 0; i < 5; i++)
        {
            if (HasSelection)
            {
                Line(spriteBatch, points[i], points[(i + 1) % 5], gold, 2f);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, points[i], PixelSource,
                    gold, 0f, new Vector2(0.5f), new Vector2(5f), SpriteEffects.None, 0f);
            }
            string caption = captions[i] ?? AxisKeys[i];
            // 縮放較長的翻譯，避免文字越過圖表邊界。
            Utils.DrawBorderString(spriteBatch, caption,
                Point(center, radius + 18f, i), new Color(230, 230, 235), captionScales[i], 0.5f, 0.5f);
        }
    }

    private static Vector2 Point(Vector2 center, float radius, int index) => center + Directions[index] * radius;

    private static Vector2[] CreateDirections()
    {
        var directions = new Vector2[5];
        for (int i = 0; i < directions.Length; i++)
        {
            float angle = -MathHelper.PiOver2 + MathHelper.TwoPi * i / 5f;
            directions[i] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
        return directions;
    }

    private static void Line(SpriteBatch batch, Vector2 a, Vector2 b,
        Color color, float thickness)
    {
        Vector2 delta = b - a;
        if (delta.LengthSquared() < 0.001f) return;
        batch.Draw(TextureAssets.MagicPixel.Value, a, PixelSource, color,
            (float)Math.Atan2(delta.Y, delta.X), new Vector2(0f, 0.5f),
            new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
    }

    private void Fill(SpriteBatch batch, Vector2[] vertices, Color color)
    {
        float min = vertices[0].Y, max = min;
        foreach (var p in vertices) { min = Math.Min(min, p.Y); max = Math.Max(max, p.Y); }
        for (int y = (int)Math.Ceiling(min - 0.5f); y < (int)Math.Ceiling(max - 0.5f); y++)
        {
            float scanY = y + 0.5f;
            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                var a = vertices[i]; var b = vertices[(i + 1) % 5];
                if ((a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY))
                    intersections[count++] = a.X + (scanY - a.Y) * (b.X - a.X) / (b.Y - a.Y);
            }
            Array.Sort(intersections, 0, count);
            for (int i = 0; i + 1 < count; i += 2)
            {
                int left = (int)Math.Ceiling(intersections[i] - 0.5f);
                int right = (int)Math.Ceiling(intersections[i + 1] - 0.5f);
                if (right > left)
                    batch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(left, y, right - left, 1), PixelSource, color);
            }
        }
    }
}

