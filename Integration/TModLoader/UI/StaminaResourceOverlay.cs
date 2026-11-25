using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class StaminaResourceOverlay : ModResourceOverlay
{
    public override void PostDrawResourceDisplay(
        PlayerStatsSnapshot snapshot,
        IPlayerResourcesDisplaySet displaySet,
        bool drawingLife,
        Color textColor,
        bool drawText)
    {
        // IngameOptions draws the resource display directly and stops the HUD layers.
        if (drawingLife && Main.ingameOptionsWindow)
            ModContent.GetInstance<StaminaHudSystem>().DrawSettingsPreview(displaySet, snapshot);
    }
}
