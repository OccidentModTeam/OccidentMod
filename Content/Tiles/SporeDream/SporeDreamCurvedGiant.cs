#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Items.Materials;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OccidentMod.Content.Tiles.SporeDream;

/// <summary>
///     孢夢之森彎曲巨菇地塊。
///     Curved giant mushroom tile of Spore Dream Forest.
/// </summary>
public sealed class SporeDreamCurvedGiant : ModTile
{
    private const string AssetRoot =
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/";

    public override string Texture =>
        AssetRoot + "amanita_crema_tiles";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;

        Main.tileAxe[Type] = true;
        Main.tileHammer[Type] = false;
        Main.tileNoFail[Type] = false;

        MinPick = 0;
        MineResist = 1f;
        DustType = DustID.WoodFurniture;

        AddMapEntry(
            new Color(156, 83, 83),
            Language.GetText(
                "Mods.OccidentMod.SporeDream.GiantMushroom"
            )
        );

        // 不註冊原版蘑菇掉落。
        // All drops are handled by the geometry system.
    }

    /// <summary>
    ///     禁止地塊自動掉落物品。
    ///     Disable automatic item drops.
    /// </summary>
    public override bool CanDrop(int i, int j)
    {
        return false;
    }

    /// <summary>
    ///     繪製彎曲巨菇幾何。
    ///     Draw curved mushroom geometry.
    /// </summary>
    public override bool PreDraw(
        int i,
        int j,
        SpriteBatch spriteBatch)
    {
        var offset = Main.drawToScreen
            ? Vector2.Zero
            : new Vector2(Main.offScreenRange);

        SporeDreamCurvedSystem.Instance.DrawAt(
            i,
            j,
            spriteBatch,
            offset,
            false
        );

        return false;
    }

    /// <summary>
    ///     避免原版重新計算自訂巨菇框架。
    ///     Prevent vanilla tile frame recalculation.
    /// </summary>
    public override bool TileFrame(
        int i,
        int j,
        ref bool resetFrame,
        ref bool noBreak)
    {
        return false;
    }

    /// <summary>
    ///     拆卸巨菇，矮菇整株掉落。
    ///     Remove mushroom geometry and drop complete dwarfs.
    /// </summary>
    public override void KillTile(
        int i,
        int j,
        ref bool fail,
        ref bool effectOnly,
        ref bool noItem)
    {
        if (fail || effectOnly)
            return;

        // 不允許原版額外產生掉落。
        // Prevent duplicate automatic drops.
        var allowCustomDrop = !noItem;
        noItem = true;

        // 多人模式由伺服器修改世界與建立掉落。
        // Only the server modifies world geometry and drops.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        var system =
            SporeDreamCurvedSystem.Instance;

        // 先取得菌蓋款式，避免拆卸後資料已不存在。
        // Capture the style before removing the plant.
        var isDwarf =
            system.TryGetDwarfCapStyle(
                i,
                j,
                out var style
            );

        // 先確定是哪一種物品。
        // Determine the correct drop before cutting.
        int itemType;

        if (isDwarf)
            itemType = style switch
            {
                0 => ModContent.ItemType<SporeDreamCapStyle0>(),
                1 => ModContent.ItemType<SporeDreamCapStyle1>(),
                2 => ModContent.ItemType<SporeDreamCapStyle2>(),
                _ => ItemID.Mushroom
            };
        else
            itemType = ItemID.Mushroom;

        // 矮菇會從第 0 段整株移除；
        // 其他彎曲巨菇仍使用原有分段砍伐。
        //
        // Dwarfs are removed from stage zero;
        // other giants retain normal segment cutting.
        system.CutAt(i, j);

        if (!allowCustomDrop)
            return;

        // 一次挖掘只建立一個掉落物品。
        // Spawn a single drop for this tile-breaking event.
        Item.NewItem(
            new EntitySource_TileBreak(i, j),
            new Rectangle(
                i * 16,
                j * 16,
                16,
                16
            ),
            itemType
        );
    }
}