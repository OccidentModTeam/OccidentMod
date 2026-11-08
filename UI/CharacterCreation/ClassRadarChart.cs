using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Gameplay.Classes;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     繪製五軸職業能力雷達圖，包含刻度、標籤及展開動畫。
///     Draws a five-axis class ability radar chart with
///     grid lines, captions, and expansion animation.
/// </summary>
/// <remarks>
///     使用 Terraria 原版 MagicPixel 繪製線條及填充區域，
///     不需要額外的網格資源或自訂著色器。
///     Uses Terraria's MagicPixel for line and polygon rendering,
///     without additional mesh resources or custom shaders.
/// </remarks>
internal sealed class ClassRadarChart : UIElement
{
    private const int AxisCount = 5;
    private const int GridLayerCount = 5;

    private const float ExpandDuration = 0.7f;
    private const float MaximumCaptionScale = 0.7f;
    private const float MaximumCaptionWidth = 86f;

    private const float CaptionDistance = 18f;
    private const float HorizontalRadiusPadding = 66f;
    private const float VerticalRadiusPadding = 28f;

    private const string RatingsLocalizationPrefix =
        "Mods.OccidentMod.UI.CharacterCreation.Ratings.";

    private static readonly Rectangle PixelSource =
        new(0, 0, 1, 1);

    private static readonly string[] AxisKeys =
    {
        "Damage",
        "Survival",
        "Mobility",
        "Control",
        "Support"
    };

    private static readonly Vector2[] Directions =
        CreateDirections();

    private static readonly Color GridColor =
        new(85, 103, 145);

    private static readonly Color OutlineColor =
        new(226, 190, 122);

    private static readonly Color CaptionColor =
        new(230, 230, 235);

    private readonly float[] captionScales = new float[AxisCount];

    private readonly string[] captions = new string[AxisCount];
    private readonly float[] displayedValues = new float[AxisCount];
    private readonly float[] intersections = new float[AxisCount];

    private readonly Vector2[] points = new Vector2[AxisCount];

    private readonly float[] values = new float[AxisCount];

    private float animationProgress = 1f;

    private DynamicSpriteFont measuredFont;

    /// <summary>
    ///     建立五軸能力雷達圖。
    ///     Creates a five-axis ability radar chart.
    /// </summary>
    public ClassRadarChart()
    {
        Width.Set(0f, 1f);
        Height.Set(300f, 0f);

        IgnoresMouseInteraction = true;
    }

    /// <summary>
    ///     是否存在有效的職業選擇。
    ///     Whether a valid class is currently selected.
    /// </summary>

    public bool HasSelection { get; set; }

    /// <summary>
    ///     雷達圖內部填充顏色。
    ///     Fill color of the radar polygon.
    /// </summary>

    public Color FillColor { get; set; } =
        new Color(178, 120, 67) * 0.3f;

    /// <summary>
    ///     設定目標能力評分並重新開始展開動畫。
    ///     Sets target ability ratings and restarts
    ///     the expansion animation.
    /// </summary>
    public void SetRatings(ClassRating rating)
    {
        values[0] = rating.Damage;
        values[1] = rating.Survival;
        values[2] = rating.Mobility;
        values[3] = rating.Control;
        values[4] = rating.Support;

        ResetAnimation();
        UpdateCaptions();
    }

    /// <summary>
    ///     重設動畫與目前顯示的能力數值。
    ///     Resets animation and currently displayed ratings.
    /// </summary>
    private void ResetAnimation()
    {
        animationProgress = HasSelection ? 0f : 1f;

        Array.Clear(displayedValues, 0, displayedValues.Length);

        measuredFont = null;
    }

    /// <summary>
    ///     限制能力數值範圍，並更新本地化標籤。
    ///     Clamps ability values and updates localized captions.
    /// </summary>
    private void UpdateCaptions()
    {
        for (var i = 0; i < AxisCount; i++)
        {
            values[i] = float.IsFinite(values[i])
                ? MathHelper.Clamp(
                    values[i],
                    0f,
                    ClassRatings.MaxRating)
                : 0f;

            var key =
                RatingsLocalizationPrefix + AxisKeys[i];

            var localized = Language.GetTextValue(key);

            var caption = localized == key
                ? AxisKeys[i]
                : localized;

            captions[i] = HasSelection
                ? $"{caption} {values[i]:0.#}"
                : caption;
        }
    }

