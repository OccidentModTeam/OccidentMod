#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace VesperMod.Presentation.UI;

internal sealed class EnergyBar
{
    private const int SourcePixelSize = 2;

    private readonly Asset<Texture2D> frame;
    private readonly Asset<Texture2D> energy;
    private readonly Asset<Texture2D> effect;
    private readonly Vector2 energyInset;
    private readonly Vector2 effectOffset;
    private readonly int effectFrameHeight;

    public Point Size => new(frame.Value.Width, frame.Value.Height);

    public EnergyBar(Mod mod, string name, string effectName, Vector2 energyInset, Vector2 effectOffset,
        int effectFrameHeight)
    {
        const string folder = "Assets/UI/EnergyBar/";
        frame = mod.Assets.Request<Texture2D>(folder + name, AssetRequestMode.ImmediateLoad);
        energy = mod.Assets.Request<Texture2D>(folder + name + "_Energy");
        effect = mod.Assets.Request<Texture2D>(folder + effectName);
        this.energyInset = energyInset;
        this.effectOffset = effectOffset;
        this.effectFrameHeight = effectFrameHeight;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, float fraction, int? effectFrame)
    {
        Texture2D energyTexture = energy.Value;
        spriteBatch.Draw(frame.Value, position, Color.White);

        Rectangle fill = BarTextureRegions.HorizontalFill(energyTexture, fraction, SourcePixelSize);
        if (fill.Width > 0)
        {
            spriteBatch.Draw(energyTexture, position + energyInset, fill, Color.White);
        }

        if (effectFrame is { } frameIndex)
        {
            Texture2D effectTexture = effect.Value;
            if (BarTextureRegions.TryVerticalFrame(effectTexture, effectFrameHeight, frameIndex, out Rectangle source))
            {
                spriteBatch.Draw(effectTexture, position + effectOffset, source, Color.White);
            }
        }
    }
}
