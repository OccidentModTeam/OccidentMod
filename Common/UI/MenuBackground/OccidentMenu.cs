using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace Occident.Common.UI.MenuBackground;

// Registered in tModLoader's existing menu-theme selector at the bottom of the title screen.
[Autoload(Side = ModSide.Client)]
public sealed class OccidentMenu : ModMenu
{
    private readonly OccidentMenuBackground background = new();

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoCenter,
        ref float logoRotation, ref float logoScale, ref Color drawColor)
    {
        // Keep the logo; optionally draw a replacement over the vanilla menu scenery.
        background.Draw(spriteBatch);
        // The renderer draws our transparent wordmark in the reserved upper-center space.
        // Suppress the default tModLoader logo.
        return false;
    }

    public override void Unload() => background.Unload();
}
