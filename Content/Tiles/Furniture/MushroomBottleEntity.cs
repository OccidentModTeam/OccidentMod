#nullable enable

// 為已放置的瓶子保存物塊實體索引，供住房檢查使用。
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.Content.Tiles.Furniture;

// 以可儲存、可同步的物塊實體索引記錄瓶子，避免每刻掃描整個世界。
public sealed class MushroomBottleEntity : ModTileEntity
{
    public override bool IsTileValidForEntity(int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
            return false;

        var tile = Main.tile[x, y];

        return tile.HasTile &&
               tile.TileType ==
               ModContent.TileType<MushroomInABottleTile>() &&
               tile.TileFrameX == 0 &&
               tile.TileFrameY == 0;
    }

    public static bool AnyPlaced()
    {
        foreach (var entity in ByID.Values)
            if (entity is MushroomBottleEntity bottle &&
                bottle.IsTileValidForEntity(
                    bottle.Position.X,
                    bottle.Position.Y))
                return true;

        return false;
    }

    public static bool TestedRoomContainsBottle()
    {
        var tileType =
            ModContent.TileType<MushroomInABottleTile>();

        // 檢查實際填充得到的房間格子，避免誤判隔壁房間。
        for (var i = 0; i < WorldGen.numRoomTiles; i++)
        {
            var tile = Main.tile[
                WorldGen.roomX[i],
                WorldGen.roomY[i]
            ];

            if (tile.HasTile &&
                tile.TileType == tileType)
                return true;
        }

        return false;
    }
}