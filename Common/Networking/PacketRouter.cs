// 模組網路封包的入口，依訊息代號與客戶端／伺服器身分分派。
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Common.Worlds;

namespace Occident.Common.Networking;

internal static class PacketRouter
{
    internal const byte SporeDreamTeleportRequest=1;
    // 先讀訊息代號，再按來源身分路由；各處理器接到的讀取位置已越過代號。
    internal static void Receive(Mod mod,BinaryReader reader,int whoAmI)
    {
        byte message;
        try { message=reader.ReadByte(); }
        catch(EndOfStreamException) { return; }
        if(message==PlayerSyncState.MessageType)
            PlayerStateNetworking.Receive(mod,reader,whoAmI,message);
        else if(message==SporeDreamCurvedSystem.SyncMessage && Main.netMode==NetmodeID.MultiplayerClient)
            SporeDreamCurvedSystem.Instance.NetReceive(reader);
        else if(message==SporeDreamCurvedSystem.SpringRequest && Main.netMode==NetmodeID.Server)
            SporeDreamCurvedSystem.Instance.ReceiveSpring(reader,whoAmI,true);
        else if(message==SporeDreamCurvedSystem.SpringAnimation && Main.netMode==NetmodeID.MultiplayerClient)
            SporeDreamCurvedSystem.Instance.ReceiveSpring(reader,whoAmI,false);
        else if(message==SporeDreamTeleportRequest && Main.netMode==NetmodeID.Server
            && whoAmI>=0 && whoAmI<Main.maxPlayers)
            ModContent.GetInstance<SporeDreamTestTeleportSystem>().TryTeleport(Main.player[whoAmI]);
    }
}
