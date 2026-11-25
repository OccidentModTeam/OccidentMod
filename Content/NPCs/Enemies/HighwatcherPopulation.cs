using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Content.NPCs.Enemies;

public sealed class HighwatcherPopulation : ModSystem
{
    private const int PixelsPerTile = 16;
    private const int RegionHalfWidth = 160 * PixelsPerTile;
    private const int RegionHalfHeight = 96 * PixelsPerTile;
    private const int CheckInterval = 120;
    private const ulong DefeatDelay = 60 * 60;

    private readonly record struct RecentDefeat(Vector2 Position, ulong Until);
    private readonly List<RecentDefeat> recentDefeats = new();

    public override void OnWorldUnload() => recentDefeats.Clear();

    // 每位玩家錯開檢查。同一個猩紅活動區的玩家會共用保底生物。
    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        ulong now = Main.GameUpdateCount;
        int slot = (int)(now % CheckInterval);
        if (slot == 0)
            recentDefeats.RemoveAll(defeat => defeat.Until <= now);

        for (int i = slot; i < Main.maxPlayers; i += CheckInterval)
            MaintainPopulation(Main.player[i]);
    }

    private void MaintainPopulation(Player player)
    {
        if (!player.active || player.dead || !player.ZoneCrimson || HasRecentDefeat(player.Center)
            || CountNearby(player.Center) > 0 || !TryFindGround(player, out Point tile))
            return;

        int index = NPC.NewNPC(new EntitySource_SpawnNPC(), tile.X * 16 + 8, tile.Y * 16,
            ModContent.NPCType<ArticulatedBiped>());
        if (index < Main.maxNPCs && Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: index);
    }

    internal void RecordDefeat(Vector2 bottom)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            recentDefeats.Add(new(bottom, Main.GameUpdateCount + DefeatDelay));
    }

    internal bool CanSpawnNaturally(NPCSpawnInfo spawn)
        => spawn.Player.ZoneCrimson && !spawn.PlayerInTown && !spawn.Water
            && !HasRecentDefeat(spawn.Player.Center) && CountNearby(spawn.Player.Center) < 2
            && CanStandAt(spawn.SpawnTileX, spawn.SpawnTileY);

    private bool HasRecentDefeat(Vector2 player)
    {
        ulong now = Main.GameUpdateCount;
        foreach (RecentDefeat defeat in recentDefeats)
        {
            if (defeat.Until > now && InRegion(player, defeat.Position))
                return true;
        }

        return false;
    }

    private static int CountNearby(Vector2 player)
    {
        int type = ModContent.NPCType<ArticulatedBiped>();
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type == type && InRegion(player, npc.Center))
                count++;
        }

        return count;
    }

    private static bool InRegion(Vector2 a, Vector2 b)
        => Math.Abs(a.X - b.X) <= RegionHalfWidth && Math.Abs(a.Y - b.Y) <= RegionHalfHeight;

    private static bool TryFindGround(Player player, out Point tile)
    {
        Point feet = player.Bottom.ToTileCoordinates();
        int firstSide = Main.rand.NextBool() ? 1 : -1;
        for (int distance = 72; distance >= 12; distance--)
        {
            for (int side = 0; side < 2; side++)
            {
                int x = feet.X + (side == 0 ? firstSide : -firstSide) * distance;
                for (int height = 0; height <= 24; height++)
                {
                    int above = feet.Y - height;
                    int below = feet.Y + height;
                    if (CanStandAt(x, above))
                    {
                        tile = new(x, above);
                        return true;
                    }

                    if (height != 0 && CanStandAt(x, below))
                    {
                        tile = new(x, below);
                        return true;
                    }
                }
            }
        }

        tile = default;
        return false;
    }

    private static bool CanStandAt(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 12))
            return false;

        Tile tile = Main.tile[x, y];
        if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || !IsCrimsonGround(tile.TileType))
            return false;

        Vector2 topLeft = new(x * 16 + 8 - ArticulatedBiped.CollisionWidth / 2f, y * 16 - ArticulatedBiped.CollisionHeight);
        return !Collision.SolidCollision(topLeft, ArticulatedBiped.CollisionWidth, ArticulatedBiped.CollisionHeight)
            && !Collision.WetCollision(topLeft, ArticulatedBiped.CollisionWidth, ArticulatedBiped.CollisionHeight);
    }

    private static bool IsCrimsonGround(ushort type) => type is
        TileID.CrimsonGrass or TileID.Crimstone or TileID.Crimsand or TileID.FleshIce
        or TileID.CrimsonHardenedSand or TileID.CrimsonSandstone or TileID.CrimsonJungleGrass;
}
