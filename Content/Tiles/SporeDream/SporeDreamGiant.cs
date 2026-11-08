#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OccidentMod.Content.Tiles.SporeDream;

/// <summary>
///     孢夢之森直立巨型蘑菇。
///     Vertical giant mushroom tile.
/// </summary>
public sealed class SporeDreamGiant : ModTile
{
    private const string Root =
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/";

    private const int SplitFrameBase = 108;
    private const int SingleFrameBase = 216;

    // 額外菇柄掉落率：25%。
    // Additional stem drop chance.
    private const int StemDropChance = 4;

    // 額外菇帽掉落率：40%。
    // Additional cap drop chance.
    private const int CapDropPercent = 40;

    private static bool _felling;

    public static readonly Rectangle[] CapFrames =
    {
        SporeDreamGiantLayout.CapFrame(0),
        SporeDreamGiantLayout.CapFrame(1),
        SporeDreamGiantLayout.CapFrame(2)
    };

    public override string Texture =>
        Root + "amanita_crema_tiles_02";

    private static bool Split(Tile tile)
    {
        return tile.TileFrameX >= SplitFrameBase &&
               tile.TileFrameX < SplitFrameBase + 108;
    }

    private static int Half(Tile tile)
    {
        return Split(tile)
            ? (tile.TileFrameX - SplitFrameBase) / 18 % 2
            : 0;
    }

    private static int PrimaryX(int x, int y)
    {
        return x - Half(Main.tile[x, y]);
    }

    public static bool IsPrimaryTile(int x, int y)
    {
        return Half(Main.tile[x, y]) == 0;
    }

    public static bool IsPairedTile(int x, int y)
    {
        return Split(Main.tile[x, y]);
    }

    public static float StemCenterX(int x, int y)
    {
        return PrimaryX(x, y) * 16 +
               (Split(Main.tile[x, y]) ? 16 : 8);
    }

    public static int CapStyle(int x, int y)
    {
        var tile = Main.tile[x, y];

        if (tile.TileFrameX >= SingleFrameBase) return (tile.TileFrameX - SingleFrameBase) / 18;

        if (Split(tile)) return (tile.TileFrameX - SplitFrameBase) / 36;

        return tile.TileFrameX / 18 % 3;
    }

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileAxe[Type] = true;
        Main.tileHammer[Type] = false;
        Main.tileLavaDeath[Type] = true;
        Main.tileNoFail[Type] = false;
        Main.tileNoAttach[Type] = true;

        MinPick = 0;
        MineResist = 1f;
        DustType = DustID.WoodFurniture;

        AddMapEntry(
            new Color(156, 83, 83),
            Language.GetOrRegister(
                "Mods.OccidentMod.SporeDream.GiantMushroom"
            )
        );

