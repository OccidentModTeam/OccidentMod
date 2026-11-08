using OccidentMod.Networking;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Gameplay.Players;

/// <summary>
///     比較玩家狀態快照，只在本機玩家資料改變時要求同步。
///     Compares player state snapshots and requests synchronization
///     only when local player data changes.
/// </summary>
public sealed class PlayerStateSyncPlayer : ModPlayer
{
    private PlayerSyncState snapshot;

    /// <summary>
    ///     初始化玩家狀態快照。
    ///     Initializes the player state snapshot.
    /// </summary>
    public override void Initialize()
    {
        snapshot = default;
    }

    /// <summary>
    ///     將目前玩家狀態同步至指定接收端。
    ///     Synchronizes the current player state to the specified recipient.
    /// </summary>
    public override void SyncPlayer(
        int toWho,
        int fromWho,
        bool newPlayer)
    {
        PlayerStateNetworking.Send(Mod, Player, toWho, fromWho);
    }

    /// <summary>
    ///     複製目前玩家狀態，供客戶端比較使用。
    ///     Copies the current player state for client-side comparison.
    /// </summary>
    public override void CopyClientState(ModPlayer targetCopy)
    {
        ((PlayerStateSyncPlayer)targetCopy).snapshot =
            PlayerStateNetworking.Capture(Player);
    }

    /// <summary>
    ///     玩家狀態改變時發送同步封包。
    ///     Sends a synchronization packet when the player state changes.
    /// </summary>
    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient ||
            Main.gameMenu ||
            Player.whoAmI != Main.myPlayer)
            return;

        var currentState =
            PlayerStateNetworking.Capture(Player);

        var previousState =
            ((PlayerStateSyncPlayer)clientPlayer).snapshot;

        if (currentState != previousState) PlayerStateNetworking.Send(Mod, Player);
    }
}