using System;
using System.Reflection;
using Occident.Common.Worlds;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace Occident.Common.UI.WorldCreation;

// 自訂難度的原版基底是 Master；只替換此預覽元件持有的兔子圖層。
internal sealed class WorldDifficultyPreview
{
    private static readonly FieldInfo Bunny = typeof(UIWorldCreationPreview).GetField("_BunnyMasterTexture",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(nameof(UIWorldCreationPreview), "_BunnyMasterTexture");
    private readonly UIWorldCreationPreview preview;
    private readonly Asset<Texture2D> original;
    private OccidentWorldDifficulty current;

    public WorldDifficultyPreview(UIWorldCreationPreview preview)
    {
        this.preview = preview;
        original = (Asset<Texture2D>)Bunny.GetValue(preview);
    }

    public void SetDifficulty(OccidentWorldDifficulty difficulty)
    {
        if (current == difficulty)
            return;
        string asset = difficulty switch
        {
            OccidentWorldDifficulty.Longentity => "PreviewLongentity",
            OccidentWorldDifficulty.Elation => "PreviewElation",
            OccidentWorldDifficulty.Finality => "PreviewFinality",
            _ => null
        };
        Bunny.SetValue(preview, asset is null ? original
            : ModContent.Request<Texture2D>("OccidentMod/Assets/UI/WorldCreation/Difficulty/Previews/" + asset));
        current = difficulty;
    }
}