        // 不再註冊每格固定掉落。
        // Drops are now calculated during felling.
    }

    /// <summary>
    ///     嘗試生成巨型蘑菇。
    ///     Attempt to grow a giant mushroom.
    /// </summary>
    public static bool TryGrow(int x, int ground)
    {
        return SporeDreamCurvedSystem.Instance.TryFloor(
            x,
            ground
        );
    }

    /// <summary>
    ///     取得菇柄上下邊界。
    ///     Find the upper and lower stem boundaries.
    /// </summary>
    public static void GetStemBounds(
        int x,
        int y,
        out int top,
        out int bottom,
        out int bodyTop)
    {
        x = PrimaryX(x, y);

        var type =
            ModContent.TileType<SporeDreamGiant>();

        top = y;
        bottom = y + 1;

        while (top > 1 &&
               Main.tile[x, top - 1].HasTile &&
               Main.tile[x, top - 1].TileType == type)
            top--;

        while (bottom < Main.maxTilesY - 1 &&
               Main.tile[x, bottom].HasTile &&
               Main.tile[x, bottom].TileType == type)
            bottom++;

        bodyTop = top +
                  (Main.tile[x, top].TileFrameY == 0 ? 1 : 0);
    }

    /// <summary>
    ///     檢查菇柄位置是否具有裝飾。
    ///     Check whether the stem has decoration.
    /// </summary>
    public static bool HasDecoratedStemAt(int x, int y)
    {
        x = PrimaryX(x, y);

        GetStemBounds(
            x,
            y,
            out var top,
            out var bottom,
            out var bodyTop
        );

        var pixel = (y - bodyTop) * 16;

        if (pixel < 0)
            return false;

        var pixels = (bottom - bodyTop) * 16;

        var first =
            pixel / SporeDreamGiantLayout.StemHeight;

        var last =
            (pixel + 15) / SporeDreamGiantLayout.StemHeight;

        return
            SporeDreamGiantLayout.Decorated(
                x,
                top,
                pixels,
                first,
                Main.worldID
            ) ||
            SporeDreamGiantLayout.Decorated(
                x,
                top,
                pixels,
                last,
                Main.worldID
            );
    }

    /// <summary>
    ///     繪製直立巨菇柄。
    ///     Draw the vertical mushroom stem.
    /// </summary>
    public static void DrawStemTile(
        int i,
        int j,
        SpriteBatch spriteBatch,
        Vector2 offset)
    {
        if (!IsPrimaryTile(i, j))
            return;

        var x = PrimaryX(i, j);

        GetStemBounds(
            x,
            j,
            out var top,
            out var bottom,
            out var bodyTop
        );

        var startPixel = (j - bodyTop) * 16;

        if (startPixel < 0)
            return;

        var bodyPixels =
            (bottom - bodyTop) * 16;

        var texture =
            ModContent.Request<Texture2D>(
                Root + "amanita_crema_tiles_02"
            ).Value;

        var cutTop =
            Main.tile[x, top].TileFrameY != 0;

        var cutTexture =
            cutTop &&
            startPixel < SporeDreamGiantLayout.StemHeight
                ? ModContent.Request<Texture2D>(
                    Root + "amanita_crema_tiles_top"
                ).Value
                : null;

        var cutStyle =
            SporeDreamGiantLayout.Style(
                x,
                top,
                1,
                Main.worldID
            );

        var color = Lighting.GetColor(i, j);

        var slots =
            SporeDreamGiantLayout.DecorationSlots(
                x,
                top,
                bodyPixels,
                Main.worldID
            );

        var pixel = startPixel;
        var remaining = 16;
        var destinationY = 0;

        Vector2 position = new(
            StemCenterX(i, j) - 8,
            j * 16
        );

        if (cutTop &&
            j == top &&
            cutTexture is not null)
        {
            var tip =
                SporeDreamGiantLayout.CutStemFrame(
                    cutStyle,
                    0,
                    SporeDreamGiantLayout.CutTopExtension
                );

            spriteBatch.Draw(
                cutTexture,
                position -
                new Vector2(
                    0,
                    SporeDreamGiantLayout.CutTopExtension
                ) -
                Main.screenPosition +
                offset,
                tip,
                color
            );
        }

        while (remaining > 0)
        {
            var segment =
                pixel /
                SporeDreamGiantLayout.StemHeight;

            var height =
                SporeDreamGiantLayout.SliceHeight(
                    pixel,
                    remaining
                );

            var style =
                SporeDreamGiantLayout.Style(
                    x,
                    top,
                    segment,
                    Main.worldID
                );

            var decorated =
                segment == slots.First ||
                segment == slots.Second;

            var frame =
                SporeDreamGiantLayout.StemFrame(
                    style,
                    decorated,
                    pixel %
                    SporeDreamGiantLayout.StemHeight,
                    height
                );

            var capped =
                cutTop &&
                segment == 0 &&
                cutTexture is not null;

            if (capped)
                frame =
                    SporeDreamGiantLayout.CutStemFrame(
                        cutStyle,
                        pixel +
                        SporeDreamGiantLayout.CutTopExtension,
                        height
                    );

            spriteBatch.Draw(
                capped ? cutTexture! : texture,
                position +
                new Vector2(
                    capped ? 0 : -2,
                    destinationY
                ) -
                Main.screenPosition +
                offset,
                frame,
                color
            );

            pixel += height;
            destinationY += height;
            remaining -= height;
        }
    }

    public override bool PreDraw(
        int i,
        int j,
        SpriteBatch spriteBatch)
    {
        var tile = Main.tile[i, j];

        var offset = Main.drawToScreen
            ? Vector2.Zero
            : new Vector2(Main.offScreenRange);

        if (tile.TileFrameY == 0)
        {
            if (!IsPrimaryTile(i, j))
                return false;

            var frame =
                CapFrames[CapStyle(i, j)];

            var cap =
                ModContent.Request<Texture2D>(
                    Root + "fleshy_mushroom_caps"
                ).Value;

            Vector2 position = new(
                i * 16 +
                (Split(tile) ? 16 : 8) -
                SporeDreamGiantLayout.CapAnchorX,
                j * 16 + 16 - frame.Height
            );

            spriteBatch.Draw(
                cap,
                position -
                Main.screenPosition +
                offset,
                frame,
                Lighting.GetColor(i, j)
            );
        }
        else
        {
            DrawStemTile(
                i,
                j,
                spriteBatch,
                offset
            );
        }

        return false;
    }

    public override bool TileFrame(
        int i,
        int j,
        ref bool resetFrame,
        ref bool noBreak)
    {
        if (_felling)
            return false;

        var tile = Main.tile[i, j];
        var below = Main.tile[i, j + 1];

        var supported =
            below.HasTile &&
            (below.TileType == Type ||
             Main.tileSolid[below.TileType]);

        if (Split(tile))
        {
            var partnerX =
                i + (Half(tile) == 0 ? 1 : -1);

            var partner =
                Main.tile[partnerX, j];

            supported &=
                partner.HasTile &&
                partner.TileType == Type &&
                Split(partner) &&
                Half(partner) != Half(tile) &&
                CapStyle(i, j) ==
                CapStyle(partnerX, j) &&
                partner.TileFrameY == tile.TileFrameY;
        }

        if (!supported) WorldGen.KillTile(i, j);

        return false;
    }

    /// <summary>
    ///     計算本次砍倒的蘑菇總掉落量。
    ///     Calculate drops for the felled section.
    /// </summary>
    private static int CalculateDrops(
        int primaryX,
        int top,
        int cutY)
    {
        // 只要成功砍倒，至少給一個。
        // Guarantee one mushroom per successful cut.
        var amount = 1;

        // 每個有效莖部高度各有 25% 掉落機率。
        // Roll once per stem row, not once per column.
        for (var y = top + 1; y <= cutY; y++)
        {
            var tile = Main.tile[primaryX, y];

            if (!tile.HasTile)
                continue;

            if (WorldGen.genRand.NextBool(
                    StemDropChance))
                amount++;
        }

        // 原本完整的菇帽額外有 40% 機率掉落。
        // An intact cap receives an extra 40% roll.
        if (Main.tile[primaryX, top].HasTile &&
            Main.tile[primaryX, top].TileFrameY == 0 &&
            WorldGen.genRand.Next(100) <
            CapDropPercent)
            amount++;

        return amount;
    }

    /// <summary>
    ///     砍斷巨菇並清除上方連接的菇柄。
    ///     Fell the upper connected stem after a cut.
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

        // 連鎖清除中的格子不得再次掉落物品。
        // Suppress drops from recursively removed tiles.
        if (_felling)
        {
            noItem = true;
            return;
        }

        var current = Main.tile[i, j];

        var x = PrimaryX(i, j);
        var width = Split(current) ? 2 : 1;

        GetStemBounds(
            i,
            j,
            out var top,
            out _,
            out _
        );

        // 防止預設掉落與自訂掉落重複。
        // Suppress default drops for the initiating tile.
        var allowDrops = !noItem;

        noItem = true;

        var amount = 0;

        if (allowDrops &&
            Main.netMode !=
            NetmodeID.MultiplayerClient)
            amount = CalculateDrops(
                x,
                top,
                j);

        _felling = true;

        try
        {
            // 保留原本整段向上砍倒的行為。
            // Preserve original upward felling behavior.
            for (var y = j; y >= top; y--)
            for (var side = 0;
                 side < width;
                 side++)
            {
                if (x + side == i && y == j)
                    continue;

                var tile =
                    Main.tile[x + side, y];

                if (tile.HasTile &&
                    tile.TileType == Type)
                    WorldGen.KillTile(
                        x + side,
                        y,
                        noItem: true
                    );
            }
        }
        finally
        {
            _felling = false;
        }

        // 僅在伺服器或單人世界產生實際掉落。
        // Spawn drops on the authoritative game side.
        if (amount > 0)
            Item.NewItem(
                new EntitySource_TileBreak(i, j),
                i * 16,
                j * 16,
                16,
                16,
                ItemID.Mushroom,
                amount
            );
    }
}