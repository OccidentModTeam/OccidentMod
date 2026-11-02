#nullable enable

using System;
using Occident.Common.Classes;
using Occident.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace Occident.Common.UI.PlayerList;

[Autoload(Side = ModSide.Client)]
public sealed class PlayerClassTagSystem : ModSystem
{
    public override void Load() => On_UICharacterListItem.ctor += AddClassTag;
    public override void Unload() => On_UICharacterListItem.ctor -= AddClassTag;

    private static void AddClassTag(On_UICharacterListItem.orig_ctor original,
        UICharacterListItem item, PlayerFileData data, int order)
    {
        original(item, data, order);
        // 新增獨立一行，避開長角色名稱、模組警告和底部原生操作按鈕。
        item.Height.Set(item.Height.Pixels + 28f, item.Height.Percent);
        var tag = new ClassTag(data) { IgnoresMouseInteraction = true };
        tag.Left.Set(65f, 0f);
        tag.Top.Set(60f, 0f);
        tag.Width.Set(-65f, 1f);
        tag.Height.Set(24f, 0f);
        item.Append(tag);
    }

    private sealed class ClassTag(PlayerFileData data) : UIElement
    {
        private string? lastClassID, lastCulture;
        private string caption = "";
        private float textWidth;
        private ReLogic.Graphics.DynamicSpriteFont? measuredFont;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (data.Player is not { loadStatus: 0 } player ||
                !player.TryGetModPlayer<ClassPlayer>(out var classPlayer))
                return;

            string culture = Language.ActiveCulture.Name;
            if (lastClassID != classPlayer.ClassID || lastCulture != culture)
            {
                ClassType currentClass = classPlayer.CurrentClass;
                string key = currentClass != ClassType.None
                    ? $"Mods.Occident.Classes.{currentClass}.DisplayName"
                    : classPlayer.hasChooseClass ? "Mods.Occident.UI.PlayerClassTag.UnknownClass"
                    : "Mods.Occident.UI.PlayerClassTag.NoClass";
                caption = Language.GetTextValue(key);
                if (caption == key && currentClass != ClassType.None)
                    caption = currentClass.ToString();
                lastClassID = classPlayer.ClassID;
                lastCulture = culture;
                measuredFont = null;
            }

            var area = GetDimensions().ToRectangle();
            if (area.Width < 32)
                return;
            var font = FontAssets.MouseText.Value;
            if (!ReferenceEquals(measuredFont, font))
            {
                textWidth = font.MeasureString(caption).X;
                measuredFont = font;
            }
            area.Width = Math.Min(area.Width, (int)Math.Max(64f, textWidth * 0.75f + 20f));
            float scale = Math.Min(0.75f, (area.Width - 16f) / Math.Max(1f, textWidth));
            DrawTagFrame(spriteBatch, area, new Color(33, 43, 79), new Color(63, 82, 151), 4);
            // 比照原生 UIText 的 16 單位行高及基線補償，避免字型額外行距將中文字推高。
            float textY = area.Y + (area.Height - 16f * scale) * 0.5f - 2f * scale;
            Utils.DrawBorderString(spriteBatch, caption, new Vector2(area.Center.X, MathF.Round(textY)),
                Color.White, scale, 0.5f, 0f);
        }
    }
    private static void DrawTagFrame(SpriteBatch batch, Rectangle area, Color fill, Color edge, int radius = 6)
    {
        if (area.Width < 4 || area.Height < 4)
            return;
        RoundedFill(batch, area, Color.Black, radius);
        area.Inflate(-1, -1);
        RoundedFill(batch, area, edge, radius - 1);
        area.Inflate(-1, -1);
        RoundedFill(batch, area, fill, radius - 2);
    }

    private static void RoundedFill(SpriteBatch batch, Rectangle area, Color color, int radius)
    {
        if (area.Width <= 0 || area.Height <= 0)
            return;
        radius = Math.Clamp(radius, 0, Math.Min(area.Width, area.Height) / 2);
        var pixel = TextureAssets.MagicPixel.Value;
        batch.Draw(pixel, new Rectangle(area.X, area.Y + radius, area.Width, area.Height - radius * 2), color);
        // 以整數像素階梯形成小圓角，維持像素邊緣，不使用模糊或額外素材。
        for (int y = 0; y < radius; y++)
        {
            float distance = radius - y - 0.5f;
            int inset = (int)Math.Ceiling(radius - Math.Sqrt(radius * radius - distance * distance));
            var strip = new Rectangle(area.X + inset, area.Y + y, area.Width - inset * 2, 1);
            batch.Draw(pixel, strip, color);
            strip.Y = area.Bottom - y - 1;
            batch.Draw(pixel, strip, color);
        }
    }
}

