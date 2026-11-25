#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Presentation.UI;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;
using Terraria.UI;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class StaminaHudSystem : ModSystem
{
    private const string ResourceLayer = "Vanilla: Resource Bars";
    private const int MinimapFrameClearance = 12;

    public override void Load() => IL_Main.DrawMap += ReserveMinimapSpace;

    public override void Unload()
    {
        IL_Main.DrawMap -= ReserveMinimapSpace;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == ResourceLayer);
        if (index >= 0)
        {
            layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                "VesperMod: Stamina",
                DrawStamina,
                InterfaceScaleType.UI));
        }
    }

    private bool DrawStamina()
    {
        if (!Main.ingameOptionsWindow && CanDraw() &&
            TryGetLayout(Main.ResourceSetsManager.ActiveSet, new PlayerStatsSnapshot(Main.LocalPlayer), out var layout))
        {
            Draw(layout, showHoverText: true);
        }

        return true;
    }

    internal void DrawSettingsPreview(IPlayerResourcesDisplaySet display, PlayerStatsSnapshot snapshot)
    {
        if (Main.ingameOptionsWindow && CanDraw() && TryGetLayout(display, snapshot, out var layout))
            Draw(layout, showHoverText: false);
    }

    private void Draw(StaminaLayout layout, bool showHoverText)
    {
        Rectangle bounds = layout.Bounds;
        var stamina = Main.LocalPlayer.GetModPlayer<StaminaPlayer>().Stamina;
        var hud = ModContent.GetInstance<FancyClassicResourceOverlay>().Hud;
        bool jade = Main.LocalPlayer.GetModPlayer<ResourceBarAppearancePlayer>().StaminaUpgraded;
        hud.DrawStamina(Main.spriteBatch, bounds, stamina.Current, stamina.Maximum, jade, layout.ShowFrame);
        if (showHoverText && bounds.Contains(Main.MouseScreen.ToPoint()))
        {
            var text = Mod.GetLocalization("UI.Stamina.Value");
            Main.instance.MouseText(text.Format(stamina.Current, stamina.Maximum));
        }
    }

    private static bool CanDraw()
    {
        if (Main.gameMenu || Main.hideUI || Main.mapFullscreen)
            return false;

        Player player = Main.LocalPlayer;
        return player.active && !player.dead && !player.ghost;
    }

    private bool TryGetLayout(IPlayerResourcesDisplaySet display, PlayerStatsSnapshot snapshot, out StaminaLayout layout)
    {
        layout = default;
        var hud = ModContent.GetInstance<FancyClassicResourceOverlay>().Hud;
        int maximum = Main.LocalPlayer.GetModPlayer<StaminaPlayer>().Stamina.Maximum;
        bool hasSecondLifeRow = snapshot.AmountOfLifeHearts > FancyClassicHud.SlotsPerRow;
        switch (display)
        {
            case FancyClassicPlayerResourcesDisplaySet:
                int lifeTop = FancyClassicResourceOverlay.LifeTop(display.ConfigKey);
                int lifeBottom = hud.LifeBottom(lifeTop, FancyClassicResourceOverlay.LifeSlots(snapshot));
                int fancyRight = FancyClassicResourceOverlay.LifeRight(Main.screenWidth);
                layout = new StaminaLayout(hud.StaminaBounds(fancyRight, lifeBottom + 4, maximum), ShowFrame: true);
                return true;
            case ClassicPlayerResourcesDisplaySet:
                layout = new StaminaLayout(hud.StaminaBounds(Main.screenWidth - 170,
                    hasSecondLifeRow ? 88 : 62, maximum), ShowFrame: false);
                return true;
            default:
                return false;
        }
    }

    private void ReserveMinimapSpace(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before,
                instruction => instruction.MatchLdsfld<Main>("_minimapTopRightAnchorOffsetTowardsBottom"),
                instruction => instruction.MatchStsfld<Main>(nameof(Main.miniMapY))))
        {
            Mod.Logger.Warn("Stamina HUD could not reserve space above the minimap.");
            return;
        }

        // Adjust the vanilla anchor before map drawing and input apply their scaling.
        cursor.Index++;
        cursor.EmitDelegate<Func<int, int>>(top =>
        {
            if (!CanDraw())
                return top;

            var display = Main.ResourceSetsManager.ActiveSet;
            if (display is HorizontalBarsPlayerResourcesDisplaySet)
            {
                var horizontal = ModContent.GetInstance<HorizontalResourceOverlay>();
                return Math.Max(top, horizontal.Bottom(display.ConfigKey) + MinimapFrameClearance);
            }

            var snapshot = new PlayerStatsSnapshot(Main.LocalPlayer);
            if (!TryGetLayout(display, snapshot, out var layout))
                return top;

            int bottom = layout.Bottom;
            int starsPerColumn = FancyClassicHud.SlotsPerRow;
            if (display is FancyClassicPlayerResourcesDisplaySet && snapshot.AmountOfManaStars > starsPerColumn)
            {
                var hud = ModContent.GetInstance<FancyClassicResourceOverlay>().Hud;
                int leftColumnSlots = Math.Min(snapshot.AmountOfManaStars - starsPerColumn, starsPerColumn);
                bottom = Math.Max(bottom,
                    hud.ManaColumnBottom(FancyClassicResourceOverlay.ManaTop, leftColumnSlots));
            }
            return Math.Max(top, bottom + MinimapFrameClearance);
        });
    }

    private readonly record struct StaminaLayout(Rectangle Bounds, bool ShowFrame)
    {
        public int Bottom => Bounds.Bottom;
    }
}
