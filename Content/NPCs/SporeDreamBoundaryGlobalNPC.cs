#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.NPCs;

/// <summary>
///     孢夢之森外來敵對 NPC 的雙向邊界保護。
///     Bidirectional forest boundary protection for
///     foreign hostile NPCs, bosses and segmented enemies.
/// </summary>
public sealed class SporeDreamBoundaryGlobalNpc : GlobalNPC
{
    // 傳送冷卻，避免敵怪反覆跨越邊界。
    // Cooldown prevents repeated boundary teleportation.
    private const ulong TeleportCooldown = 45;

    // 森林出口至少保留三格間距。
    // Minimum clearance outside the forest.
    private const int ExitMarginTiles = 3;

    private const int MaximumSearchDistance = 48;
    private const int MaximumVerticalSearch = 160;

    private ulong nextTeleportTick;
    public override bool InstancePerEntity => true;

    public override void OnSpawn(
        NPC npc,
        IEntitySource source)
    {
        nextTeleportTick = 0;
    }

    /// <summary>
    ///     NPC 更新後檢查森林邊界。
    ///     Check forest boundaries after NPC AI updates.
    /// </summary>
    public override void PostAI(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient ||
            !npc.active ||
            !IsForeignHostile(npc))
            return;

        var forest =
            SporeDreamEnvironmentSystem.Instance;

        if (!forest.Exists || !forest.Ready)
            return;

        if (Main.GameUpdateCount < nextTeleportTick)
            return;

        // 檢查 NPC 當前位置與移動路徑。
        // Check both current and predicted positions.
        if (!CrossesForest(npc, forest))
            return;

        var group = CollectGroup(npc);

        var bounds = GetGroupBounds(group);

        var fromLeft = DetermineEntrySide(
            npc,
            bounds,
            forest);

        if (!TryFindExit(
                group,
                bounds,
                forest,
                fromLeft,
                out var offset))
            return;

