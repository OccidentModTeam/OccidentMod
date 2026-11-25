#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class InventorySlotTextures : ModSystem
{
    private readonly Dictionary<Texture2D, Texture2D> slotBackgrounds = new(ReferenceEqualityComparer.Instance);

    private delegate void DrawSprite(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source,
        Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth);
    private delegate void DrawSpriteHook(DrawSprite original, SpriteBatch batch, Texture2D texture,
        Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale,
        SpriteEffects effects, float depth);

    private Asset<Texture2D>[]? previousChestStack;
    private Asset<Texture2D>[]? chestStackReplacement;

    private Asset<Texture2D>[]? previousSort;
    private Asset<Texture2D>[]? sortReplacement;

    private Asset<Texture2D>? previousTrash;
    private Asset<Texture2D>? trashReplacement;

    private Asset<Texture2D>? previousTrashFrame;
    private Asset<Texture2D>? trashFrameReplacement;

    private Asset<Texture2D>? previousBestiary;
    private Asset<Texture2D>? bestiaryReplacement;

    private readonly (Asset<Texture2D> Previous, Asset<Texture2D> Replacement)?[] mapIcons = new (Asset<Texture2D>, Asset<Texture2D>)?[8];

    public override void PostSetupContent()
    {
        AddSlotBackground(TextureAssets.InventoryBack, "Inventory_Back_Game");
        AddSlotBackground(TextureAssets.InventoryBack9, "Inventory_Back9_Game");
        AddSlotBackground(TextureAssets.InventoryBack14, "Inventory_Back14_Game");
        AddSlotBackground(TextureAssets.InventoryBack5, "Inventory_Back5_Game");

        var draw = typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.Draw),
            [typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float),
                typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float)]);
        if (draw is not null)
            MonoModHooks.Add(draw, (DrawSpriteHook)DrawSlotBackground);
        else
            Mod.Logger.Warn("SpriteBatch.Draw was not found. The four restored slot frames will use vanilla art.");

        chestStackReplacement =
        [
            Mod.Assets.Request<Texture2D>("Assets/UI/Inventory/ChestStack_0", AssetRequestMode.ImmediateLoad),
            Mod.Assets.Request<Texture2D>("Assets/UI/Inventory/ChestStack_1", AssetRequestMode.ImmediateLoad),
        ];

        sortReplacement =
        [
            Mod.Assets.Request<Texture2D>("Assets/UI/Inventory/Sort_0", AssetRequestMode.ImmediateLoad),
            Mod.Assets.Request<Texture2D>("Assets/UI/Inventory/Sort_1", AssetRequestMode.ImmediateLoad),
        ];
        previousSort = (Asset<Texture2D>[])TextureAssets.InventorySort.Clone();
        for (int i = 0; i < sortReplacement.Length; i++)
            TextureAssets.InventorySort[i] = sortReplacement[i];

        previousChestStack = (Asset<Texture2D>[])TextureAssets.ChestStack.Clone();
        for (int i = 0; i < chestStackReplacement.Length; i++)
            TextureAssets.ChestStack[i] = chestStackReplacement[i];

        trashReplacement = Mod.Assets.Request<Texture2D>(
            "Assets/UI/Inventory/Trash", AssetRequestMode.ImmediateLoad);
        trashFrameReplacement = Mod.Assets.Request<Texture2D>(
            "Assets/UI/Inventory/Inventory_Back7_Game", AssetRequestMode.ImmediateLoad);
        previousTrashFrame = TextureAssets.InventoryBack7;
        TextureAssets.InventoryBack7 = trashFrameReplacement;

        previousTrash = TextureAssets.Trash;
        TextureAssets.Trash = trashReplacement;

        bestiaryReplacement = Mod.Assets.Request<Texture2D>(
            "Assets/UI/Inventory/Bestiary", AssetRequestMode.ImmediateLoad);
        previousBestiary = TextureAssets.BestiaryMenuButton;
        TextureAssets.BestiaryMenuButton = bestiaryReplacement;

        for (int i = 0; i < mapIcons.Length; i++)
        {
            var icon = Mod.Assets.Request<Texture2D>(
                $"Assets/UI/Inventory/Map_{i}", AssetRequestMode.ImmediateLoad);
            mapIcons[i] = (TextureAssets.MapIcon[i], icon);
            TextureAssets.MapIcon[i] = icon;
        }
    }

    private void AddSlotBackground(Asset<Texture2D> vanilla, string name)
    {
        var original = Main.Assets.Request<Texture2D>(vanilla.Name, AssetRequestMode.ImmediateLoad);
        var restored = Mod.Assets.Request<Texture2D>("Assets/UI/Inventory/" + name, AssetRequestMode.ImmediateLoad);
        slotBackgrounds.Add(original.Value, restored.Value);
    }

    private void DrawSlotBackground(DrawSprite original, SpriteBatch batch, Texture2D texture, Vector2 position,
        Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (slotBackgrounds.TryGetValue(texture, out var restored) &&
            (source is null || source == new Rectangle(0, 0, texture.Width, texture.Height)))
        {
            // Terraria uses the vanilla asset's dimensions for slot spacing, hitboxes and item centers.
            origin += new Vector2(restored.Width - texture.Width, restored.Height - texture.Height) / 2f;
            texture = restored;
            source = null;
        }

        original(batch, texture, position, source, color, rotation, origin, scale, effects, depth);
    }

    public override void Unload()
    {
        slotBackgrounds.Clear();

        for (int i = 0; i < mapIcons.Length; i++)
        {
            if (mapIcons[i] is { } binding && ReferenceEquals(TextureAssets.MapIcon[i], binding.Replacement))
                TextureAssets.MapIcon[i] = binding.Previous;

            mapIcons[i] = null;
        }

        if (previousChestStack is not null && chestStackReplacement is not null)
        {
            for (int i = 0; i < chestStackReplacement.Length; i++)
            {
                if (ReferenceEquals(TextureAssets.ChestStack[i], chestStackReplacement[i]))
                    TextureAssets.ChestStack[i] = previousChestStack[i];
            }
        }

        previousChestStack = chestStackReplacement = null;

        if (previousSort is not null && sortReplacement is not null)
        {
            for (int i = 0; i < sortReplacement.Length; i++)
            {
                if (ReferenceEquals(TextureAssets.InventorySort[i], sortReplacement[i]))
                    TextureAssets.InventorySort[i] = previousSort[i];
            }
        }

        previousSort = sortReplacement = null;

        if (previousTrash is not null && ReferenceEquals(TextureAssets.Trash, trashReplacement))
            TextureAssets.Trash = previousTrash;

        previousTrash = trashReplacement = null;

        if (previousTrashFrame is not null && ReferenceEquals(TextureAssets.InventoryBack7, trashFrameReplacement))
            TextureAssets.InventoryBack7 = previousTrashFrame;

        previousTrashFrame = trashFrameReplacement = null;

        if (previousBestiary is not null && ReferenceEquals(TextureAssets.BestiaryMenuButton, bestiaryReplacement))
            TextureAssets.BestiaryMenuButton = previousBestiary;

        previousBestiary = bestiaryReplacement = null;
    }
}
