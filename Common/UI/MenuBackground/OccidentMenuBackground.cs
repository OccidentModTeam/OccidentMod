using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Occident.Common.UI.MenuBackground;

internal sealed class OccidentMenuBackground
{
    private const string AssetFolder = "Occident/Assets/UI/MenuBackground/";
    // Optional replacement image. When absent, leave the vanilla menu scenery visible.
    private const string BackgroundAssetPath = AssetFolder + "CustomBackground";
    private Asset<Texture2D> background;
    private Asset<Texture2D> logo;
    private bool backgroundChecked;

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!backgroundChecked)
        {
            if (ModContent.HasAsset(BackgroundAssetPath))
                background = ModContent.Request<Texture2D>(BackgroundAssetPath, AssetRequestMode.ImmediateLoad);

            backgroundChecked = true;
        }
        logo ??= ModContent.Request<Texture2D>(AssetFolder + "OccidentLogo", AssetRequestMode.ImmediateLoad);

        spriteBatch.End();
        // The transparent logo uses straight alpha. Nothing in this theme animates.
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        try
        {
            if (background != null)
                DrawBackground(spriteBatch, background.Value);
            DrawLogo(spriteBatch);
        }
        finally
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        }
    }

    private static void DrawBackground(SpriteBatch spriteBatch, Texture2D texture)
    {
        // Cover the menu while preserving the image's aspect ratio; apply UI scaling once.
        float scale = Math.Max(Main.screenWidth / (float)texture.Width, Main.screenHeight / (float)texture.Height);
        float width = texture.Width * scale;
        float height = texture.Height * scale;
        Rectangle bounds = Bounds((Main.screenWidth - width) * 0.5f,
            (Main.screenHeight - height) * 0.5f, width, height);
        spriteBatch.Draw(texture, bounds, Color.White);
    }

    private void DrawLogo(SpriteBatch spriteBatch)
    {
        Texture2D texture = logo.Value;
        float maxWidth = Math.Min(560f, Main.screenWidth * 0.34f);
        float maxHeight = Math.Min(210f, Main.screenHeight * 0.25f);
        float scale = Math.Min(maxWidth / texture.Width, maxHeight / texture.Height);
        float width = texture.Width * scale;
        float height = texture.Height * scale;
        spriteBatch.Draw(texture, Bounds((Main.screenWidth - width) * 0.5f, 12f, width, height), Color.White);
    }

    private static Rectangle Bounds(float left, float top, float width, float height)
    {
        int x = (int)Math.Round(left);
        int y = (int)Math.Round(top);
        return new Rectangle(x, y, (int)Math.Round(left + width) - x, (int)Math.Round(top + height) - y);
    }

    public void Unload()
    {
        // ModContent owns and disposes the assets.
        background = null;
        logo = null;
        backgroundChecked = false;
    }
}
