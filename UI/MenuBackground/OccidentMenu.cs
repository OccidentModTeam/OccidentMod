#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace OccidentMod.UI.MenuBackground;

/// <summary>
///     註冊《輓歌》的自訂主選單主題。
///     Registers the custom Occident main menu theme.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class OccidentMenu : ModMenu
{
    private readonly OccidentMenuBackground background = new();

    /// <summary>
    ///     主選單主題名稱。
    ///     Main menu theme display name.
    /// </summary>
    public override string DisplayName => "輓歌 Occident";

    /// <summary>
    ///     繪製自訂背景與模組標誌。
    ///     Draw the custom background and mod logo.
    /// </summary>
    public override bool PreDrawLogo(
        SpriteBatch spriteBatch,
        ref Vector2 logoCenter,
        ref float logoRotation,
        ref float logoScale,
        ref Color drawColor)
    {
        background.Draw(spriteBatch);

        // 背景系統已自行繪製 Logo，
        // 不再繪製原版主選單標誌。
        //
        // The custom renderer draws the logo,
        // so skip the vanilla logo.
        return false;
    }

    /// <summary>
    ///     清理背景與標誌素材參照。
    ///     Clear cached menu asset references.
    /// </summary>
    public override void Unload()
    {
        background.Unload();
    }
}