#nullable enable

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Presentation.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class PotentialHudSystem : ModSystem
{
    private static readonly Point HotbarPosition = new(512, 27);
    private const int FlightMeterGap = 4;
    private const int FlightMeterLift = 8;

    private HotbarHudStack stack = null!;
    private FlightMeter flightMeter = null!;
    private float potentialFraction;
    private float resonanceFraction;
    private readonly ResonanceAnimation resonanceAnimation = new();
    private readonly PotentialFullAnimation fullAnimation = new();

    public override void Load()
    {
        var bar = new EnergyBar(Mod, "PotentialBar", "PotentialBarLightning", new Vector2(6, 10), new Vector2(-2, 0), 34);
        var resonanceBar = new EnergyBar(Mod, "ResonanceBar", "ResonanceBar_Activate", new Vector2(4, 10),
            new Vector2(-20, 0), 38);
        stack = new HotbarHudStack(HotbarPosition);
        stack.AddAbove(bar.Size, (spriteBatch, position) =>
            bar.Draw(spriteBatch, position, potentialFraction, fullAnimation.FrameIndex));

        stack.AddAbove(resonanceBar.Size, (spriteBatch, position) =>
            resonanceBar.Draw(spriteBatch, position, resonanceFraction, resonanceAnimation.FrameIndex));

        flightMeter = new FlightMeter(Mod);
    }

    public override void OnWorldLoad()
    {
        ResetHud();
    }

    public override void OnWorldUnload() => ResetHud();

    private void ResetHud()
    {
        potentialFraction = 0f;
        resonanceFraction = 0f;
        resonanceAnimation.Reset();
        fullAnimation.Reset();
        flightMeter.Reset();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        double elapsed = gameTime.ElapsedGameTime.TotalSeconds;

        if (Main.gameMenu || !Main.LocalPlayer.active || Main.LocalPlayer.dead || Main.LocalPlayer.ghost)
        {
            ResetHud();
            return;
        }

        var potential = Main.LocalPlayer.GetModPlayer<PotentialChargePlayer>().Potential;
        potentialFraction = potential.Current / potential.Maximum;
        fullAnimation.Update(potential.IsFull, potential.IsActive, elapsed);

        var resonance = Main.LocalPlayer.GetModPlayer<ResonancePreviewPlayer>();
        resonanceFraction = resonance.Fraction;
        resonanceAnimation.Update(resonance.IsFull, elapsed);

        var flight = FlightStatus.Read(Main.LocalPlayer);
        flightMeter.Update(elapsed, flight.Remaining, flight.Maximum, flight.IsInfinite, Main.LocalPlayer.controlJump);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Hotbar");
        if (index >= 0)
        {
            layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                "VesperMod: Hotbar HUD",
                DrawHotbarHud,
                InterfaceScaleType.UI));
        }

        int flightIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
        if (flightIndex >= 0)
        {
            layers.Insert(flightIndex, new LegacyGameInterfaceLayer(
                "VesperMod: Flight Meter",
                DrawFlightMeter,
                InterfaceScaleType.None));
        }
    }

    private bool DrawFlightMeter()
    {
        if (CanShowPreview() && flightMeter.IsVisible)
        {
            Player player = Main.LocalPlayer;
            var rightCenter = new Vector2(player.Right.X, player.Center.Y);
            var screenAnchor = Vector2.Transform(
                rightCenter - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);

            // Project the player through the camera while keeping the meter upright and pixel-aligned.
            var position = new Vector2(
                System.MathF.Round(screenAnchor.X) + FlightMeterGap,
                System.MathF.Round(screenAnchor.Y) - flightMeter.Size.Y / 2 - FlightMeterLift);
            flightMeter.Draw(Main.spriteBatch, position);
        }

        return true;
    }


    private bool DrawHotbarHud()
    {
        if (CanShowPreview())
            stack.Draw(Main.spriteBatch);

        return true;
    }

    private static bool CanShowPreview() =>
        !Main.gameMenu && !Main.hideUI && !Main.mapFullscreen && !Main.playerInventory &&
        Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost;
}
