using System;
using Microsoft.Xna.Framework;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Presentation.UI;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class HorizontalResourceOverlay : ModResourceOverlay
{
    private const int Top = 12;
    private const int RowGap = 2;
    private const int RightPadding = 20;
    private const int StaminaRightInset = 20;
    private const int ManaTopWithFullText = 87;

    private LegacyResourceBars bars = null!;

    private int CompactStaminaTop => Top + bars.Life.Height + RowGap;

    public override void Load() => bars = new LegacyResourceBars(Mod);

    private int ManaTop(string configKey) => configKey == "HorizontalBarsWithFullText"
        ? ManaTopWithFullText
        : CompactStaminaTop + bars.Stamina.Height + RowGap;

    private int StaminaTop(string configKey) => configKey == "HorizontalBarsWithFullText"
        ? ManaTop(configKey) + bars.Mana.Height + RowGap
        : CompactStaminaTop;

    internal int Bottom(string configKey) =>
        Math.Max(ManaTop(configKey) + bars.Mana.Height, StaminaTop(configKey) + bars.Stamina.Height);

    public override bool PreDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet,
        bool drawingLife, ref Color textColor, out bool drawText)
    {
        drawText = true;
        if (displaySet is not HorizontalBarsPlayerResourcesDisplaySet)
            return true;

        var player = Main.LocalPlayer;
        var appearance = player.GetModPlayer<ResourceBarAppearancePlayer>();
        var row = drawingLife ? bars.Life : bars.Mana;
        int segments = Math.Clamp(drawingLife ? snapshot.AmountOfLifeHearts : snapshot.AmountOfManaStars, 1, 20);
        int upgraded = drawingLife ? Math.Clamp(snapshot.LifeFruitCount, 0, segments) : appearance.ManaUpgraded ? segments : 0;
        int current = drawingLife ? snapshot.Life : snapshot.Mana;
        int maximum = drawingLife ? snapshot.LifeMax : snapshot.ManaMax;
        int right = Main.screenWidth - RightPadding;
        var bounds = row.GetBounds(right, drawingLife ? Top : ManaTop(displaySet.ConfigKey), segments);
        row.Draw(Main.spriteBatch, bounds, segments, current, maximum, upgraded);

        bool showHover = !Main.ingameOptionsWindow && !player.ghost;
        if (showHover && bounds.Contains(Main.MouseScreen.ToPoint()))
        {
            string label = Lang.inter[drawingLife ? 0 : 2].Value;
            Main.instance.MouseText($"{label} {current}/{maximum}");
        }

        if (!drawingLife && player.active && !player.dead && !player.ghost)
        {
            var stamina = player.GetModPlayer<StaminaPlayer>().Stamina;
            int staminaSegments = LegacyResourceBars.StaminaSegments(stamina.Maximum);
            var staminaBounds = bars.Stamina.GetBounds(right - StaminaRightInset, StaminaTop(displaySet.ConfigKey), staminaSegments);
            bars.Stamina.Draw(Main.spriteBatch, staminaBounds, staminaSegments, stamina.Current, stamina.Maximum,
                appearance.StaminaUpgraded ? staminaSegments : 0);

            if (showHover && staminaBounds.Contains(Main.MouseScreen.ToPoint()))
                Main.instance.MouseText(Mod.GetLocalization("UI.Stamina.Value").Format(stamina.Current, stamina.Maximum));
        }

        return false;
    }
}