    /// <summary>
    ///     更新雷達圖的展開動畫。
    ///     Updates the radar chart expansion animation.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        if (animationProgress < 1f) UpdateAnimation(gameTime);

        base.Update(gameTime);
    }

    /// <summary>
    ///     使用三次緩出曲線計算目前顯示的能力數值。
    ///     Uses cubic ease-out interpolation for displayed ratings.
    /// </summary>
    private void UpdateAnimation(GameTime gameTime)
    {
        var elapsed =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        animationProgress = MathHelper.Clamp(
            animationProgress + elapsed / ExpandDuration,
            0f,
            1f);

        var remaining = 1f - animationProgress;

        var amount =
            1f - remaining * remaining * remaining;

        for (var i = 0; i < AxisCount; i++) displayedValues[i] = values[i] * amount;
    }

    /// <summary>
    ///     繪製雷達圖填充、網格、邊框及能力標籤。
    ///     Draws the radar fill, grid, outline, and captions.
    /// </summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var area = GetDimensions();

        Vector2 center = new(
            area.X + area.Width / 2f,
            area.Y + area.Height / 2f);

        var radius = Math.Min(
            area.Width / 2f - HorizontalRadiusPadding,
            area.Height / 2f - VerticalRadiusPadding);

        if (radius <= 0f || ClassRatings.MaxRating <= 0f)
            return;

        UpdateCaptionMeasurements();
        UpdatePolygonPoints(center, radius);

        if (HasSelection) FillPolygon(spriteBatch, points, FillColor);

        DrawGrid(spriteBatch, center, radius);
        DrawAxes(spriteBatch, center, radius);
        DrawRatings(spriteBatch, center, radius);
    }

    /// <summary>
    ///     更新標籤的文字縮放快取。
    ///     Updates cached caption text scales.
    /// </summary>
    private void UpdateCaptionMeasurements()
    {
        var font = FontAssets.MouseText.Value;

        if (ReferenceEquals(measuredFont, font))
            return;

        for (var i = 0; i < AxisCount; i++)
        {
            var caption = captions[i] ?? AxisKeys[i];

            var width = Math.Max(
                1f,
                font.MeasureString(caption).X);

            captionScales[i] = Math.Min(
                MaximumCaptionScale,
                MaximumCaptionWidth / width);
        }

        measuredFont = font;
    }

    /// <summary>
    ///     根據動畫數值計算雷達圖的五個頂點。
    ///     Calculates the five polygon vertices from animated ratings.
    /// </summary>
    private void UpdatePolygonPoints(Vector2 center, float radius)
    {
        for (var i = 0; i < AxisCount; i++)
        {
            var distance =
                radius * displayedValues[i] /
                ClassRatings.MaxRating;

            points[i] = GetPoint(center, distance, i);
        }
    }

    /// <summary>
    ///     繪製五層雷達圖刻度網格。
    ///     Draws the five concentric radar grid layers.
    /// </summary>
    private static void DrawGrid(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius)
    {
        for (var layer = 1; layer <= GridLayerCount; layer++)
        {
            var layerRadius =
                radius * layer / GridLayerCount;

            for (var i = 0; i < AxisCount; i++)
            {
                var start =
                    GetPoint(center, layerRadius, i);

                var end = GetPoint(
                    center,
                    layerRadius,
                    (i + 1) % AxisCount);

                DrawLine(
                    spriteBatch,
                    start,
                    end,
                    GridColor,
                    1f);
            }
        }
    }

    /// <summary>
    ///     繪製從中心延伸至各能力方向的軸線。
    ///     Draws axis lines from the center to each rating direction.
    /// </summary>
    private static void DrawAxes(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius)
    {
        for (var i = 0; i < AxisCount; i++)
            DrawLine(
                spriteBatch,
                center,
                GetPoint(center, radius, i),
                GridColor,
                1f);
    }

    /// <summary>
    ///     繪製能力多邊形邊框、頂點與標籤。
    ///     Draws the ability polygon outline, vertices, and captions.
    /// </summary>
    private void DrawRatings(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius)
    {
        for (var i = 0; i < AxisCount; i++)
        {
            if (HasSelection)
            {
                DrawLine(
                    spriteBatch,
                    points[i],
                    points[(i + 1) % AxisCount],
                    OutlineColor,
                    2f);

                DrawVertex(spriteBatch, points[i]);
            }

            DrawCaption(
                spriteBatch,
                center,
                radius,
                i);
        }
    }

    /// <summary>
    ///     繪製能力多邊形的頂點標記。
    ///     Draws a vertex marker on the ability polygon.
    /// </summary>
    private static void DrawVertex(
        SpriteBatch spriteBatch,
        Vector2 position)
    {
        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            position,
            PixelSource,
            OutlineColor,
            0f,
            new Vector2(0.5f),
            new Vector2(5f),
            SpriteEffects.None,
            0f);
    }

    /// <summary>
    ///     繪製指定軸線的本地化能力標籤。
    ///     Draws the localized caption for the specified axis.
    /// </summary>
    private void DrawCaption(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius,
        int index)
    {
        var caption = captions[index] ?? AxisKeys[index];

        var position = GetPoint(
            center,
            radius + CaptionDistance,
            index);

        Utils.DrawBorderString(
            spriteBatch,
            caption,
            position,
            CaptionColor,
            captionScales[index],
            0.5f,
            0.5f);
    }

    /// <summary>
    ///     取得指定軸線及半徑的座標。
    ///     Gets a point along the specified axis and radius.
    /// </summary>
    private static Vector2 GetPoint(
        Vector2 center,
        float radius,
        int index)
    {
        return center + Directions[index] * radius;
    }

    /// <summary>
    ///     預先計算五條軸線的方向向量。
    ///     Precomputes direction vectors for the five axes.
    /// </summary>
    private static Vector2[] CreateDirections()
    {
        var directions = new Vector2[AxisCount];

        for (var i = 0; i < AxisCount; i++)
        {
            var angle =
                -MathHelper.PiOver2 +
                MathHelper.TwoPi * i / AxisCount;

            directions[i] = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle));
        }

        return directions;
    }

    /// <summary>
    ///     使用 MagicPixel 繪製指定粗細的線段。
    ///     Draws a line segment of the specified thickness
    ///     using MagicPixel.
    /// </summary>
    private static void DrawLine(
        SpriteBatch spriteBatch,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness)
    {
        var delta = end - start;

        if (delta.LengthSquared() < 0.001f)
            return;

        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            start,
            PixelSource,
            color,
            (float)Math.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(delta.Length(), thickness),
            SpriteEffects.None,
            0f);
    }

    /// <summary>
    ///     使用水平掃描線填充能力多邊形。
    ///     Fills the ability polygon using horizontal scanlines.
    /// </summary>
    /// <remarks>
    ///     保留原本的掃描線演算法，避免額外建立頂點緩衝區。
    ///     Preserves the original scanline algorithm without
    ///     allocating additional vertex buffers.
    /// </remarks>
    private void FillPolygon(
        SpriteBatch spriteBatch,
        Vector2[] vertices,
        Color color)
    {
        var minY = vertices[0].Y;
        var maxY = minY;

        foreach (var vertex in vertices)
        {
            minY = Math.Min(minY, vertex.Y);
            maxY = Math.Max(maxY, vertex.Y);
        }

        var startY = (int)Math.Ceiling(minY - 0.5f);
        var endY = (int)Math.Ceiling(maxY - 0.5f);

        for (var y = startY; y < endY; y++)
        {
            var scanY = y + 0.5f;
            var intersectionCount = 0;

            for (var i = 0; i < AxisCount; i++)
            {
                var start = vertices[i];
                var end = vertices[(i + 1) % AxisCount];

                var crosses =
                    (start.Y <= scanY && end.Y > scanY) ||
                    (end.Y <= scanY && start.Y > scanY);

                if (!crosses)
                    continue;

                intersections[intersectionCount++] =
                    start.X +
                    (scanY - start.Y) *
                    (end.X - start.X) /
                    (end.Y - start.Y);
            }

            Array.Sort(
                intersections,
                0,
                intersectionCount);

            for (var i = 0; i + 1 < intersectionCount; i += 2)
            {
                var left = (int)Math.Ceiling(
                    intersections[i] - 0.5f);

                var right = (int)Math.Ceiling(
                    intersections[i + 1] - 0.5f);

                if (right <= left)
                    continue;

                spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle(
                        left,
                        y,
                        right - left,
                        1),
                    PixelSource,
                    color);
            }
        }
    }
}