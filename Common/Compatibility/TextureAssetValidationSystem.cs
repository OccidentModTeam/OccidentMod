// 客戶端載入完成時檢查專案貼圖，錯誤訊息會指出素材路徑。
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
                // 尚未被物品引用的素材也一起載入驗證，讓損壞貼圖提早暴露。
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
