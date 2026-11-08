#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Tiles.SporeDream;
using OccidentMod.Networking;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using G = OccidentMod.World.SporeDreamForest.SporeDreamGrowthPath;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     彎曲巨菇的生成、幾何、放置、拆卸、彈跳及同步系統。
///     Curved mushroom generation, geometry, placement,
///     removal, spring behavior and synchronization.
/// </summary>
public sealed class SporeDreamCurvedSystem : ModSystem
{
    private const string Root =
        "OccidentMod/Assets/Sprites/Tiles/Biomes/SporeDreamForest/";

    // 集中管理封包 ID。
    // Centralized network packet identifiers.
    internal const byte SyncMessage =
        PacketIds.SporeDreamCurvedSync;

    internal const byte SpringRequest =
        PacketIds.SporeDreamSpringRequest;

    internal const byte SpringAnimation =
        PacketIds.SporeDreamSpringAnimation;

    private const int MaximumStoredPlants = 256;
    private const int MaximumPieces = 64;

    private readonly Dictionary<
        Point,
        (Plant Plant, int Stage)> cells = new();

    private readonly Dictionary<
        Point,
        List<(Plant Plant, int Stage)>> draws = new();

    private readonly List<Plant> plants = new();

    private readonly ulong[] springRequestAfter =
        new ulong[256];

    private bool cutting;
    private bool dirty;

    // 天然巨菇與總容量分開限制。
    // Separate natural growth limit from total capacity.
    private int MaximumNaturalPlants =>
        SporeDreamPopulationPolicy.GetCurvedPlantLimit(
            SporeDreamEnvironmentSystem.Instance.Surface.Length);

    internal int MapRevision { get; private set; }

    public static SporeDreamCurvedSystem Instance =>
        ModContent.GetInstance<SporeDreamCurvedSystem>();

    private int NaturalPlantCount =>
        plants.Count(plant => !plant.PlacedByPlayer);

    public override void ClearWorld()
    {
        plants.Clear();
        cells.Clear();
        draws.Clear();

        cutting = false;
        dirty = false;

        MapRevision++;

        Array.Clear(springRequestAfter);

        SporeDreamDwarfPlacement.Reset();
    }

    public override void PostWorldLoad()
    {
        if (plants.Count > 0)
            Mod.Logger.Info(
                $"Spore Dream Forest curved giants loaded: " +
                $"{plants.Count} plants, " +
                $"{cells.Count} occupied tiles."
            );
    }

    /// <summary>
    ///     判斷位置是否位於兩株朝上菌蓋之間。
    ///     Check whether a position lies between
    ///     two upward crowns.
    /// </summary>
    private bool BetweenUpwardCrowns(int x)
    {
        SporeDreamGrowthPath.Piece? left = null;
        SporeDreamGrowthPath.Piece? right = null;

        foreach (var plant in plants)
        {
            if (plant.Pieces.Count == 0)
                continue;

            var crown = plant.Pieces[^1];

            if (crown.Kind != G.Crown)
                continue;

            if (crown.X < x &&
                (left == null ||
                 crown.X > left.Value.X))
                left = crown;

            if (crown.X > x &&
                (right == null ||
                 crown.X < right.Value.X))
                right = crown;
        }

        return left?.Direction == G.Up &&
               right?.Direction == G.Up;
    }

    /// <summary>
    ///     取得菌蓋顯示範圍。
    ///     Get the crown rendering bounds.
    /// </summary>
    private static Rectangle CrownBounds(SporeDreamGrowthPath.Piece piece)
    {
        return piece.Direction switch
        {
            G.Right => new Rectangle(
                piece.X,
                piece.Y - 62,
                72,
                126
            ),

            G.Left => new Rectangle(
                piece.X - 72,
                piece.Y - 64,
                72,
                126
            ),

            _ => new Rectangle(
                piece.X - 62,
                piece.Y - 72,
                126,
                72
            )
        };
    }

    /// <summary>
    ///     取得巨菇分段的地塊範圍。
    ///     Get tile bounds of a mushroom segment.
    /// </summary>
    private static Rectangle Bounds(SporeDreamGrowthPath.Piece piece)
    {
        return piece.Kind switch
        {
            G.Vertical => new Rectangle(
                piece.X,
                piece.Y,
                16,
                20
            ),

            G.Horizontal => new Rectangle(
                piece.X,
                piece.Y,
                20,
                16
            ),

            G.Turn => new Rectangle(
                piece.X,
                piece.Y,
                20,
                20
            ),

            G.End => piece.Direction switch
            {
                G.Up => new Rectangle(
                    piece.X - 8,
                    piece.Y,
                    16,
                    20
                ),

                G.Down => new Rectangle(
                    piece.X - 8,
                    piece.Y - 20,
                    16,
                    20
                ),

                G.Right => new Rectangle(
                    piece.X - 20,
                    piece.Y - 8,
                    20,
                    16
                ),

                _ => new Rectangle(
                    piece.X,
                    piece.Y - 8,
                    20,
                    16
                )
            },

            _ => piece.Direction switch
            {
                G.Right => new Rectangle(
                    piece.X,
                    piece.Y - 8,
                    16,
                    16
                ),

                G.Left => new Rectangle(
                    piece.X - 16,
                    piece.Y - 8,
                    16,
                    16
                ),

                _ => new Rectangle(
                    piece.X - 8,
                    piece.Y - 16,
                    16,
                    16
                )
            }
        };
    }

