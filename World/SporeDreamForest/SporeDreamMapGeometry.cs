using System;
using G = OccidentMod.World.SporeDreamForest.SporeDreamGrowthPath;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     巨菇素材像素與世界座標轉換。
///     Giant mushroom pixel and world coordinate conversion.
/// </summary>
internal static class SporeDreamMapGeometry
{
    /// <summary>
    ///     將分段素材座標轉換為世界像素座標。
    ///     Convert local sprite pixels to world pixel coordinates.
    /// </summary>
    internal static (int X, int Y) Pixel(
        SporeDreamGrowthPath.Piece piece,
        int x,
        int y)
    {
        return piece.Kind switch
        {
            G.Crown => piece.Direction switch
            {
                G.Right => (
                    piece.X + 71 - y,
                    piece.Y - 62 + x
                ),

                G.Left => (
                    piece.X - 72 + y,
                    piece.Y + 61 - x
                ),

                _ => (
                    piece.X - 62 + x,
                    piece.Y - 72 + y
                )
            },

            G.Vertical => (
                piece.X - 2 + x,
                piece.Y + y
            ),

            G.Horizontal => (
                piece.X + x,
                piece.Y - 2 + y
            ),

            G.End => piece.Direction switch
            {
                G.Right => (
                    piece.X + 3 - y,
                    piece.Y - 8 + x
                ),

                G.Down => (
                    piece.X - 8 + x,
                    piece.Y + 3 - y
                ),

                G.Left => (
                    piece.X - 4 + y,
                    piece.Y - 8 + x
                ),

                _ => (
                    piece.X - 8 + x,
                    piece.Y - 4 + y
                )
            },

            _ => (
                piece.X + x,
                piece.Y + y
            )
        };
    }

    /// <summary>
    ///     將世界像素座標轉換成物塊座標。
    ///     Convert world pixel coordinates to tile coordinates.
    /// </summary>
    internal static (int X, int Y) Tile(
        int x,
        int y)
    {
        return (
            (int)MathF.Floor(x / 16f),
            (int)MathF.Floor(y / 16f)
        );
    }
}