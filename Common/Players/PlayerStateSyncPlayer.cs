// 比較前後狀態快照，只在本機玩家資料改變時要求同步。
using Occident.Common.Networking;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Common.Players;

// 用一份狀態快照涵蓋職業與休閒模式，避免兩者同時變更時重複發送封包。
public sealed class PlayerStateSyncPlayer : ModPlayer
{
    private PlayerSyncState snapshot;

    public override void Initialize() => snapshot = default;

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        => PlayerStateNetworking.Send(Mod, Player, toWho, fromWho);

    public override void CopyClientState(ModPlayer targetCopy)
        => ((PlayerStateSyncPlayer)targetCopy).snapshot = PlayerStateNetworking.Capture(Player);

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || Main.gameMenu || Player.whoAmI != Main.myPlayer) return;
        if (PlayerStateNetworking.Capture(Player) != ((PlayerStateSyncPlayer)clientPlayer).snapshot)
            PlayerStateNetworking.Send(Mod, Player);
    }
}
