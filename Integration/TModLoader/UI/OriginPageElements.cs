using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace VesperMod.Integration.TModLoader.UI;

    internal sealed class OriginClassPicker : UIElement
    {
    private const int CardsPerRow = 4;
    private const float RowHeight = 62f;
    private const float RowTravel = RowHeight + 2f;
    private const float SlideDuration = 0.18f;

    private readonly UIElement[] rows = { new(), new() };
    private readonly ClassPageButton pageButton = new();
    private int visiblePage;
    private float slideTime = SlideDuration;

    public OriginClassPicker(int selectedClassIndex)
    {
        visiblePage = selectedClassIndex >= CardsPerRow ? 1 : 0;
        Width.Set(0f, 1f);
        Height.Set(RowHeight, 0f);

        var viewport = new UIElement { OverflowHidden = true };
        viewport.Width.Set(-36f, 1f);
        viewport.Height.Set(0f, 1f);
        Append(viewport);

        foreach (var row in rows)
        {
            row.Width.Set(0f, 1f);
            row.Height.Set(RowHeight, 0f);
            viewport.Append(row);
        }

        pageButton.Width.Set(28f, 0f);
        pageButton.Height.Set(RowHeight - 4f, 0f);
        pageButton.Left.Set(-31f, 1f);
        pageButton.Top.Set(2f, 0f);
        pageButton.OnLeftClick += (_, _) =>
        {
            if (slideTime < SlideDuration)
                return;

            visiblePage = 1 - visiblePage;
            slideTime = 0f;
            PositionRows();
        };
        Append(pageButton);
        PositionRows();
    }

    public void AddCard(UIPanel card, int index)
    {
        card.Width.Set(-6f, 1f / CardsPerRow);
        card.Height.Set(RowHeight - 4f, 0f);
        card.Left.Set(3f, (index % CardsPerRow) / (float)CardsPerRow);
        card.Top.Set(2f, 0f);
        rows[index / CardsPerRow].Append(card);
    }

    public override void Update(GameTime gameTime)
    {
        if (slideTime < SlideDuration)
        {
            slideTime = Math.Min(SlideDuration, slideTime + (float)gameTime.ElapsedGameTime.TotalSeconds);
            PositionRows();
        }

        base.Update(gameTime);
    }

    private void PositionRows()
    {
        float progress = slideTime / SlideDuration;
        float eased = MathHelper.SmoothStep(0f, 1f, progress);
        float pageOffset = visiblePage == 0 ? 1f - eased : eased;

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            row.IgnoresMouseInteraction = slideTime < SlideDuration || i != visiblePage;
            row.Top.Set(MathF.Round((i - pageOffset) * RowTravel), 0f);
            row.Recalculate();
        }

        pageButton.VisiblePage = visiblePage;
    }

    private sealed class ClassPageButton : UIPanel
    {
        public int VisiblePage { get; set; }

        public ClassPageButton()
        {
            SetPadding(0f);
            BackgroundColor = new Color(36, 45, 74);
            BorderColor = OriginPage.CardBorder;
            OriginPage.AddCardHover(this);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            var bounds = GetDimensions();
            int center = (int)(bounds.X + bounds.Width / 2f);
            int arrowTop = (int)(bounds.Y + bounds.Height / 2f) - 8;
            var pixel = TextureAssets.MagicPixel.Value;
            var arrowColor = IsMouseHovering ? OriginPage.SectionAccent : new Color(208, 215, 232);

            for (int step = 0; step < 3; step++)
            {
                int halfWidth = VisiblePage == 0 ? 2 - step : step;
                int y = arrowTop + step * 2;
                spriteBatch.Draw(pixel, new Rectangle(center - 1 - halfWidth * 2, y, 2, 2), arrowColor);
                spriteBatch.Draw(pixel, new Rectangle(center - 1 + halfWidth * 2, y, 2, 2), arrowColor);
            }

            int dotsY = arrowTop + 15;
            var inactive = new Color(99, 112, 145);
            spriteBatch.Draw(pixel, new Rectangle(center - 5, dotsY, 3, 3), VisiblePage == 0 ? OriginPage.SectionAccent : inactive);
            spriteBatch.Draw(pixel, new Rectangle(center + 2, dotsY, 3, 3), VisiblePage == 1 ? OriginPage.SectionAccent : inactive);
        }
    }
    }

    internal sealed class OriginDetails : UIPanel
    {
    private const float DividerPosition = 0.46f;

    private readonly UIText[] summary;
    private readonly UIText[] lore;

    public OriginDetails(UIText[] summary, UIText[] lore)
    {
        this.summary = summary;
        this.lore = lore;
        Width.Set(-6f, 1f);
        Left.Set(3f, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(25, 31, 52);
        BorderColor = OriginPage.CardBorder;

        foreach (var text in summary)
            AddColumnText(text, 0f);

        foreach (var text in lore)
            AddColumnText(text, DividerPosition);
    }

    private void AddColumnText(UIText text, float start)
    {
        text.Left.Set(12f, start);
        text.Width.Set(-24f, start == 0f ? DividerPosition : 1f - DividerPosition);
        Append(text);
    }

    public override void Recalculate()
    {
        base.Recalculate();

        // UIText wraps using its previously calculated width.
        foreach (var text in summary)
            text.Recalculate();
        foreach (var text in lore)
            text.Recalculate();

        float height = Math.Max(Stack(summary, 1), Stack(lore, 1)) + 9f;
        Height.Set(height, 0f);
        base.Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var bounds = GetInnerDimensions();
        var divider = new Rectangle(
            (int)(bounds.X + bounds.Width * DividerPosition),
            (int)bounds.Y + 9,
            1,
            Math.Max(0, (int)bounds.Height - 18));
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, divider, OriginPage.CardBorder);
    }

    private static float Stack(UIText[] column, int breakAfter)
    {
        float top = 9f;

        for (int i = 0; i < column.Length; i++)
        {
            var text = column[i];
            text.Top.Set(top, 0f);
            top += text.GetOuterDimensions().Height;

            if (i < column.Length - 1)
                top += i == breakAfter ? 5f : 1f;
        }

        return top;
    }
    }
