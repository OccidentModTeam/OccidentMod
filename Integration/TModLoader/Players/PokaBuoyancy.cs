using System;
using Microsoft.Xna.Framework;
using VesperMod.Content.Mounts;
using Terraria;
using Terraria.ID;

namespace VesperMod.Integration.TModLoader.Players;

internal static class PokaBuoyancy
{
    internal static void Apply(Player player, float surface, float walkClock, ref int waterBobTick)
    {
        // The sprite is anchored at the player hitbox bottom.
        float targetBottom = surface + PokaMount.FrameHeight * PokaMount.SpriteScale * 0.5f;
        // Tie the bob to the walk cycle and fade it in near the waterline.
        float nearSurface = 1f - MathHelper.Clamp(Math.Abs(targetBottom - player.Bottom.Y) / 12f, 0f, 1f);
        float movement = MathHelper.Clamp(Math.Abs(player.velocity.X) / 2f, 0f, 1f);
        waterBobTick = (waterBobTick + 1) % 100;
        float idleBob = 2f * MathF.Sin(waterBobTick * MathHelper.TwoPi / 100f);
        float walkBob = 3f * MathF.Sin(walkClock * MathHelper.TwoPi / PokaMount.WalkFrameCount);
        float bob = MathHelper.Lerp(idleBob, walkBob, movement);
        float distance = targetBottom + bob * nearSurface - player.Bottom.Y;
        // Rise quickly from deep water and ease in over the last two tiles.
        player.velocity.Y = MathHelper.Clamp(distance * 0.45f, -14f, 6f);
    }

    internal static bool TryGetSurface(Player player, out float surface)
    {
        surface = 0f;
        if (player.gravDir < 0f || player.lavaWet || player.honeyWet || player.shimmerWet ||
            player.grapCount > 0 || player.pulley || player.tongued)
            return false;

        int x = (int)(player.Center.X / 16f);
        int bottom = (int)(player.Bottom.Y / 16f);
        if (!WorldGen.InWorld(x, bottom, 10))
            return false;

        // Confirm water at the bird's feet before following its surface.
        int y = (int)((player.Bottom.Y - 2f) / 16f);
        Tile tile = Main.tile[x, y];
        if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Water)
            return false;

        int top = Math.Max(1, y - 16);
        while (y > top)
        {
            Tile above = Main.tile[x, y - 1];
            if (above.LiquidAmount == 0 || above.LiquidType != LiquidID.Water)
                break;

            tile = above;
            y--;
        }

        surface = 16f * (y + 1f - tile.LiquidAmount / 255f);
        return true;
    }
}
