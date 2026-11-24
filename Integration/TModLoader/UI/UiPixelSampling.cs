#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using Terraria.ModLoader;

namespace OccidentMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class UiPixelSampling : ModSystem
{
    private readonly HashSet<Texture> uiTextures = new(ReferenceEqualityComparer.Instance);
    private Hook? drawHook;

    private delegate void DrawPrimitives(SpriteBatch batch, Texture texture, int baseSprite, int batchSize);
    private delegate void DrawPrimitivesHook(
        DrawPrimitives original, SpriteBatch batch, Texture texture, int baseSprite, int batchSize);

    public override void PostSetupContent()
    {
        var draw = typeof(SpriteBatch).GetMethod("DrawPrimitives", BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(Texture), typeof(int), typeof(int)], null);
        if (draw is null)
        {
            Mod.Logger.Warn("FNA's SpriteBatch texture draw was not found. UI textures will use the game's sampler.");
            return;
        }

        foreach (string file in Mod.GetFileNames())
        {
            string path = file.Replace('\\', '/');
            if (path.StartsWith("Assets/UI/", StringComparison.Ordinal) &&
                Path.GetExtension(path) is ".rawimg" or ".png")
            {
                var asset = Mod.Assets.Request<Texture2D>(Path.ChangeExtension(path, null),
                    AssetRequestMode.ImmediateLoad);
                uiTextures.Add(asset.Value);
            }
        }

        // Shared UI batches do not expose per-texture sampler selection.
        drawHook = new Hook(draw, (DrawPrimitivesHook)DrawWithPointSampling);
    }

    public override void Unload()
    {
        drawHook?.Dispose();
        drawHook = null;
        uiTextures.Clear();
    }

    private void DrawWithPointSampling(
        DrawPrimitives original, SpriteBatch batch, Texture texture, int baseSprite, int batchSize)
    {
        if (!uiTextures.Contains(texture))
        {
            original(batch, texture, baseSprite, batchSize);
            return;
        }

        // FNA flushes each texture together. Set its sampler for this draw and restore the batch's state.
        var samplers = batch.GraphicsDevice.SamplerStates;
        var previous = samplers[0];
        samplers[0] = SamplerState.PointClamp;
        try
        {
            original(batch, texture, baseSprite, batchSize);
        }
        finally
        {
            samplers[0] = previous;
        }
    }
}
