#nullable enable

using System.IO;
using VesperMod.Features.Character.Classes;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Networking;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;

namespace VesperMod;

public class VesperMod : Mod
{
    internal static ModKeybind? ReleasePotentialKey { get; private set; }
    internal static ModKeybind? RollKey { get; private set; }

    public override void Load()
    {
        ClassRegistry.RegisterAll();
        if (!Main.dedServ)
        {
            ReleasePotentialKey = KeybindLoader.RegisterKeybind(this, "ReleasePotential", Keys.G);
            RollKey = KeybindLoader.RegisterKeybind(this, "Roll", Keys.LeftAlt);
        }
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        try
        {
            byte message = reader.ReadByte();
            switch (message)
            {
                case PokaAnimationSync.MessageType:
                    PokaAnimationSync.Receive(reader, whoAmI);
                    break;
                case PokaCallSync.MessageType:
                    PokaCallSync.Receive(reader);
                    break;
                case RollSync.MessageType:
                    RollSync.Receive(reader, whoAmI);
                    break;
                case ResourceBarAppearancePlayer.MessageType:
                    ResourceBarAppearancePlayer.Receive(reader, whoAmI);
                    break;
                default:
                    PotentialSync.Receive(reader, whoAmI, message);
                    break;
            }
        }
        catch (EndOfStreamException)
        {
            // Ignore incomplete packets.
        }
    }

    public override void Unload()
    {
        ReleasePotentialKey = null;
        RollKey = null;
        ClassRegistry.Clear();
    }
}
