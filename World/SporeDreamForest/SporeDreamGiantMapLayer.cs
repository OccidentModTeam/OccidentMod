using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Tiles.SporeDream;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using G = OccidentMod.World.SporeDreamForest.SporeDreamGrowthPath;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森巨型蘑菇地圖圖層。
///     Spore Dream Forest giant mushroom map layer.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamGiantMapLayer : ModMapLayer
{
    private const string Root =
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/";

    // 已探索巨菇的地圖像素。
    // Cached explored mushroom map pixels.
    private readonly Dictionary<Point, bool> ink = new();

    // 快取素材像素，避免每次繪製重新讀取。
    // Cache source pixels to avoid repeated texture reads.
    private readonly Dictionary<
        string,
        (Color[] Pixels, int Width, int Height)> sources = new();

    private Texture2D mapPixel;

    private ulong refreshed;

    private int revision = -1;
    private int world = -1;

    public override Position GetDefaultPosition()
    {
        return BeforeFirstVanillaLayer;
    }

    /// <summary>
    ///     取得並快取素材像素。
    ///     Retrieve and cache sprite pixels.
    /// </summary>
    private (Color[] Pixels, int Width, int Height) Source(
        string name)
    {
        if (sources.TryGetValue(
                name,
                out var cached))
            return cached;

        var texture =
            ModContent.Request<Texture2D>(
                Root + name
            ).Value;

        var pixels =
            new Color[texture.Width * texture.Height];

        texture.GetData(pixels);

        var source = (
            Pixels: pixels,
            texture.Width,
            texture.Height
        );

        sources[name] = source;

        return source;
    }

    /// <summary>
    ///     將巨菇素材的不透明像素投影至地圖。
    ///     Project opaque mushroom sprite pixels onto the map.
    /// </summary>
    private void Stamp(SporeDreamGrowthPath.Piece piece)
    {
        string name;
        Rectangle frame;

        switch (piece.Kind)
        {
            case G.Crown:
                name = "fleshy_mushroom_caps";

                frame = SporeDreamGiantLayout.CapFrame(
                    piece.Style
                );
                break;

            case G.Vertical:
                name = "amanita_crema_tiles_02";

                frame = SporeDreamGiantLayout.StemFrame(
                    piece.Style,
                    piece.Extra == 1,
                    0,
                    20,
                    piece.Direction == G.Down
                );
                break;

            case G.Horizontal:
                name = "amanita_crema_tiles";

                frame = SporeDreamGiantLayout.HorizontalFrame(
                    piece.Style,
                    piece.Extra == 1,
                    piece.Direction == G.Right
                );
                break;

            case G.Turn:
                name = "amanita_crema_tiles_corner";

                frame = new Rectangle(
                    G.CornerIndex(
                        piece.Extra,
                        piece.Direction
                    ) * 22,
                    piece.Style % 3 * 22,
                    20,
                    20
                );
                break;

            case G.End:
                name = "amanita_crema_tiles_top";

                frame = SporeDreamGiantLayout.CutStemFrame(
                    piece.Style,
                    0,
                    24
                );
                break;

            default:
                return;
        }

        var source = Source(name);

        // 防止素材裁切範圍超出圖片。
        // Prevent source-frame access outside the texture.
        if (frame.X < 0 ||
            frame.Y < 0 ||
            frame.Right > source.Width ||
            frame.Bottom > source.Height)
            return;

        for (var y = 0; y < frame.Height; y++)
        for (var x = 0; x < frame.Width; x++)
        {
            var index =
                (frame.Y + y) * source.Width +
                frame.X + x;

            if (source.Pixels[index].A == 0)
                continue;

            var pixel = SporeDreamMapGeometry.Pixel(
                piece,
                x,
                y
            );

            var tile = SporeDreamMapGeometry.Tile(
                pixel.X,
                pixel.Y
            );

            Point point = new(
                tile.X,
                tile.Y
            );

            if (!WorldGen.InWorld(
                    point.X,
                    point.Y,
                    1))
                continue;

            ink.TryGetValue(
                point,
                out var cap
            );

            // 菌蓋顏色優先於菇柄。
            // Cap color takes priority over stem color.
            ink[point] =
                cap || piece.Kind == G.Crown;
        }
    }

    /// <summary>
    ///     重新建立巨菇地圖輪廓。
    ///     Rebuild the giant mushroom map geometry.
    /// </summary>
    private void Refresh()
    {
        ink.Clear();

        var curved =
            SporeDreamCurvedSystem.Instance;

        // 彎曲巨菇使用儲存的完整幾何。
        // Curved mushrooms use their stored geometry.
        foreach (var piece in curved.MapPieces()) Stamp(piece);

        // 直立菇柄已有實體地圖格。
        // 只補畫超出地塊範圍的菌蓋。
        //
        // Vertical stems already occupy vanilla map tiles.
        // Only their overhanging caps need an overlay.
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (forest.Exists)
        {
            var giantType =
                ModContent.TileType<SporeDreamGiant>();

            for (var x = forest.Left;
                 x < forest.Right;
                 x++)
            {
                var ground =
                    forest.Surface[x - forest.Left];

                var top = Math.Max(
                    1,
                    ground - 64
                );

                for (var y = top;
                     y < ground;
                     y++)
                {
                    var tile = Main.tile[x, y];

                    if (!tile.HasTile ||
                        tile.TileType != giantType ||
                        tile.TileFrameY != 0 ||
                        !SporeDreamGiant.IsPrimaryTile(x, y))
                        continue;

                    Stamp(
                        new SporeDreamGrowthPath.Piece(
                            G.Crown,
                            (int)SporeDreamGiant.StemCenterX(
                                x,
                                y
                            ),
                            (y + 1) * 16,
                            G.Up,
                            SporeDreamGiant.CapStyle(x, y)
                        )
                    );
                }
            }
        }

        revision = curved.MapRevision;
        world = Main.worldID;

        refreshed = Main.GameUpdateCount;
    }

    /// <summary>
    ///     繪製已探索的巨菇地圖像素。
    ///     Draw explored mushroom pixels on the map.
    /// </summary>
    public override void Draw(
        ref MapOverlayDrawContext context,
        ref string text)
    {
        if (Main.gameMenu || Main.Map == null)
            return;

        var curved =
            SporeDreamCurvedSystem.Instance;

        // 幾何改變或經過一定時間後更新快取。
        // Refresh the cache when geometry changes or expires.
        if (world != Main.worldID ||
            revision != curved.MapRevision ||
            Main.GameUpdateCount < refreshed ||
            Main.GameUpdateCount - refreshed > 120)
            Refresh();

        if (ink.Count == 0)
            return;

        // MagicPixel 並非固定 1×1，使用獨立貼圖。
        // Use a dedicated 1×1 map texture.
        if (mapPixel == null)
        {
            mapPixel = new Texture2D(
                Main.instance.GraphicsDevice,
                1,
                1
            );

            mapPixel.SetData(
                new[] { Color.White }
            );
        }

        var scale =
            context.MapScale / context.DrawScale;

        foreach (var entry in ink)
        {
            var point = entry.Key;

            var light =
                Main.Map[point.X, point.Y].Light;

            // 未探索區域不繪製，避免洩露地形。
            // Never reveal unexplored mushroom geometry.
            if (light == 0)
                continue;

            Vector2 tilePosition = new(
                point.X,
                point.Y
            );

            var screenPosition =
                (tilePosition - context.MapPosition)
                * context.MapScale
                + context.MapOffset;

            if (context.ClippingRectangle is Rectangle clip &&
                !clip.Contains(screenPosition.ToPoint()))
                continue;

            var color = entry.Value
                ? new Color(164, 98, 103)
                : new Color(142, 108, 106);

            color *= light / 255f;

            var result = context.Draw(
                mapPixel,
                tilePosition,
                color,
                new SpriteFrame(1, 1),
                scale,
                scale,
                Alignment.TopLeft
            );

            if (result.IsMouseOver)
                text = Language.GetTextValue(
                    "Mods.OccidentMod.SporeDream.GiantMushroom"
                );
        }
    }

    public override void Unload()
    {
        ink.Clear();
        sources.Clear();

        var oldPixel = mapPixel;
        mapPixel = null;

        if (oldPixel != null)
            Main.QueueMainThreadAction(() => oldPixel.Dispose()
            );
    }
}