        // 對整個敵怪群組套用相同位移。
        // Move all linked segments by the same offset.
        foreach (var member in group)
        {
            member.position += offset;

            member.netUpdate = true;

            member.GetGlobalNPC<
                        SporeDreamBoundaryGlobalNpc>()
                    .nextTeleportTick =
                Main.GameUpdateCount +
                TeleportCooldown;
        }
    }

    /// <summary>
    ///     判斷是否為需要阻擋的外來敵怪。
    ///     Determine whether the NPC is a foreign hostile.
    /// </summary>
    private static bool IsForeignHostile(NPC npc)
    {
        if (npc.friendly ||
            npc.townNPC ||
            NPCID.Sets.CountsAsCritter[npc.type])
            return false;

        // 原生 NPC 可以自由進出森林。
        // Native NPCs may cross the forest freely.
        if (npc.ModNPC is ISporeDreamNativeNpc) return false;

        // 不排除 Boss 和多節敵怪。
        // Bosses and segmented enemies remain eligible.
        return true;
    }

    /// <summary>
    ///     檢查 NPC 是否進入森林區域。
    ///     Detect entry along the NPC movement path.
    /// </summary>
    private static bool CrossesForest(
        NPC npc,
        SporeDreamEnvironmentSystem forest)
    {
        var hitbox = npc.Hitbox;

        var velocityX = npc.velocity.X;
        var velocityY = npc.velocity.Y;

        var minX = Math.Min(
            hitbox.Left,
            hitbox.Left + velocityX);

        var maxX = Math.Max(
            hitbox.Right,
            hitbox.Right + velocityX);

        if (maxX < forest.Left * 16f ||
            minX >= forest.Right * 16f)
            return false;

        var steps = Math.Clamp(
            (int)Math.Ceiling(
                npc.velocity.Length() / 8f),
            1,
            512);

        for (var step = 0; step <= steps; step++)
        {
            var progress = step / (float)steps;

            var sample = hitbox;

            sample.Offset(
                (int)(velocityX * progress),
                (int)(velocityY * progress));

            if (IntersectsForest(sample, forest))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     判斷碰撞矩形是否與森林重疊。
    ///     Test intersection with the forest region.
    /// </summary>
    private static bool IntersectsForest(
        Rectangle bounds,
        SporeDreamEnvironmentSystem forest)
    {
        var left = Math.Max(
            forest.Left,
            bounds.Left / 16);

        var right = Math.Min(
            forest.Right - 1,
            (bounds.Right - 1) / 16);

        if (left > right)
            return false;

        var top = Math.Max(0, bounds.Top / 16);

        var bottom = Math.Min(
            Main.maxTilesY - 1,
            (bounds.Bottom - 1) / 16);

        for (var x = left; x <= right; x++)
        {
            var surface =
                forest.Surface[x - forest.Left];

            if (top <= surface +
                SporeDreamEnvironmentSystem.UndergroundDepth &&
                bottom >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     收集共享 realLife 的多節敵怪。
    ///     Collect linked NPC segments without breaking AI links.
    /// </summary>
    private static List<NPC> CollectGroup(NPC npc)
    {
        List<NPC> group = new();

        var root = npc.realLife >= 0 &&
                   npc.realLife < Main.maxNPCs
            ? npc.realLife
            : npc.whoAmI;

        foreach (var member in Main.ActiveNPCs)
            if (member.whoAmI == root ||
                member.realLife == root)
                group.Add(member);

        // 非多節敵怪通常只會有自身。
        // Ordinary NPCs form a one-member group.
        if (group.Count == 0)
            group.Add(npc);

        return group;
    }

    /// <summary>
    ///     取得整個多節敵怪的碰撞範圍。
    ///     Compute the bounding rectangle of all segments.
    /// </summary>
    private static Rectangle GetGroupBounds(
        List<NPC> group)
    {
        var bounds = group[0].Hitbox;

        for (var i = 1; i < group.Count; i++)
            bounds = Rectangle.Union(
                bounds,
                group[i].Hitbox);

        return bounds;
    }

    /// <summary>
    ///     判定從哪側進入森林。
    ///     Determine the entry side.
    /// </summary>
    private static bool DetermineEntrySide(
        NPC npc,
        Rectangle bounds,
        SporeDreamEnvironmentSystem forest)
    {
        // 先以水平移動方向判定。
        // Prefer horizontal velocity for entry direction.
        if (npc.velocity.X > 0.1f)
            return true;

        if (npc.velocity.X < -0.1f)
            return false;

        // 靜止敵怪依目前位置所在半側決定出口。
        // Stationary NPCs use their current forest half.
        var midpoint =
            (forest.Left + forest.Right) * 8f;

        return bounds.Center.X < midpoint;
    }

    /// <summary>
    ///     尋找森林另一側能容納整組 NPC 的位置。
    ///     Find a safe destination for the entire NPC group.
    /// </summary>
    private static bool TryFindExit(
        List<NPC> group,
        Rectangle bounds,
        SporeDreamEnvironmentSystem forest,
        bool fromLeft,
        out Vector2 offset)
    {
        offset = Vector2.Zero;

        var direction = fromLeft ? 1 : -1;

        var boundaryX = fromLeft
            ? forest.Right * 16f
            : forest.Left * 16f;

        for (var distance = ExitMarginTiles;
             distance <= MaximumSearchDistance;
             distance += 3)
        {
            var destinationX = direction > 0
                ? boundaryX + distance * 16f
                : boundaryX -
                  distance * 16f -
                  bounds.Width;

            var offsetX =
                destinationX - bounds.Left;

            // 優先維持高度，再往上尋找安全位置。
            // Preserve height first, then search upward.
            for (var vertical = 0;
                 vertical <= MaximumVerticalSearch;
                 vertical++)
            {
                Vector2 candidateOffset = new(
                    offsetX,
                    -vertical * 16f);

                if (!IsSafeGroupDestination(
                        group,
                        candidateOffset))
                    continue;

                offset = candidateOffset;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     檢查整個 NPC 群組是否能安全放置。
    ///     Validate all segments at the destination.
    /// </summary>
    private static bool IsSafeGroupDestination(
        List<NPC> group,
        Vector2 offset)
    {
        foreach (var npc in group)
        {
            var destination =
                npc.position + offset;

            var left = (int)MathF.Floor(
                destination.X / 16f);

            var top = (int)MathF.Floor(
                destination.Y / 16f);

            var right = (int)MathF.Ceiling(
                (destination.X + npc.width) / 16f);

            var bottom = (int)MathF.Ceiling(
                (destination.Y + npc.height) / 16f);

            if (!WorldGen.InWorld(left, top, 3) ||
                !WorldGen.InWorld(right, bottom, 3))
                return false;

            // 穿牆型 NPC 不需要檢查實體地塊。
            // Tile-phasing NPCs do not require clearance.
            if (npc.noTileCollide)
                continue;

            if (Collision.SolidCollision(
                    destination,
                    npc.width,
                    npc.height))
                return false;
        }

        return true;
    }
}