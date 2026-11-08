using System.IO;
using OccidentMod.Networking;
using Terraria.ModLoader;

namespace Occident;

public sealed class Occident : Mod
{
    // Route incoming packets through the refactored networking system.
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        PacketRouter.Receive(this, reader, whoAmI);
    }
}