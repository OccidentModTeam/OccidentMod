using System;
using System.Collections.Generic;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     彎曲巨菇的生長路徑與分段幾何。
///     Curved giant mushroom growth path and segment geometry.
/// </summary>
internal static class SporeDreamGrowthPath
{
    internal const int Up = 0;
    internal const int Right = 1;
    internal const int Down = 2;
    internal const int Left = 3;

    internal const int Vertical = 0;
    internal const int Horizontal = 1;
    internal const int Turn = 2;
    internal const int Crown = 3;
    internal const int End = 4;

    internal static int Dx(int direction)
    {
        return direction == Right
            ? 1
            : direction == Left
                ? -1
                : 0;
    }

    internal static int Dy(int direction)
    {
        return direction == Down
            ? 1
            : direction == Up
                ? -1
                : 0;
    }

    /// <summary>
    ///     計算彎角方向。
    ///     Calculate the elbow corner orientation.
    /// </summary>
    internal static int CornerIndex(
        int incoming,
        int outgoing)
    {
        var entry = (incoming + 2) % 4;

        if ((entry + outgoing) % 2 == 0)
            throw new ArgumentException(
                "An elbow needs one vertical and one horizontal port"
            );

        return
            (entry == Down || outgoing == Down ? 2 : 0) +
            (entry == Right || outgoing == Right ? 1 : 0);
    }

    internal static (int X, int Y) Port(
        int corner,
        int direction)
    {
        return direction switch
        {
            Up => (
                corner % 2 == 1 ? 8 : 12,
                0
            ),

            Down => (
                corner % 2 == 1 ? 8 : 12,
                20
            ),

            Right => (
                20,
                corner >= 2 ? 8 : 12
            ),

            _ => (
                0,
                corner >= 2 ? 8 : 12
            )
        };
    }

    /// <summary>
    ///     刪除砍斷位置後的所有段落並添加斷端。
    ///     Remove pieces after a cut and append an end cap.
    /// </summary>
    internal static void Cut(
        List<Piece> pieces,
        int stage)
    {
        if (stage < 0 || stage >= pieces.Count)
            throw new ArgumentOutOfRangeException(
                nameof(stage)
            );

        pieces.RemoveRange(
            stage,
            pieces.Count - stage
        );

        if (pieces.Count == 0)
            return;

        var last = pieces[^1];
        var end = last.Exit;

        pieces.Add(
            new Piece(
                End,
                end.X,
                end.Y,
                last.Direction,
                last.Style % 5
            )
        );
    }

    /// <summary>
    ///     建立彎曲巨菇的完整生長路徑。
    ///     Create the complete curved mushroom growth path.
    /// </summary>
    internal static List<Piece> Create(
        int x,
        int y,
        int initialDirection,
        bool multiple,
        Func<int, int, int> next,
        int uprightSegments = -1,
        int floorSide = -1)
    {
        List<Piece> result = new();
        var direction = initialDirection;

        // 依目前方向添加直線段。
        // Append straight segments in the current direction.
        void Straight(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var nx = x + Dx(direction) * 20;
                var ny = y + Dy(direction) * 20;

                result.Add(
                    new Piece(
                        direction is Up or Down
                            ? Vertical
                            : Horizontal,
                        direction is Up or Down
                            ? x - 8
                            : Math.Min(x, nx),
                        direction is Up or Down
                            ? Math.Min(y, ny)
                            : y - 8,
                        direction,
                        next(0, 5)
                    )
                );

                x = nx;
                y = ny;
            }
        }

        // 依入口與出口方向添加彎角。
        // Append an elbow between two directions.
        void Bend(int outgoing)
        {
            var corner = CornerIndex(direction, outgoing);

            var entry = Port(
                corner,
                (direction + 2) % 4
            );

            var exit = Port(corner, outgoing);

            var left = x - entry.X;
            var top = y - entry.Y;

            result.Add(
                new Piece(
                    Turn,
                    left,
                    top,
                    outgoing,
                    next(0, 4),
                    direction
                )
            );

            x = left + exit.X;
            y = top + exit.Y;
            direction = outgoing;
        }

        if (direction == Up)
        {
            // 先構建完整地面根部。
            // Create a connected underground root.
            var side = floorSide is Left or Right
                ? floorSide
                : next(0, 2) == 0
                    ? Left
                    : Right;

            Bend(side);
            Straight(1);
            Bend(Up);

            Straight(
                uprightSegments >= 0
                    ? uprightSegments
                    : next(6, 10)
            );

            if (multiple &&
                uprightSegments != 0 &&
                uprightSegments != 1)
            {
                Bend(side);
                Straight(1);
            }
        }
        else
        {
            var outward = direction;

            Straight(
                uprightSegments is 0 or 1
                    ? 1
                    : next(1, 3)
            );

            Bend(Up);

            // 彎折之間保留較長的直立段。
            // Keep long upright sections between bends.
            Straight(
                uprightSegments >= 0
                    ? uprightSegments
                    : next(5, 9)
            );

            if (multiple &&
                uprightSegments != 0 &&
                uprightSegments != 1)
            {
                Bend(outward);
                Straight(next(1, 3));
            }
        }

        result.Add(
            new Piece(
                Crown,
                x,
                y,
                direction,
                next(0, 3)
            )
        );

        List<int> eligible = new();

        for (var i = 2; i < result.Count - 2; i++)
            if (result[i].Kind == Vertical)
                eligible.Add(i);

        // 整株分配膨大裝飾，避免連續出現。
        // Allocate swollen segments without clustering.
        if (eligible.Count > 0 && next(0, 10) < 9)
        {
            var quota = eligible.Count >= 10
                ? 3
                : eligible.Count >= 5
                    ? 2
                    : 1;

            for (var chosen = 0;
                 chosen < quota && eligible.Count > 0;
                 chosen++)
            {
                var selected =
                    eligible[next(0, eligible.Count)];

                result[selected] =
                    result[selected] with { Extra = 1 };

                eligible.RemoveAll(i => Math.Abs(i - selected) < 3
                );
            }
        }

        return result;
    }

    /// <summary>
    ///     巨菇路徑的一段。
    ///     One piece of a mushroom growth path.
    /// </summary>
    internal readonly record struct Piece(
        int Kind,
        int X,
        int Y,
        int Direction,
        int Style,
        int Extra = 0)
    {
        internal (int X, int Y) Exit => Kind switch
        {
            Vertical => (
                X + 8,
                Direction == Up ? Y : Y + 20
            ),

            Horizontal => (
                Direction == Right ? X + 20 : X,
                Y + 8
            ),

            Turn => (
                X + Port(
                    CornerIndex(Extra, Direction),
                    Direction
                ).X,
                Y + Port(
                    CornerIndex(Extra, Direction),
                    Direction
                ).Y
            ),

            _ => (X, Y)
        };
    }
}