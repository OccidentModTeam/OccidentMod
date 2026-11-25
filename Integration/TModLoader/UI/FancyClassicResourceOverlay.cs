using System;
using Microsoft.Xna.Framework;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Presentation.UI;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class FancyClassicResourceOverlay : ModResourceOverlay
{
    private const int ScreenInset = 12;
    private const int HeartToManaGap = 8;
    internal const int ManaTop = 22;

    internal FancyClassicHud Hud { get; private set; } = null!;

    public override void Load() => Hud = new FancyClassicHud(Mod);

    internal static int LifeRight(int screenWidth) =>
        screenWidth - ScreenInset - 2 * FancyClassicHud.ManaColumnSpacing - HeartToManaGap;

    internal static int ManaRight(int screenWidth) => screenWidth - ScreenInset;

    internal static int LifeTop(string configKey) => configKey == "NewWithText" ? 28 : 16;

    internal static int LifeSlots(PlayerStatsSnapshot snapshot) => Math.Clamp(snapshot.AmountOfLifeHearts, 1, 20);

    public override bool PreDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet,
        bool drawingLife, ref Color textColor, out bool drawText)
    {
        drawText = true;
        if (displaySet is not FancyClassicPlayerResourcesDisplaySet)
            return true;

        // Keep the vanilla style selection and slot roles while anchoring whole pixels from the right.
        Rectangle bounds;
        int current;
        int maximum;
        string label;
        if (drawingLife)
        {
            current = snapshot.Life;
            maximum = snapshot.LifeMax;
            label = Lang.inter[0].Value;
            bounds = Hud.DrawLife(Main.spriteBatch, LifeRight(Main.screenWidth), LifeTop(displaySet.ConfigKey),
                LifeSlots(snapshot), current, maximum, snapshot.LifeFruitCount);
            if (displaySet.ConfigKey == "NewWithText")
                Utils.DrawBorderString(Main.spriteBatch, $"{label} {current}/{maximum}",
                    new Vector2(bounds.Right, 4), textColor, anchorx: 1f);
        }
        else
        {
            current = snapshot.Mana;
            maximum = snapshot.ManaMax;
            label = Lang.inter[2].Value;
            bool silver = Main.LocalPlayer.GetModPlayer<ResourceBarAppearancePlayer>().ManaUpgraded;
            bounds = Hud.DrawMana(Main.spriteBatch, ManaRight(Main.screenWidth), ManaTop,
                Math.Clamp(snapshot.AmountOfManaStars, 0, 20), current, maximum, silver);
        }

        if (!Main.ingameOptionsWindow && !Main.LocalPlayer.ghost &&
            bounds.Contains(Main.MouseScreen.ToPoint()))
        {
            Main.instance.MouseText($"{label} {current}/{maximum}");
        }

        return false;
    }
}