    /// <summary>
    ///     列舉單一分段占用的世界地塊。
    ///     Enumerate world tiles occupied by a segment.
    /// </summary>
    private static IEnumerable<Point> Footprint(
        Plant plant,
        SporeDreamGrowthPath.Piece piece)
    {
        var rect = Bounds(piece);

        if (piece.Kind == G.Turn &&
            piece.Extra == G.Up &&
            plant.Pieces.Count > 0 &&
            piece == plant.Pieces[0])
            rect.Height = Math.Min(
                rect.Height,
                plant.RootA.Y * 16 - rect.Y
            );

        if (rect.Width <= 0 || rect.Height <= 0)
            yield break;

        var left = (int)Math.Floor(rect.Left / 16f);
        var right = (int)Math.Floor(
            (rect.Right - 1) / 16f
        );

        var top = (int)Math.Floor(rect.Top / 16f);
        var bottom = (int)Math.Floor(
            (rect.Bottom - 1) / 16f
        );

        for (var x = left; x <= right; x++)
        for (var y = top; y <= bottom; y++)
        {
            Point cell = new(x, y);

            if (cell != plant.RootA &&
                cell != plant.RootB)
                yield return cell;
        }
    }

    /// <summary>
    ///     重建巨菇占格與繪製索引。
    ///     Rebuild occupancy and rendering indices.
    /// </summary>
    private void Rebuild()
    {
        cells.Clear();
        draws.Clear();

        MapRevision++;

        foreach (var plant in plants)
            for (var stage = 0;
                 stage < plant.Pieces.Count;
                 stage++)
            {
                Point? anchor = null;

                foreach (var point in Footprint(
                             plant,
                             plant.Pieces[stage]))
                {
                    if (!WorldGen.InWorld(
                            point.X,
                            point.Y,
                            1))
                        continue;

                    anchor ??= point;

                    cells.TryAdd(
                        point,
                        (plant, stage)
                    );
                }

                if (anchor is Point key)
                {
                    if (!draws.TryGetValue(
                            key,
                            out var entries))
                    {
                        entries = new List<(Plant Plant, int Stage)>();
                        draws[key] = entries;
                    }

                    entries.Add((plant, stage));
                }
            }
    }

    /// <summary>
    ///     判斷地塊是否為可生長的天然土壤。
    ///     Check whether a tile is valid natural soil.
    /// </summary>
    private static bool Soil(Tile tile)
    {
        return tile.HasTile &&
               (tile.TileType ==
                ModContent.TileType<SporeDreamGrass>() ||
                tile.TileType == TileID.Mud ||
                tile.TileType == TileID.Dirt);
    }

    private static bool RootSafe(Point point)
    {
        if (!WorldGen.InWorld(
                point.X,
                point.Y,
                10))
            return false;

        var tile = Main.tile[
            point.X,
            point.Y
        ];

        return Soil(tile) &&
               !Main.wallHouse[tile.WallType] &&
               tile.LiquidAmount == 0;
    }

