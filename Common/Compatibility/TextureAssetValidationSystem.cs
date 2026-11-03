using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace Occident.Common.Compatibility;

[Autoload(Side = ModSide.Client)]
public sealed class TextureAssetValidationSystem : ModSystem
{
    public override void PostSetupContent()
    {
        var textures = Mod.GetFileNames()
            .Where(file => file.StartsWith("Assets/", StringComparison.Ordinal)
                && (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".rawimg", StringComparison.OrdinalIgnoreCase)))
            .Select(file => Path.ChangeExtension(file, null))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        foreach (string texture in textures)
        {
            string assetPath = Mod.Name + "/" + texture;
            try
            {
                // Also validate assets not yet referenced by implemented items.
                _ = ModContent.Request<Texture2D>(assetPath, AssetRequestMode.ImmediateLoad).Value;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Cannot load project texture: " + assetPath, error);
            }
        }
        Mod.Logger.Info($"Project texture validation completed: {textures.Length} textures loaded.");
    }
}
