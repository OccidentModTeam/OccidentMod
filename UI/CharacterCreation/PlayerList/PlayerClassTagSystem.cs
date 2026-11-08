#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Gameplay.Classes;
using OccidentMod.Gameplay.Players;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation.PlayerList;

/// <summary>
///     在原版角色列表中加入職業名稱標籤。
///     Adds class name tags to the vanilla character selection list.
/// </summary>
/// <remarks>
///     標籤直接讀取角色存檔中的職業資料，
///     並使用原版 MagicPixel 繪製外框，無需額外圖片素材。
///     Tags read class information from player save data
///     and use the vanilla MagicPixel texture for rendering.
///     No additional image assets are required.
/// </remarks>
[Autoload(Side = ModSide.Client)]
public sealed class PlayerClassTagSystem : ModSystem
{
    private const float AdditionalItemHeight = 28f;

    private const float TagLeft = 65f;
    private const float TagTop = 60f;
    private const float TagHeight = 24f;

    private const string LocalizationPrefix =
        "Mods.OccidentMod.";

    private static readonly Color TagBackground =
        new(33, 43, 79);

    private static readonly Color TagBorder =
        new(63, 82, 151);

    /// <summary>
    ///     註冊角色列表項目的建立攔截。
    ///     Registers the character list item constructor hook.
    /// </summary>
    public override void Load()
    {
        On_UICharacterListItem.ctor += AddClassTag;
    }

    /// <summary>
    ///     移除角色列表項目的建立攔截。
    ///     Removes the character list item constructor hook.
    /// </summary>
    public override void Unload()
    {
        On_UICharacterListItem.ctor -= AddClassTag;
    }

    /// <summary>
    ///     在原版角色列表項目建立完成後附加職業標籤。
    ///     Appends a class tag after the vanilla character
    ///     list item has been constructed.
    /// </summary>
    private static void AddClassTag(
        On_UICharacterListItem.orig_ctor original,
        UICharacterListItem item,
        PlayerFileData data,
        int order)
    {
        original(item, data, order);

        // 增加獨立一行，避免標籤與原版元件重疊。
        // Add a separate row to avoid overlapping
        // vanilla character list elements.

        item.Height.Set(
            item.Height.Pixels + AdditionalItemHeight,
            item.Height.Percent);

        var tag = new ClassTag(data)
        {
            IgnoresMouseInteraction = true
        };

        tag.Left.Set(TagLeft, 0f);
        tag.Top.Set(TagTop, 0f);

        tag.Width.Set(-TagLeft, 1f);
        tag.Height.Set(TagHeight, 0f);

        item.Append(tag);
    }

    /// <summary>
    ///     使用像素繪製帶有邊框的職業標籤。
    ///     Draws a class tag with pixel-based borders.
    /// </summary>
    private static void DrawTagFrame(
        SpriteBatch spriteBatch,
        Rectangle area,
        Color fill,
        Color edge,
        int radius = 6)
    {
        if (area.Width < 4 || area.Height < 4)
            return;

        DrawRoundedFill(
            spriteBatch,
            area,
            Color.Black,
            radius);

        area.Inflate(-1, -1);

        DrawRoundedFill(
            spriteBatch,
            area,
            edge,
            radius - 1);

        area.Inflate(-1, -1);

        DrawRoundedFill(
            spriteBatch,
            area,
            fill,
            radius - 2);
    }

    /// <summary>
    ///     使用 MagicPixel 繪製像素風格的圓角矩形。
    ///     Draws a pixel-style rounded rectangle
    ///     using the vanilla MagicPixel texture.
    /// </summary>
    private static void DrawRoundedFill(
        SpriteBatch spriteBatch,
        Rectangle area,
        Color color,
        int radius)
    {
        if (area.Width <= 0 || area.Height <= 0)
            return;

        radius = Math.Clamp(
            radius,
            0,
            Math.Min(area.Width, area.Height) / 2);

        var pixel =
            TextureAssets.MagicPixel.Value;

        Rectangle center = new(
            area.X,
            area.Y + radius,
            area.Width,
            area.Height - radius * 2);

        spriteBatch.Draw(
            pixel,
            center,
            color);

        // 以整數像素階梯建立小圓角，
        // 不使用模糊效果或額外圖片。
        //
        // Create small rounded corners with integer pixels,
        // without blur effects or additional textures.

        for (var y = 0; y < radius; y++)
        {
            var distance =
                radius - y - 0.5f;

            var inset = (int)Math.Ceiling(
                radius -
                Math.Sqrt(
                    radius * radius -
                    distance * distance));

            Rectangle strip = new(
                area.X + inset,
                area.Y + y,
                area.Width - inset * 2,
                1);

            spriteBatch.Draw(
                pixel,
                strip,
                color);

            strip.Y = area.Bottom - y - 1;

            spriteBatch.Draw(
                pixel,
                strip,
                color);
        }
    }