    /// <summary>
    ///     檢查巨菇生長空間及地形安全性。
    ///     Validate mushroom growth space and terrain safety.
    /// </summary>
    private bool SpaceAvailable(
        Plant plant,
        bool allowOutsideForest = false)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!allowOutsideForest && !forest.Exists)
            return false;

        var giant =
            ModContent.TileType<SporeDreamGiant>();

        var curved =
            ModContent.TileType<SporeDreamCurvedGiant>();

        var cover =
            ModContent.TileType<SporeDreamPlants>();

        List<Rectangle> regions = new();

        for (var stage = 0;
             stage < plant.Pieces.Count;
             stage++)
        {
            var piece = plant.Pieces[stage];

            var rect = piece.Kind == G.Crown
                ? CrownBounds(piece)
                : Bounds(piece);

            // 永遠不能超出世界安全範圍。
            // Never exceed safe world bounds.
            if (rect.Left < 160 ||
                rect.Right > (Main.maxTilesX - 10) * 16 ||
                rect.Top < 160 ||
                rect.Bottom > (Main.maxTilesY - 10) * 16)
                return false;

            // 天然生成受森林邊界限制。
            // Natural growth is limited to the forest.
            if (!allowOutsideForest &&
                (rect.Left < (forest.Left + 2) * 16 ||
                 rect.Right > (forest.Right - 2) * 16))
                return false;

            if (piece.Kind != G.Crown)
                for (var earlier = 0;
                     earlier < regions.Count - 1;
                     earlier++)
                    if (rect.Intersects(
                            regions[earlier]))
                        return false;

            regions.Add(rect);

            var left = (int)Math.Floor(
                rect.Left / 16f
            );

            var right = (int)Math.Floor(
                (rect.Right - 1) / 16f
            );

            var top = (int)Math.Floor(
                rect.Top / 16f
            );

            var bottom = (int)Math.Floor(
                (rect.Bottom - 1) / 16f
            );

            for (var x = left; x <= right; x++)
            for (var y = top; y <= bottom; y++)
            {
                if (!WorldGen.InWorld(x, y, 10))
                    return false;

                Point point = new(x, y);
                var tile = Main.tile[x, y];

                var root =
                    stage == 0 &&
                    (point == plant.RootA ||
                     point == plant.RootB);

                if (tile.LiquidAmount > 0 ||
                    Main.wallHouse[tile.WallType] ||
                    (!root &&
                     tile.HasTile &&
                     tile.TileType != cover))
                    return false;
            }

            // 避開其他直立或彎曲巨菇。
            // Avoid existing straight and curved giants.
            var margin = rect;
            margin.Inflate(4 * 16, 3 * 16);

            var marginLeft = Math.Max(
                1,
                margin.Left / 16
            );

            var marginRight = Math.Min(
                Main.maxTilesX - 2,
                margin.Right / 16
            );

            var marginTop = Math.Max(
                1,
                margin.Top / 16
            );

            var marginBottom = Math.Min(
                Main.maxTilesY - 2,
                margin.Bottom / 16
            );

            for (var x = marginLeft;
                 x <= marginRight;
                 x++)
            for (var y = marginTop;
                 y <= marginBottom;
                 y++)
            {
                var tile = Main.tile[x, y];

                if (tile.HasTile &&
                    (tile.TileType == giant ||
                     tile.TileType == curved))
                    return false;
            }
        }

        return RootSafe(plant.RootA) &&
               RootSafe(plant.RootB);
    }

    /// <summary>
    ///     將新巨菇註冊到世界及占格資料。
    ///     Register a new mushroom and its world tiles.
    /// </summary>
    private void Place(Plant plant)
    {
        plants.Add(plant);
        Rebuild();

        var type =
            ModContent.TileType<SporeDreamCurvedGiant>();

        foreach (var entry in cells)
        {
            if (!ReferenceEquals(
                    entry.Value.Plant,
                    plant))
                continue;

            var tile = Main.tile[
                entry.Key.X,
                entry.Key.Y
            ];

            tile.ClearTile();

            tile.HasTile = true;
            tile.TileType = (ushort)type;
            tile.TileFrameX = 0;
            tile.TileFrameY = 0;
        }

        dirty = true;
    }

    /// <summary>
    ///     規劃天然地面巨菇。
    ///     Plan a natural ground mushroom.
    /// </summary>
    private FloorPlan? PlanFloor(
        int x,
        int ground,
        int growth)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (Main.netMode == NetmodeID.MultiplayerClient ||
            NaturalPlantCount >= MaximumNaturalPlants ||
            plants.Count >= MaximumStoredPlants)
            return null;

        if (growth < 0)
            growth = WorldGen.genRand.NextBool(6)
                ? 1
                : 0;

        var dwarf = growth == 1;

        var direction = WorldGen.genRand.NextBool()
            ? G.Left
            : G.Right;

        var padX =
            dwarf && direction == G.Left
                ? x - 1
                : x;

        var padWidth = dwarf ? 2 : 1;

        if (!forest.PrepareGiantFoundation(
                padX,
                ground,
                false,
                padWidth))
            return null;

        for (var attempt = 0;
             attempt < 4;
             attempt++)
        {
            var startX = x * 16 + 8;

            var segments = dwarf
                ? WorldGen.genRand.Next(0, 2)
                : growth == 2
                    ? WorldGen.genRand.Next(10, 13)
                    : -1;

            var multiple =
                !dwarf &&
                (BetweenUpwardCrowns(startX)
                    ? WorldGen.genRand.Next(5) < 4
                    : WorldGen.genRand.NextBool(5));

            Plant plant = new(
                new Point(x, ground),
                new Point(
                    dwarf
                        ? direction == G.Left
                            ? x - 1
                            : x + 1
                        : x,
                    ground
                ),
                G.Create(
                    startX,
                    ground * 16,
                    G.Up,
                    multiple,
                    WorldGen.genRand.Next,
                    segments,
                    direction
                )
            );

            if (plant.Pieces.Count > MaximumPieces)
                continue;

            if (SpaceAvailable(plant))
                return new FloorPlan(
                    plant,
                    padX,
                    ground,
                    padWidth
                );
        }

        return null;
    }

    /// <summary>
    ///     嘗試生成天然地面巨菇。
    ///     Attempt to generate a natural ground mushroom.
    /// </summary>
    public bool TryFloor(
        int x,
        int ground,
        int growth = -1)
    {
        var plan = PlanFloor(
            x,
            ground,
            growth
        );

        if (plan == null)
            return false;

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.PrepareGiantFoundation(
                plan.X,
                plan.Ground,
                true,
                plan.Width))
            return false;

        Place(plan.Plant);
        return true;
    }

    /// <summary>
    ///     在森林外或森林內重新種植完整矮菇。
    ///     Replant a complete dwarf mushroom anywhere
    ///     on suitable ground.
    /// </summary>
    public bool TryPlaceDwarf(
        int x,
        int ground,
        int style)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient ||
            style < 0 ||
            style >= SporeDreamGiantLayout.CapCount ||
            plants.Count >= MaximumStoredPlants)
            return false;

        if (!WorldGen.InWorld(x, ground, 12))
            return false;

        for (var directionIndex = 0;
             directionIndex < 2;
             directionIndex++)
        {
            var direction = directionIndex == 0
                ? G.Right
                : G.Left;

            var neighborX =
                x + (direction == G.Right ? 1 : -1);

            Point rootA = new(x, ground);
            Point rootB = new(neighborX, ground);

            if (!RootSafe(rootA) ||
                !RootSafe(rootB))
                continue;

            for (var segments = 0;
                 segments <= 1;
                 segments++)
            {
                var pieces = G.Create(
                    x * 16 + 8,
                    ground * 16,
                    G.Up,
                    false,
                    Main.rand.Next,
                    segments,
                    direction
                );

                if (pieces.Count == 0 ||
                    pieces.Count > MaximumPieces)
                    continue;

                var crown = pieces[^1];

                if (crown.Kind != G.Crown)
                    continue;

                // 保留原始菌蓋款式。
                // Preserve the original mushroom cap style.
                pieces[^1] = crown with
                {
                    Style = style
                };

                Plant plant = new(
                    rootA,
                    rootB,
                    pieces
                )
                {
                    PlacedByPlayer = true
                };

                if (!SpaceAvailable(
                        plant,
                        true))
                    continue;

                Place(plant);

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendTileSquare(
                        -1,
                        x,
                        ground - 5,
                        12
                    );

                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     嘗試生成一對高大巨菇。
    ///     Attempt to generate a pair of tall mushrooms.
    /// </summary>
    public int TryTallPair()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient ||
            NaturalPlantCount + 2 > MaximumNaturalPlants ||
            plants.Count + 2 > MaximumStoredPlants)
            return 0;

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists ||
            forest.Surface.Length < 20)
            return 0;

        var choices = Enumerable.Range(
            forest.Left + 8,
            forest.Surface.Length - 16
        ).ToList();

        var firstTarget =
            forest.Left +
            forest.Surface.Length / 4;

        choices.Sort((a, b) =>
            Math.Abs(a - firstTarget)
                .CompareTo(
                    Math.Abs(b - firstTarget))
        );

        foreach (var first in choices)
        {
            var one = PlanFloor(
                first,
                forest.Surface[
                    first - forest.Left
                ],
                2
            );

            if (one == null)
                continue;

            var secondTarget =
                forest.Left +
                forest.Surface.Length * 3 / 4;

            foreach (var second in choices.OrderBy(x => Math.Abs(x - secondTarget)))
            {
                if (Math.Abs(first - second) <
                    Math.Max(
                        20,
                        forest.Surface.Length / 3))
                    continue;

                var two = PlanFloor(
                    second,
                    forest.Surface[
                        second - forest.Left
                    ],
                    2
                );

                if (two == null)
                    continue;

                if (!forest.PrepareGiantFoundation(
                        one.X,
                        one.Ground,
                        true,
                        one.Width))
                    return 0;

                if (!forest.PrepareGiantFoundation(
                        two.X,
                        two.Ground,
                        true,
                        two.Width))
                    return 0;

                Place(one.Plant);
                Place(two.Plant);

                return 2;
            }
        }

        return 0;
    }

    /// <summary>
    ///     沿森林側面生成彎曲巨菇。
    ///     Populate curved mushrooms along terrain side faces.
    /// </summary>
    public int PopulateSides(int budget)
    {
        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists)
            return 0;

        var count = 0;

        List<(int X, int Direction)> candidates =
            new();

        for (var x = forest.Left + 8;
             x < forest.Right - 8;
             x++)
            foreach (var direction in new[] { -1, 1 })
            {
                var ground =
                    forest.Surface[
                        x - forest.Left
                    ];

                if (forest.Surface[
                        x + direction - forest.Left
                    ] >= ground + 3)
                    candidates.Add((x, direction));
            }

        // 隨機化側面生成位置。
        // Shuffle side-growth candidates.
        for (var i = candidates.Count - 1;
             i > 0;
             i--)
        {
            var j = WorldGen.genRand.Next(i + 1);

            (candidates[i], candidates[j]) =
                (candidates[j], candidates[i]);
        }

        foreach (var candidate in candidates)
        {
            if (count >= budget ||
                NaturalPlantCount >= MaximumNaturalPlants ||
                plants.Count >= MaximumStoredPlants)
                break;

            var x = candidate.X;
            var direction = candidate.Direction;

            var ground =
                forest.Surface[
                    x - forest.Left
                ];

            Point root = new(
                x,
                ground + 1
            );

            var safe = true;

            for (var dx = 0; dx <= 1; dx++)
            for (var y = ground;
                 y <= ground + 2;
                 y++)
                safe &= RootSafe(
                    new Point(
                        x - direction * dx,
                        y
                    )
                );

            if (!safe)
                continue;

            for (var attempt = 0;
                 attempt < 4;
                 attempt++)
            {
                var face =
                    (direction > 0 ? x + 1 : x) * 16;

                Plant plant = new(
                    root,
                    root,
                    G.Create(
                        face,
                        (ground + 1) * 16 + 8,
                        direction > 0
                            ? G.Right
                            : G.Left,
                        WorldGen.genRand.NextBool(5),
                        WorldGen.genRand.Next
                    )
                );

                if (plant.Pieces.Count > MaximumPieces ||
                    !SpaceAvailable(plant))
                    continue;

                for (var dx = 0; dx <= 1; dx++)
                for (var y = ground;
                     y <= ground + 2;
                     y++)
                {
                    var tile = Main.tile[
                        x - direction * dx,
                        y
                    ];

                    tile.TileType = (ushort)(
                        dx == 0
                            ? ModContent.TileType<SporeDreamGrass>()
                            : TileID.Mud
                    );

                    tile.IsHalfBlock = false;
                    tile.Slope = 0;
                    tile.TileFrameX = 0;
                    tile.TileFrameY = 0;
                }

                for (var xx = x - 2;
                     xx <= x + 2;
                     xx++)
                for (var y = ground - 1;
                     y <= ground + 4;
                     y++)
                    WorldGen.SquareTileFrame(
                        xx,
                        y,
                        false
                    );

                Place(plant);

                count++;
                break;
            }
        }

        return count;
    }

    /// <summary>
    ///     判斷地塊是否屬於完整矮菇，
    ///     並取得原始菌蓋款式。
    ///     Identify a complete dwarf mushroom
    ///     and return its original cap style.
    /// </summary>
    internal bool TryGetDwarfCapStyle(
        int x,
        int y,
        out int style)
    {
        style = -1;

        if (!cells.TryGetValue(
                new Point(x, y),
                out var hit))
            return false;

        var plant = hit.Plant;

        if (plant.RootA == plant.RootB ||
            plant.Pieces.Count == 0 ||
            !SporeDreamSpringMath.IsSpring(
                plant.Pieces))
            return false;

        var crown = plant.Pieces[^1];

        if (crown.Kind != G.Crown ||
            crown.Direction != G.Up ||
            crown.Style < 0 ||
            crown.Style >= SporeDreamGiantLayout.CapCount)
            return false;

        style = crown.Style;
        return true;
    }

    /// <summary>
    ///     砍斷巨菇，矮菇則拆除整株。
    ///     Cut a giant mushroom or remove an entire dwarf.
    /// </summary>
    public void CutAt(int x, int y)
    {
        if (cutting ||
            Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!cells.TryGetValue(
                new Point(x, y),
                out var hit))
            return;

        var isDwarf =
            TryGetDwarfCapStyle(x, y, out _);

        Cut(
            hit.Plant,
            isDwarf ? 0 : hit.Stage
        );
    }

    /// <summary>
    ///     切斷分段並更新世界地塊。
    ///     Cut a segment and update world tiles.
    /// </summary>
    private void Cut(Plant plant, int stage)
    {
        cutting = true;

        var oldCells = cells
            .Where(entry =>
                ReferenceEquals(
                    entry.Value.Plant,
                    plant))
            .Select(entry => entry.Key)
            .ToArray();

        try
        {
            foreach (var point in oldCells)
                Main.tile[
                    point.X,
                    point.Y
                ].ClearTile();

            G.Cut(plant.Pieces, stage);

            if (plant.Pieces.Count == 0)
                plants.Remove(plant);

            Rebuild();

            var type =
                ModContent.TileType<SporeDreamCurvedGiant>();

            foreach (var entry in cells)
            {
                if (!ReferenceEquals(
                        entry.Value.Plant,
                        plant))
                    continue;

                var tile = Main.tile[
                    entry.Key.X,
                    entry.Key.Y
                ];

                tile.ClearTile();
                tile.HasTile = true;
                tile.TileType = (ushort)type;
                tile.TileFrameX = 0;
                tile.TileFrameY = 0;
            }

            if (Main.netMode == NetmodeID.Server &&
                oldCells.Length > 0)
            {
                var left = oldCells.Min(p => p.X);
                var top = oldCells.Min(p => p.Y);

                NetMessage.SendTileSquare(
                    -1,
                    left,
                    top,
                    oldCells.Max(p => p.X) - left + 1,
                    oldCells.Max(p => p.Y) - top + 1
                );
            }

            dirty = true;
        }
        finally
        {
            cutting = false;
        }
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (Main.GameUpdateCount % 30 == 0)
            foreach (var plant in plants.ToArray())
                if (!RootSafe(plant.RootA) ||
                    !RootSafe(plant.RootB))
                    Cut(plant, 0);

        if (dirty && Main.netMode == NetmodeID.Server)
        {
            var packet = Mod.GetPacket();

            packet.Write(SyncMessage);

            NetSend(packet);

            packet.Send();

            dirty = false;
        }
    }

    /// <summary>
    ///     儲存全部巨菇及玩家種植標記。
    ///     Save all mushrooms and player placement flags.
    /// </summary>
    public override void SaveWorldData(TagCompound tag)
    {
        tag["CurvedGiants"] = plants.Select(plant => new TagCompound
            {
                ["A"] = new[]
                {
                    plant.RootA.X,
                    plant.RootA.Y
                },

                ["B"] = new[]
                {
                    plant.RootB.X,
                    plant.RootB.Y
                },

                ["PlacedByPlayer"] =
                    plant.PlacedByPlayer,

                ["Pieces"] = plant.Pieces
                    .SelectMany(piece => new[]
                    {
                        piece.Kind,
                        piece.X,
                        piece.Y,
                        piece.Direction,
                        piece.Style,
                        piece.Extra
                    })
                    .ToArray()
            }
        ).ToList();
    }

    /// <summary>
    ///     載入世界巨菇幾何及種植標記。
    ///     Load mushroom geometry and placement flags.
    /// </summary>
    public override void LoadWorldData(TagCompound tag)
    {
        ClearWorld();

        if (!tag.ContainsKey("CurvedGiants"))
            return;

        foreach (var data in
                 tag.GetList<TagCompound>(
                     "CurvedGiants"))
        {
            var a = data.GetIntArray("A");
            var b = data.GetIntArray("B");
            var values = data.GetIntArray("Pieces");

            if (a.Length != 2 ||
                b.Length != 2 ||
                values.Length % 6 != 0 ||
                values.Length > MaximumPieces * 6 ||
                plants.Count >= MaximumStoredPlants)
                continue;

            List<SporeDreamGrowthPath.Piece> pieces = new();

            for (var i = 0;
                 i < values.Length;
                 i += 6)
                pieces.Add(
                    new SporeDreamGrowthPath.Piece(
                        values[i],
                        values[i + 1],
                        values[i + 2],
                        values[i + 3],
                        values[i + 4],
                        values[i + 5]
                    )
                );

            Plant plant = new(
                new Point(a[0], a[1]),
                new Point(b[0], b[1]),
                pieces
            );

            // 舊存檔若沒有此欄位，一律視為天然生成。
            // Missing flags in older saves default to natural.
            plant.PlacedByPlayer =
                data.ContainsKey("PlacedByPlayer") &&
                data.GetBool("PlacedByPlayer");

            plants.Add(plant);
        }

        Rebuild();
    }

    /// <summary>
    ///     傳送全部巨菇資料。
    ///     Send complete mushroom data.
    /// </summary>
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(plants.Count);

        foreach (var plant in plants)
        {
            writer.Write(plant.RootA.X);
            writer.Write(plant.RootA.Y);

            writer.Write(plant.RootB.X);
            writer.Write(plant.RootB.Y);

            // 與 NetReceive 順序必須完全一致。
            // Must match NetReceive field order.
            writer.Write(plant.PlacedByPlayer);
            writer.Write(plant.Pieces.Count);

            foreach (var piece in plant.Pieces)
            {
                writer.Write(piece.Kind);
                writer.Write(piece.X);
                writer.Write(piece.Y);
                writer.Write(piece.Direction);
                writer.Write(piece.Style);
                writer.Write(piece.Extra);
            }
        }
    }

    /// <summary>
    ///     接收全部巨菇資料。
    ///     Receive complete mushroom data.
    /// </summary>
    public override void NetReceive(BinaryReader reader)
    {
        var count = reader.ReadInt32();

        if (count < 0 ||
            count > MaximumStoredPlants)
            throw new IOException(
                "Invalid curved giant count."
            );

        List<Plant> received = new();

        for (var i = 0; i < count; i++)
        {
            Point a = new(
                reader.ReadInt32(),
                reader.ReadInt32()
            );

            Point b = new(
                reader.ReadInt32(),
                reader.ReadInt32()
            );

            var placedByPlayer =
                reader.ReadBoolean();

            var length = reader.ReadInt32();

            if (length < 0 ||
                length > MaximumPieces)
                throw new IOException(
                    "Invalid curved giant path."
                );

            List<SporeDreamGrowthPath.Piece> pieces = new();

            for (var j = 0;
                 j < length;
                 j++)
            {
                SporeDreamGrowthPath.Piece piece = new(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );

                if (piece.Kind < 0 ||
                    piece.Kind > G.End ||
                    piece.Direction < 0 ||
                    piece.Direction > 3 ||
                    piece.X < 0 ||
                    piece.Y < 0 ||
                    piece.X > Main.maxTilesX * 16 ||
                    piece.Y > Main.maxTilesY * 16)
                    throw new IOException(
                        "Invalid curved giant geometry."
                    );

                pieces.Add(piece);
            }

            Plant plant = new(a, b, pieces)
            {
                PlacedByPlayer = placedByPlayer
            };

            received.Add(plant);
        }

        plants.Clear();
        plants.AddRange(received);

        Rebuild();
    }

    /// <summary>
    ///     繪製指定位置的巨菇分段。
    ///     Draw mushroom segments at a tile.
    /// </summary>
    public void DrawAt(
        int i,
        int j,
        SpriteBatch batch,
        Vector2 offset,
        bool stemsOnly)
    {
        if (!draws.TryGetValue(
                new Point(i, j),
                out var entries))
            return;

        foreach (var entry in entries)
        {
            var piece =
                entry.Plant.Pieces[entry.Stage];

            if (stemsOnly && piece.Kind == G.Crown)
                continue;

            var depression =
                piece.Kind == G.Crown
                    ? SpringOffset(entry.Plant)
                    : 0f;

            DrawPiece(
                piece,
                batch,
                offset,
                Lighting.GetColor(i, j),
                depression
            );
        }
    }

    /// <summary>
    ///     列舉地圖圖層需要的巨菇分段。
    ///     Enumerate pieces for the mushroom map layer.
    /// </summary>
    internal IEnumerable<SporeDreamGrowthPath.Piece> MapPieces()
    {
        foreach (var plant in plants)
        foreach (var piece in plant.Pieces)
            yield return piece;
    }

    private Plant? SpringAt(Point root)
    {
        return plants.FirstOrDefault(plant =>
            plant.RootA == root &&
            SporeDreamSpringMath.IsSpring(
                plant.Pieces)
        );
    }

    private static float SpringOffset(Plant plant)
    {
        if (!plant.Pressing)
            return 0f;

        var elapsed =
            Main.GameUpdateCount -
            plant.PressStarted;

        if (elapsed >=
            SporeDreamSpringMath.AnimationTicks)
            return 0f;

        return SporeDreamSpringMath.Offset(
            (int)elapsed
        );
    }

    /// <summary>
    ///     查詢彈跳菌蓋表面。
    ///     Query the mushroom spring surface.
    /// </summary>
    internal bool TrySpringSurface(
        Point root,
        float left,
        float width,
        out float top,
        out float depression)
    {
        top = 0f;
        depression = 0f;

        var plant = SpringAt(root);

        if (plant == null ||
            plant.Pieces.Count == 0)
            return false;

        if (!SporeDreamSpringMath.TryTop(
                plant.Pieces[^1],
                left,
                width,
                out top))
            return false;

        depression = SpringOffset(plant);

        return true;
    }

    /// <summary>
    ///     檢查玩家是否落在矮菇菌蓋上。
    ///     Check whether a player lands on a spring crown.
    /// </summary>
    internal bool TrySpringLanding(
        float left,
        float width,
        float before,
        float after,
        float velocity,
        out Point root,
        out float top)
    {
        root = default;
        top = float.MaxValue;

        var found = false;

        foreach (var plant in plants)
        {
            if (plant.Pieces.Count == 0 ||
                !SporeDreamSpringMath.IsSpring(
                    plant.Pieces))
                continue;

            if (!SporeDreamSpringMath.TryTop(
                    plant.Pieces[^1],
                    left,
                    width,
                    out var surface))
                continue;

            surface += SpringOffset(plant);

            if (surface >= top ||
                !SporeDreamSpringMath.Crosses(
                    before,
                    after,
                    surface,
                    velocity))
                continue;

            found = true;
            root = plant.RootA;
            top = surface;
        }

        return found;
    }

    internal int SpringAge(Point root)
    {
        var plant = SpringAt(root);

        if (plant == null || !plant.Pressing)
            return 0;

        return (int)Math.Min(
            Main.GameUpdateCount -
            plant.PressStarted,
            SporeDreamSpringMath.AnimationTicks
        );
    }

    /// <summary>
    ///     啟動矮菇彈跳動畫。
    ///     Start the mushroom spring animation.
    /// </summary>
    internal bool StartSpring(
        Point root,
        bool request = true)
    {
        var plant = SpringAt(root);

        if (plant == null)
            return false;

        if (!plant.Pressing ||
            Main.GameUpdateCount -
            plant.PressStarted >=
            SporeDreamSpringMath.PressTicks)
        {
            plant.Pressing = true;
            plant.PressStarted = Main.GameUpdateCount;
        }

        if (request &&
            Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Mod.GetPacket();

            packet.Write(SpringRequest);
            packet.Write(root.X);
            packet.Write(root.Y);

            packet.Send();
        }

        return true;
    }

    /// <summary>
    ///     接收矮菇彈跳動畫請求或同步訊息。
    ///     Receive spring requests and animation synchronization.
    /// </summary>
    internal void ReceiveSpring(
        BinaryReader reader,
        int whoAmI,
        bool request)
    {
        Point root;

        try
        {
            root = new Point(
                reader.ReadInt32(),
                reader.ReadInt32()
            );
        }
        catch (EndOfStreamException)
        {
            return;
        }

        if (request)
        {
            if (Main.netMode != NetmodeID.Server ||
                whoAmI < 0 ||
                whoAmI >= Main.maxPlayers ||
                whoAmI >= springRequestAfter.Length)
                return;

            var player = Main.player[whoAmI];

            if (!player.active ||
                player.dead ||
                player.ghost ||
                player.gravDir != 1 ||
                player.mount.Active ||
                Main.GameUpdateCount <
                springRequestAfter[whoAmI] ||
                !TrySpringSurface(
                    root,
                    player.position.X,
                    player.width,
                    out var top,
                    out var depression) ||
                Math.Abs(
                    player.Bottom.Y -
                    top -
                    depression
                ) > 64)
                return;

            springRequestAfter[whoAmI] =
                Main.GameUpdateCount +
                SporeDreamSpringMath.CooldownTicks;

            StartSpring(root, false);

            var packet = Mod.GetPacket();

            packet.Write(SpringAnimation);
            packet.Write(root.X);
            packet.Write(root.Y);

            packet.Send(-1, whoAmI);
        }
        else if (Main.netMode ==
                 NetmodeID.MultiplayerClient)
        {
            StartSpring(root, false);
        }
    }

    /// <summary>
    ///     查詢巨菇孢子釋放位置。
    ///     Query mushroom spore emission positions.
    /// </summary>
    public bool TrySporeAt(
        int x,
        int y,
        out Vector2 center,
        out float spread)
    {
        center = default;
        spread = 0f;

        if (!draws.TryGetValue(
                new Point(x, y),
                out var entries))
            return false;

        foreach (var entry in entries)
        {
            var piece =
                entry.Plant.Pieces[entry.Stage];

            if (piece.Kind == G.Crown)
            {
                center =
                    CrownBounds(piece)
                        .Center.ToVector2();

                spread = 45f;
                return true;
            }

            if ((piece.Kind == G.Vertical ||
                 piece.Kind == G.Horizontal) &&
                piece.Extra == 1)
            {
                center =
                    Bounds(piece)
                        .Center.ToVector2();

                spread = 14f;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     繪製巨菇素材分段。
    ///     Render individual mushroom sprite pieces.
    /// </summary>
    private static void DrawPiece(
        SporeDreamGrowthPath.Piece piece,
        SpriteBatch batch,
        Vector2 offset,
        Color color,
        float depression)
    {
        var screen =
            -Main.screenPosition + offset;

        void Draw(
            string file,
            Rectangle source,
            int x,
            int y)
        {
            var texture =
                ModContent.Request<Texture2D>(
                    Root + file
                ).Value;

            batch.Draw(
                texture,
                new Vector2(x, y) + screen,
                source,
                color
            );
        }

        if (piece.Kind == G.Crown)
        {
            var texture =
                ModContent.Request<Texture2D>(
                    Root + "fleshy_mushroom_caps"
                ).Value;

            batch.Draw(
                texture,
                new Vector2(
                    piece.X,
                    piece.Y + depression
                ) + screen,
                SporeDreamGiantLayout.CapFrame(
                    piece.Style
                ),
                color,
                piece.Direction *
                MathHelper.PiOver2,
                new Vector2(
                    SporeDreamGiantLayout.CapAnchorX,
                    72
                ),
                1f,
                SpriteEffects.None,
                0f
            );

            return;
        }

        if (piece.Kind == G.End)
        {
            var rotation =
                piece.Direction *
                MathHelper.PiOver2;

            var texture =
                ModContent.Request<Texture2D>(
                    Root + "amanita_crema_tiles_top"
                ).Value;

            var local =
                Vector2.Transform(
                    new Vector2(-8, -4),
                    Matrix.CreateRotationZ(rotation)
                );

            var effect =
                piece.Direction == G.Left ||
                piece.Direction == G.Down
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None;

            batch.Draw(
                texture,
                new Vector2(
                    piece.X,
                    piece.Y
                ) + screen + local,
                SporeDreamGiantLayout.CutStemFrame(
                    piece.Style,
                    0,
                    24
                ),
                color,
                rotation,
                Vector2.Zero,
                1f,
                effect,
                0f
            );

            return;
        }

        if (piece.Kind == G.Vertical)
        {
            Draw(
                "amanita_crema_tiles_02",
                SporeDreamGiantLayout.StemFrame(
                    piece.Style,
                    piece.Extra == 1,
                    0,
                    20,
                    piece.Direction == G.Down
                ),
                piece.X - 2,
                piece.Y
            );

            return;
        }

        if (piece.Kind == G.Horizontal)
        {
            Draw(
                "amanita_crema_tiles",
                SporeDreamGiantLayout.HorizontalFrame(
                    piece.Style,
                    piece.Extra == 1,
                    piece.Direction == G.Right
                ),
                piece.X,
                piece.Y - 2
            );

            return;
        }

        var corner = G.CornerIndex(
            piece.Extra,
            piece.Direction
        );

        Draw(
            "amanita_crema_tiles_corner",
            new Rectangle(
                corner * 22,
                piece.Style % 3 * 22,
                20,
                20
            ),
            piece.X,
            piece.Y
        );
    }

    /// <summary>
    ///     單株彎曲巨菇的資料。
    ///     Data of one curved mushroom.
    /// </summary>
    private sealed class Plant
    {
        internal readonly List<SporeDreamGrowthPath.Piece> Pieces;
        internal readonly Point RootA;
        internal readonly Point RootB;

        // 玩家放置的巨菇不占天然生成名額。
        // Player-placed mushrooms do not consume
        // the natural generation quota.
        internal bool PlacedByPlayer;
        internal ulong PressStarted;

        internal bool Pressing;

        internal Plant(
            Point rootA,
            Point rootB,
            List<SporeDreamGrowthPath.Piece> pieces)
        {
            RootA = rootA;
            RootB = rootB;
            Pieces = pieces;
        }
    }

    private sealed record FloorPlan(
        Plant Plant,
        int X,
        int Ground,
        int Width
    );
}