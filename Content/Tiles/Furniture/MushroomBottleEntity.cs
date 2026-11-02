using Terraria;
using Terraria.ModLoader;

namespace Occident.Content.Tiles.Furniture;

// A saved, networked index of placed bottles avoids scanning the whole world each tick.
public sealed class MushroomBottleEntity : ModTileEntity
{
    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        return tile.HasTile && tile.TileType == ModContent.TileType<MushroomInABottleTile>() &&
            tile.TileFrameX == 0 && tile.TileFrameY == 0;
    }

    public static bool AnyPlaced()
    {
        foreach (var entity in Terraria.DataStructures.TileEntity.ByID.Values)
            if (entity is MushroomBottleEntity bottle &&
                bottle.IsTileValidForEntity(bottle.Position.X, bottle.Position.Y)) return true;
        return false;
    }

    public static bool TestedRoomContainsBottle()
    {
        int tileType = ModContent.TileType<MushroomInABottleTile>();
        // Use the actual flood-filled room, rather than its bounding rectangle.
        for (int i = 0; i < WorldGen.numRoomTiles; i++) {
            Tile tile = Main.tile[WorldGen.roomX[i], WorldGen.roomY[i]];
            if (tile.HasTile && tile.TileType == tileType) return true;
        }
        return false;
    }
}
