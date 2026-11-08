#nullable enable

using System;
using System.IO;
using Microsoft.Xna.Framework;
using OccidentMod.Content.Items.Materials;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Networking;

/// <summary>
///     矮菇種植請求與伺服器驗證。
///     Dwarf mushroom placement and server validation.
/// </summary>
internal static class SporeDreamDwarfPlacement
{
    private const float MaximumReach = 160f;
    private const ulong RequestCooldown = 15;

    private static readonly ulong[] LastRequests =
        new ulong[256];

    /// <summary>
    ///     由持有物品的客戶端送出種植請求。
    ///     Send a placement request from the owning client.
    /// </summary>
    internal static void SendRequest(
        Mod mod,
        Player player,
        int x,
        int y)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient ||
            player.whoAmI != Main.myPlayer)
            return;

        var packet = mod.GetPacket();

        packet.Write(PacketIds.SporeDreamDwarfPlaceRequest);
        packet.Write(x);
        packet.Write(y);

        packet.Send();
    }

    /// <summary>
    ///     伺服器讀取、驗證並執行種植。
    ///     Server validates and performs mushroom placement.
    /// </summary>
    internal static void Receive(
        BinaryReader reader,
        int whoAmI)
    {
        int x;
        int y;

        try
        {
            x = reader.ReadInt32();
            y = reader.ReadInt32();
        }
        catch (EndOfStreamException)
        {
            return;
        }

        if (Main.netMode != NetmodeID.Server ||
            whoAmI < 0 ||
            whoAmI >= Main.maxPlayers ||
            whoAmI >= LastRequests.Length)
            return;

        var player = Main.player[whoAmI];

        if (!player.active ||
            player.dead ||
            player.ghost ||
            !WorldGen.InWorld(x, y, 12))
            return;

        ulong now = Main.GameUpdateCount;

        if (now < LastRequests[whoAmI])
            return;

        var slot = player.selectedItem;

        if (slot < 0 || slot > 9)
            return;

        var held = player.inventory[slot];

        if (held.IsAir ||
            held.stack <= 0 ||
            held.ModItem is not SporeDreamCapItem)
            return;

        Vector2 target = new(
            x * 16f + 8f,
            y * 16f + 8f
        );

        if (Vector2.Distance(
                player.Center,
                target) > MaximumReach)
            return;

        // 伺服器依實際持有的物品類型判定款式。
        // Derive the style from the authoritative held item.
        var style = held.type switch
        {
            var type when type ==
                          ModContent.ItemType<SporeDreamCapStyle0>() => 0,

            var type when type ==
                          ModContent.ItemType<SporeDreamCapStyle1>() => 1,

            var type when type ==
                          ModContent.ItemType<SporeDreamCapStyle2>() => 2,

            _ => -1
        };

        if (style < 0)
            return;

        // 成功的種植請求具有短暫冷卻。
        // Rate-limit successful placement requests.
        LastRequests[whoAmI] =
            now + RequestCooldown;

        if (!SporeDreamCurvedSystem.Instance
                .TryPlaceDwarf(x, y, style))
            return;

        // 放置成功後才由伺服器扣除物品。
        // Consume the item on the server after success.
        held.stack--;

        if (held.stack <= 0)
            held.TurnToAir();

        // 同步玩家實際持有的物品欄位。
        // Sync the authoritative inventory slot.
        NetMessage.SendData(
            MessageID.SyncEquipment,
            -1,
            -1,
            null,
            player.whoAmI,
            slot
        );
    }

    /// <summary>
    ///     世界切換時清除請求冷卻。
    ///     Reset request cooldowns on world changes.
    /// </summary>
    internal static void Reset()
    {
        Array.Clear(LastRequests);
    }
}