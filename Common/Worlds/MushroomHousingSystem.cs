using Microsoft.Xna.Framework;
using Occident.Content.NPCs.TownPets;
using Occident.Content.Tiles.Furniture;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Occident.Common.Worlds;

public sealed class MushroomHousingSystem : ModSystem
{
    private int scanColumn;
    private int checkTimer;

    public override void ClearWorld()
    {
        scanColumn = 0;
        checkTimer = 0;
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // One bounded scan per world load also recovers bottles placed before their
        // tile entity existed. Normal placement still uses the networked tile hook.
        int columns = System.Math.Max(1, 65536 / Main.maxTilesY);
        int end = System.Math.Min(Main.maxTilesX, scanColumn + columns);
        int tileType = ModContent.TileType<MushroomInABottleTile>();
        for (; scanColumn < end; scanColumn++) {
            for (int y = 1; y < Main.maxTilesY - 1; y++) {
                Tile tile = Main.tile[scanColumn, y];
                if (!tile.HasTile || tile.TileType != tileType || tile.TileFrameX != 0 || tile.TileFrameY != 0)
                    continue;
                Point16 position = new(scanColumn, y);
                if (TileEntity.ByPosition.ContainsKey(position)) continue;
                int id = ModContent.GetInstance<MushroomBottleEntity>().Place(position.X, position.Y);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.TileEntitySharing, number: id, number2: position.X, number3: position.Y);
            }
        }

        if (++checkTimer < 180) return;
        checkTimer = 0;
        TryWelcomeMushroom();
    }

    internal static bool TryFindHome(Point16 bottlePosition, int ignoreNpc, out Point home)
    {
        home = Point.Zero;
        int type = ModContent.NPCType<BabyMushroomTownPet>();
        if (!WorldGen.InWorld(bottlePosition.X, bottlePosition.Y) ||
            !WorldGen.StartRoomCheck(bottlePosition.X, bottlePosition.Y) ||
            !WorldGen.RoomNeeds(type) || !MushroomBottleEntity.TestedRoomContainsBottle() ||
            TestedRoomHasHousingBlocker()) return false;
        WorldGen.ScoreRoom(ignoreNpc, type);
        if (WorldGen.hiScore <= 0) return false;
        home = new Point(WorldGen.bestX, WorldGen.bestY);
        return true;
    }

    private static bool TestedRoomHasHousingBlocker()
    {
        for (int i = 0; i < WorldGen.numRoomTiles; i++) {
            Tile tile = Main.tile[WorldGen.roomX[i], WorldGen.roomY[i]];
            if (tile.HasTile && (tile.TileType == TileID.StinkbugHousingBlocker ||
                tile.TileType == TileID.StinkbugHousingBlockerEcho)) return true;
        }
        return false;
    }

    private static void TryWelcomeMushroom()
    {
        int type = ModContent.NPCType<BabyMushroomTownPet>();
        int existing = NPC.FindFirstNPC(type);
        if (existing >= 0 && !Main.npc[existing].homeless) return;

        foreach (TileEntity entity in TileEntity.ByID.Values) {
            if (entity is not MushroomBottleEntity bottle ||
                !bottle.IsTileValidForEntity(bottle.Position.X, bottle.Position.Y) ||
                !TryFindHome(bottle.Position, existing, out Point home)) continue;

            bool newlyArrived = existing < 0;
            if (newlyArrived) {
                existing = NPC.NewNPC(new EntitySource_Misc("OccidentMushroomHousing"),
                    home.X * 16 + 8, home.Y * 16, type);
                if (existing >= Main.maxNPCs) return;
            }
            NPC npc = Main.npc[existing];
            npc.GivenName = string.Empty;
            npc.homeTileX = home.X;
            npc.homeTileY = home.Y;
            npc.homeless = false;
            WorldGen.TownManager.SetRoom(type, home.X, home.Y);
            npc.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: existing);
            if (newlyArrived) {
                if (Main.netMode == NetmodeID.SinglePlayer)
                    Main.NewText(Language.GetTextValue("Announcement.HasArrived", npc.FullName), 50, 125, 255);
                else
                    ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasArrived", npc.GetFullNetName()),
                        new Color(50, 125, 255));
            }
            return;
        }
    }
}
