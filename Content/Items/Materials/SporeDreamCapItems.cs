#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Networking;
using OccidentMod.World.SporeDreamForest;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.Items.Materials;

/// <summary>
///     可攜帶並重新種植的完整矮菇。
///     Portable and replantable dwarf mushroom.
/// </summary>
public abstract class SporeDreamCapItem : ModItem
{
    // 每種物品固定對應一種原始菌蓋。
    // Each item represents one original cap style.
    protected abstract int CapStyle { get; }

    public override string Texture =>
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/fleshy_mushroom_caps";

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = Item.CommonMaxStack;

        Item.value = Item.sellPrice(silver: 1);
        Item.rare = ItemRarityID.White;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 20;
        Item.useAnimation = 20;

        Item.useTurn = true;
        Item.autoReuse = false;
        Item.noUseGraphic = true;

        // 成功放置後自行扣除，避免重複消耗。
        // Consume manually after successful placement.
        Item.consumable = false;

        Item.material = false;
    }

    /// <summary>
    ///     使用矮菇物品，在目標地面重新種植。
    ///     Use the portable mushroom to replant it.
    /// </summary>
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer)
            return false;

        // 這兩個欄位屬於 Terraria.Player。
        // Tile targeting fields belong to Terraria.Player.
        var x = Player.tileTargetX;
        var y = Player.tileTargetY;

        if (!WorldGen.InWorld(x, y, 12))
            return false;

        Vector2 target = new(
            x * 16f + 8f,
            y * 16f + 8f
        );

        // 限制放置距離。
        // Limit placement distance.
        if (Vector2.Distance(
                player.Center,
                target
            ) > 160f)
            return false;

        // 多人模式由伺服器驗證及放置。
        // Multiplayer placement is server-authoritative.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SporeDreamDwarfPlacement.SendRequest(
                Mod,
                player,
                x,
                y
            );

            return true;
        }

        if (Main.netMode != NetmodeID.SinglePlayer)
            return false;

        // 單人模式直接執行合法性檢查及放置。
        // Single-player placement is performed locally.
        if (!SporeDreamCurvedSystem.Instance
                .TryPlaceDwarf(x, y, CapStyle))
            return false;

        // 僅成功種植才消耗一株矮菇。
        // Consume exactly one item on successful planting.
        Item.stack--;

        if (Item.stack <= 0)
            Item.TurnToAir();

        return true;
    }

    /// <summary>
    ///     背包中繪製對應的原始菌蓋樣式。
    ///     Draw the matching original cap style in inventory.
    /// </summary>
    public override bool PreDrawInInventory(
        SpriteBatch spriteBatch,
        Vector2 position,
        Rectangle frame,
        Color drawColor,
        Color itemColor,
        Vector2 origin,
        float scale)
    {
        var texture =
            ModContent.Request<Texture2D>(
                Texture,
                AssetRequestMode.ImmediateLoad
            ).Value;

        var source =
            SporeDreamGiantLayout.CapFrame(CapStyle);

        var drawScale =
            scale * 38f / source.Width;

        spriteBatch.Draw(
            texture,
            position,
            source,
            drawColor,
            0f,
            source.Size() / 2f,
            drawScale,
            SpriteEffects.None,
            0f
        );

        return false;
    }

    /// <summary>
    ///     世界中繪製對應的菌蓋物品。
    ///     Draw the matching mushroom item in the world.
    /// </summary>
    public override bool PreDrawInWorld(
        SpriteBatch spriteBatch,
        Color lightColor,
        Color alphaColor,
        ref float rotation,
        ref float scale,
        int whoAmI)
    {
        var texture =
            ModContent.Request<Texture2D>(
                Texture,
                AssetRequestMode.ImmediateLoad
            ).Value;

        var source =
            SporeDreamGiantLayout.CapFrame(CapStyle);

        spriteBatch.Draw(
            texture,
            Item.Center - Main.screenPosition,
            source,
            lightColor,
            rotation,
            source.Size() / 2f,
            scale * 0.35f,
            SpriteEffects.None,
            0f
        );

        return false;
    }
}

/// <summary>
///     第一款完整矮菇。
///     Complete dwarf mushroom, style 0.
/// </summary>
public sealed class SporeDreamCapStyle0 : SporeDreamCapItem
{
    protected override int CapStyle => 0;
}

/// <summary>
///     第二款完整矮菇。
///     Complete dwarf mushroom, style 1.
/// </summary>
public sealed class SporeDreamCapStyle1 : SporeDreamCapItem
{
    protected override int CapStyle => 1;
}

/// <summary>
///     第三款完整矮菇。
///     Complete dwarf mushroom, style 2.
/// </summary>
public sealed class SporeDreamCapStyle2 : SporeDreamCapItem
{
    protected override int CapStyle => 2;
}