    /// <summary>
    ///     顯示單一角色職業名稱的介面元件。
    ///     UI element that displays a character's class name.
    /// </summary>
    private sealed class ClassTag : UIElement
    {
        private readonly PlayerFileData data;

        private string caption = string.Empty;

        private string? lastClassId;
        private string? lastCulture;

        private DynamicSpriteFont? measuredFont;

        private float textWidth;

        /// <summary>
        ///     建立與角色存檔關聯的職業標籤。
        ///     Creates a class tag associated with player save data.
        /// </summary>
        public ClassTag(PlayerFileData data)
        {
            this.data = data;
        }

        /// <summary>
        ///     繪製職業標籤與本地化名稱。
        ///     Draws the class tag and localized class name.
        /// </summary>
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!TryGetClassPlayer(out var classPlayer))
                return;

            UpdateCaption(classPlayer);

            var area = GetDimensions().ToRectangle();

            if (area.Width < 32)
                return;

            var font =
                FontAssets.MouseText.Value;

            UpdateTextMeasurement(font);

            var scale = CalculateTextScale(ref area);

            DrawTagFrame(
                spriteBatch,
                area,
                TagBackground,
                TagBorder,
                4);

            DrawCaption(
                spriteBatch,
                area,
                scale);
        }

        /// <summary>
        ///     嘗試從角色存檔取得職業資料。
        ///     Attempts to retrieve class information
        ///     from the player save data.
        /// </summary>
        private bool TryGetClassPlayer(
            out ClassPlayer classPlayer)
        {
            classPlayer = null!;

            if (data.Player is not { loadStatus: 0 } player)
                return false;

            return player.TryGetModPlayer(out classPlayer);
        }

        /// <summary>
        ///     當職業或語言變更時更新顯示文字。
        ///     Updates the displayed text when the class
        ///     or active language changes.
        /// </summary>
        private void UpdateCaption(ClassPlayer classPlayer)
        {
            var culture = Language.ActiveCulture.Name;

            if (lastClassId == classPlayer.ClassId &&
                lastCulture == culture)
                return;

            caption = ResolveClassCaption(classPlayer);

            lastClassId = classPlayer.ClassId;
            lastCulture = culture;

            // 文字內容變更後重新計算寬度。
            // Recalculate width after the caption changes.

            measuredFont = null;
        }

        /// <summary>
        ///     解析職業名稱，並提供未知職業及未選職業的備援。
        ///     Resolves the localized class name with fallbacks
        ///     for unknown or unselected classes.
        /// </summary>
        private static string ResolveClassCaption(
            ClassPlayer classPlayer)
        {
            var currentClass =
                classPlayer.CurrentClass;

            string key;

            if (currentClass != ClassType.None)
                key =
                    $"{LocalizationPrefix}Classes." +
                    $"{currentClass}.DisplayName";
            else
                key = classPlayer.HasChooseClass
                    ? LocalizationPrefix + "UI.PlayerClassTag.UnknownClass"
                    : LocalizationPrefix + "UI.PlayerClassTag.NoClass";

            var result = Language.GetTextValue(key);

            // 職業翻譯缺失時使用職業代號作為備援。
            // Fall back to the enum name if class localization
            // is unavailable.

            if (result == key &&
                currentClass != ClassType.None)
                result = currentClass.ToString();

            return result;
        }

        /// <summary>
        ///     在字型或文字變更時更新快取的文字寬度。
        ///     Updates cached text width when the font
        ///     or caption changes.
        /// </summary>
        private void UpdateTextMeasurement(
            DynamicSpriteFont font)
        {
            if (ReferenceEquals(measuredFont, font))
                return;

            textWidth = font.MeasureString(caption).X;

            measuredFont = font;
        }

        /// <summary>
        ///     計算標籤寬度與文字縮放比例。
        ///     Calculates tag width and text scale.
        /// </summary>
        private float CalculateTextScale(ref Rectangle area)
        {
            area.Width = Math.Min(
                area.Width,
                (int)Math.Max(
                    64f,
                    textWidth * 0.75f + 20f));

            return Math.Min(
                0.75f,
                (area.Width - 16f) /
                Math.Max(1f, textWidth));
        }

        /// <summary>
        ///     將職業名稱置中繪製於標籤內。
        ///     Draws the class name centered inside the tag.
        /// </summary>
        private void DrawCaption(
            SpriteBatch spriteBatch,
            Rectangle area,
            float scale)
        {
            // 配合原版 UIText 的行高與基線補償。
            // Match vanilla UIText line height
            // and baseline compensation.

            var textY =
                area.Y +
                (area.Height - 16f * scale) * 0.5f -
                2f * scale;

            Vector2 position = new(
                area.Center.X,
                MathF.Round(textY));

            Utils.DrawBorderString(
                spriteBatch,
                caption,
                position,
                Color.White,
                scale,
                0.5f);
        }
    }
}