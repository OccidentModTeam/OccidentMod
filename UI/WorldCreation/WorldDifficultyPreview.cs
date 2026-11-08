using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.World;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace OccidentMod.UI.WorldCreation;

/// <summary>
///     管理世界建立介面的自訂難度預覽圖片。
///     Manages custom difficulty preview textures
///     in the world creation interface.
/// </summary>
/// <remarks>
///     僅替換原版 Master 難度的兔子預覽圖層，
///     不修改原版世界生成設定。
///     Only replaces the vanilla Master bunny preview layer.
///     Does not modify vanilla world generation settings.
/// </remarks>
internal sealed class WorldDifficultyPreview
{
    private const string PreviewAssetFolder =
        "OccidentMod/Assets/UI/WorldCreation/Difficulty/Previews/";

    private const BindingFlags ReflectionFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly FieldInfo BunnyTextureField =
        typeof(UIWorldCreationPreview).GetField(
            "_BunnyMasterTexture",
            ReflectionFlags)
        ?? throw new MissingFieldException(
            nameof(UIWorldCreationPreview),
            "_BunnyMasterTexture");

    private readonly Asset<Texture2D> originalTexture;

    private readonly UIWorldCreationPreview preview;

    private readonly Dictionary<
        OccidentWorldDifficulty,
        Asset<Texture2D>
    > textureCache = new();

    private OccidentWorldDifficulty currentDifficulty;

    /// <summary>
    ///     建立難度預覽管理器並保留原版圖片參照。
    ///     Creates a difficulty preview controller
    ///     and preserves the original texture reference.
    /// </summary>
    public WorldDifficultyPreview(UIWorldCreationPreview preview)
    {
        this.preview = preview;

        originalTexture = (Asset<Texture2D>)
            BunnyTextureField.GetValue(preview);
    }

    /// <summary>
    ///     根據目前的世界難度切換預覽圖片。
    ///     Changes the preview texture according
    ///     to the selected world difficulty.
    /// </summary>
    public void SetDifficulty(OccidentWorldDifficulty difficulty)
    {
        if (currentDifficulty == difficulty)
            return;

        var texture =
            GetPreviewTexture(difficulty);

        BunnyTextureField.SetValue(
            preview,
            texture);

        currentDifficulty = difficulty;
    }

    /// <summary>
    ///     取得指定難度的預覽圖片。
    ///     Gets the preview texture for the specified difficulty.
    /// </summary>
    private Asset<Texture2D> GetPreviewTexture(
        OccidentWorldDifficulty difficulty)
    {
        var assetName = GetAssetName(difficulty);

        // 沒有自訂素材時還原原版圖片。
        // Restore the vanilla texture when
        // no custom preview is required.

        if (assetName is null)
            return originalTexture;

        if (textureCache.TryGetValue(
                difficulty,
                out var cached))
            return cached;

        var texture =
            ModContent.Request<Texture2D>(
                PreviewAssetFolder + assetName);

        textureCache.Add(
            difficulty,
            texture);

        return texture;
    }

    /// <summary>
    ///     取得自訂世界難度對應的素材名稱。
    ///     Gets the asset name associated
    ///     with a custom world difficulty.
    /// </summary>
    private static string GetAssetName(
        OccidentWorldDifficulty difficulty)
    {
        return difficulty switch
        {
            OccidentWorldDifficulty.Longentity =>
                "PreviewLongentity",

            OccidentWorldDifficulty.Elation =>
                "PreviewElation",

            OccidentWorldDifficulty.Finality =>
                "PreviewFinality",

            _ => null
        };
    }
}