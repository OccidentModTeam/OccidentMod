using Occident.Common.Networking;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Common.Players;

// One small snapshot covers both ModPlayers, avoiding duplicate packets when both change.
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
