using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.UI.MenuBackground;

/// <summary>
///     管理主選單背景與模組標誌的載入及繪製。
///     Manages loading and rendering of the main menu
///     background and mod logo.
/// </summary>
/// <remarks>
///     僅使用一張必要標誌與一張可選背景。
///     素材由 ModContent 管理，此類別只快取參照。
///     Uses one required logo and one optional background.
///     Assets are owned by ModContent; this class only
///     caches references to them.
/// </remarks>
internal sealed class OccidentMenuBackground
{
    private const string AssetFolder =
        "OccidentMod/Assets/UI/MenuBackground/";

    private const string LogoAssetPath =
        AssetFolder + "OccidentLogo";

    private const string BackgroundAssetPath =
        AssetFolder + "CustomBackground";

    private const float LogoMaxWidth = 560f;
    private const float LogoMaxHeight = 210f;

    private const float LogoWidthRatio = 0.34f;
    private const float LogoHeightRatio = 0.25f;

    private const float LogoTop = 12f;

    private Asset<Texture2D> background;

    private bool backgroundChecked;
    private Asset<Texture2D> logo;

    /// <summary>
    ///     載入必要素材並繪製主選單。
    ///     Loads required assets and renders the main menu.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        EnsureAssetsLoaded();

        BeginCustomRendering(spriteBatch);

        try
        {
            if (background != null)
                DrawBackground(
                    spriteBatch,
                    background.Value);

            DrawLogo(spriteBatch);
        }
        finally
        {
            RestoreRendering(spriteBatch);
        }
    }

    /// <summary>
    ///     延遲載入標誌，並且只檢查一次可選背景。
    ///     Lazily loads the logo and checks the optional
    ///     background asset only once.
    /// </summary>
    private void EnsureAssetsLoaded()
    {
        if (!backgroundChecked)
        {
            if (ModContent.HasAsset(BackgroundAssetPath))
                background = ModContent.Request<Texture2D>(
                    BackgroundAssetPath,
                    AssetRequestMode.ImmediateLoad);

            backgroundChecked = true;
        }

        logo ??= ModContent.Request<Texture2D>(
            LogoAssetPath,
            AssetRequestMode.ImmediateLoad);
    }

    /// <summary>
    ///     切換至支援直通透明度的繪製模式。
    ///     Switches to non-premultiplied alpha blending.
    /// </summary>
    private static void BeginCustomRendering(
        SpriteBatch spriteBatch)
    {
        spriteBatch.End();

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            Main.Rasterizer,
            null,
            Main.UIScaleMatrix);
    }

    /// <summary>
    ///     還原原本選單使用的透明混合模式。
    ///     Restores the menu's alpha blending configuration.
    /// </summary>
    private static void RestoreRendering(
        SpriteBatch spriteBatch)
    {
        spriteBatch.End();

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            Main.Rasterizer,
            null,
            Main.UIScaleMatrix);
    }

    /// <summary>
    ///     等比例放大背景，使圖片完整覆蓋主選單畫面。
    ///     Scales the background proportionally to cover
    ///     the entire main menu screen.
    /// </summary>
    private static void DrawBackground(
        SpriteBatch spriteBatch,
        Texture2D texture)
    {
        var scale = Math.Max(
            Main.screenWidth / (float)texture.Width,
            Main.screenHeight / (float)texture.Height);

        var width = texture.Width * scale;
        var height = texture.Height * scale;

        var left =
            (Main.screenWidth - width) * 0.5f;

        var top =
            (Main.screenHeight - height) * 0.5f;

        var bounds = CreateBounds(
            left,
            top,
            width,
            height);

        spriteBatch.Draw(
            texture,
            bounds,
            Color.White);
    }

    /// <summary>
    ///     在主選單上方中央繪製模組標誌。
    ///     Draws the mod logo at the top center
    ///     of the main menu.
    /// </summary>
    private void DrawLogo(SpriteBatch spriteBatch)
    {
        var texture = logo.Value;

        var maxWidth = Math.Min(
            LogoMaxWidth,
            Main.screenWidth * LogoWidthRatio);

        var maxHeight = Math.Min(
            LogoMaxHeight,
            Main.screenHeight * LogoHeightRatio);

        var scale = Math.Min(
            maxWidth / texture.Width,
            maxHeight / texture.Height);

        var width = texture.Width * scale;
        var height = texture.Height * scale;

        var left =
            (Main.screenWidth - width) * 0.5f;

        var bounds = CreateBounds(
            left,
            LogoTop,
            width,
            height);

        spriteBatch.Draw(
            texture,
            bounds,
            Color.White);
    }

    /// <summary>
    ///     將浮點座標轉換為整數矩形，維持原始邊界計算方式。
    ///     Converts floating-point coordinates into an
    ///     integer rectangle using the original rounding behavior.
    /// </summary>
    private static Rectangle CreateBounds(
        float left,
        float top,
        float width,
        float height)
    {
        var x = (int)Math.Round(left);
        var y = (int)Math.Round(top);

        var right =
            (int)Math.Round(left + width);

        var bottom =
            (int)Math.Round(top + height);

        return new Rectangle(
            x,
            y,
            right - x,
            bottom - y);
    }

    /// <summary>
    ///     清除已快取的素材參照與載入狀態。
    ///     Clears cached asset references and loading state.
    /// </summary>
    /// <remarks>
    ///     不直接釋放 Texture2D，因為素材由 ModContent 管理。
    ///     Does not dispose Texture2D directly because
    ///     asset lifetime is managed by ModContent.
    /// </remarks>
    public void Unload()
    {
        background = null;
        logo = null;

        backgroundChecked = false;
    }
}