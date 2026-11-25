#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace VesperMod.Presentation.UI;

internal sealed class FlightMeter
{
    private const int SourcePixelSize = 2;
    private static readonly Vector2 BarInset = new(10, 10);
    private static readonly Vector2 EnergyInset = new(10, 14);
    private static readonly Point EffectSize = new(44, 82);

    private readonly Asset<Texture2D> frame;
    private readonly Asset<Texture2D> energy;
    private readonly Asset<Texture2D> refillEffect;
    private readonly Asset<Texture2D> infiniteEffect;
    private readonly FlightMeterAnimation animation = new();

    private float fraction;
    private bool hasFlight;
    private float previousRemaining;
    private int previousMaximum;
    private bool flightStarted;

    public Point Size => EffectSize;
    public bool IsVisible => (hasFlight && flightStarted) || animation.UsesLoop;

    public FlightMeter(Mod mod)
    {
        const string folder = "Assets/UI/EnergyBar/";
        frame = mod.Assets.Request<Texture2D>(folder + "FlightMeterBar");
        energy = mod.Assets.Request<Texture2D>(folder + "FlightMeterBar_Energy");
        refillEffect = mod.Assets.Request<Texture2D>(folder + "FlightMeterInfinite");
        infiniteEffect = mod.Assets.Request<Texture2D>(folder + "FlightMeterInfinite_loop");
    }

    public void Reset()
    {
        hasFlight = false;
        fraction = 0;
        previousRemaining = 0;
        previousMaximum = 0;
        flightStarted = false;
        animation.Reset();
    }

    public void Update(double elapsed, float remaining, int maximum, bool infinite, bool jumpHeld)
    {
        hasFlight = maximum > 0;
        float available = hasFlight ? Math.Clamp(remaining, 0f, maximum) : 0f;
        fraction = hasFlight ? available / maximum : 0f;
        bool full = hasFlight && available >= maximum;
        bool spentFlightTime = jumpHeld && hasFlight && maximum == previousMaximum && available < previousRemaining;
        flightStarted = hasFlight && (flightStarted || spentFlightTime || infinite);

        animation.Update(elapsed, hasFlight, full, hasFlight && infinite);
        if (full && !animation.IsAnimating)
            flightStarted = false;

        previousRemaining = available;
        previousMaximum = maximum;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        bool usesLoop = animation.UsesLoop;
        float opacity = animation.LoopOpacity;
        float fill = usesLoop ? MathHelper.Lerp(fraction, 1f, opacity) : fraction;
        var barPosition = position + BarInset;
        spriteBatch.Draw(frame.Value, barPosition, hasFlight ? Color.White : Color.White * opacity);

        Texture2D fillTexture = energy.Value;
        int rows = fillTexture.Height / SourcePixelSize;
        int visibleHeight = (int)MathF.Ceiling(fill * rows) * SourcePixelSize;
        if (visibleHeight > 0)
        {
            int hiddenHeight = fillTexture.Height - visibleHeight;
            var source = new Rectangle(0, hiddenHeight, fillTexture.Width, visibleHeight);
            var fillPosition = barPosition + EnergyInset + new Vector2(0, hiddenHeight);
            spriteBatch.Draw(fillTexture, fillPosition, source, hasFlight ? Color.White : Color.White * opacity);
        }

        if (animation.IsAnimating)
        {
            Texture2D sheet = usesLoop ? infiniteEffect.Value : refillEffect.Value;
            var source = new Rectangle(0, animation.Frame * EffectSize.Y, EffectSize.X, EffectSize.Y);
            spriteBatch.Draw(sheet, position, source, Color.White * opacity);
        }
    }
}
