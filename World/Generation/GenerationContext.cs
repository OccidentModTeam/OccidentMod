using System;

namespace OccidentMod.World.Generation;

/// <summary>
///     世界生成環境資料。
///     Shared world-generation context.
/// </summary>
public sealed class GenerationContext
{
    public GenerationContext(
        int worldWidth,
        int worldHeight,
        int spawnX,
        Func<int, WorldColumn> inspectColumn)
    {
        if (worldWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(worldWidth));

        if (worldHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(worldHeight));

        WorldWidth = worldWidth;
        WorldHeight = worldHeight;
        SpawnX = spawnX;

        InspectColumn = inspectColumn
                        ?? throw new ArgumentNullException(nameof(inspectColumn));
    }

    public int WorldWidth { get; }

    public int WorldHeight { get; }

    public int SpawnX { get; }

    // 世界生成時的地表欄位查詢。
    // Surface-column inspection during world generation.
    public Func<int, WorldColumn> InspectColumn { get; }
}

/// <summary>
///     地表欄位的環境資訊。
///     Surface-column information.
/// </summary>
public readonly record struct WorldColumn(
    int Ground,
    bool Safe,
    bool Grass,
    bool Evil,
    int WaterDepth = 0
);