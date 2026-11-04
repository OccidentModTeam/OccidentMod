// 模組主入口；多人訊息交由 PacketRouter 統一分派。
using System.IO;
using Terraria.ModLoader;
using Occident.Common.Networking;

namespace Occident
{
	public class Occident : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI)
			=> PacketRouter.Receive(this, reader, whoAmI);
	}
}
