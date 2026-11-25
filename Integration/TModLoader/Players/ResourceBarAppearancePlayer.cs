using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Integration.TModLoader.Players;

/// <summary>Appearance switches for the Occident resource display set.</summary>
public sealed class ResourceBarAppearancePlayer : ModPlayer
{
    internal const byte MessageType = 8;
    private const byte ManaFlag = 1;
    private const byte StaminaFlag = 2;
    private const byte AllowedFlags = ManaFlag | StaminaFlag;
    private const string SaveKey = "resourceBarAppearance";

    private byte appearance;

    public bool ManaUpgraded
    {
        get => (appearance & ManaFlag) != 0;
        set => SetFlag(ManaFlag, value);
    }

    public bool StaminaUpgraded
    {
        get => (appearance & StaminaFlag) != 0;
        set => SetFlag(StaminaFlag, value);
    }

    private void SetFlag(byte flag, bool value) =>
        appearance = (byte)(value ? appearance | flag : appearance & ~flag);

    public override void SaveData(TagCompound tag)
    {
        if (appearance != 0)
            tag[SaveKey] = appearance;
    }

    public override void LoadData(TagCompound tag) => appearance = (byte)(tag.GetByte(SaveKey) & AllowedFlags);

    public override void CopyClientState(ModPlayer targetCopy) =>
        ((ResourceBarAppearancePlayer)targetCopy).appearance = appearance;

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        if (((ResourceBarAppearancePlayer)clientPlayer).appearance != appearance)
            Send(-1, -1);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => Send(toWho, fromWho);

    public override void PlayerDisconnect() => appearance = 0;

    private void Send(int toWho, int ignore)
    {
        var packet = Mod.GetPacket();
        packet.Write(MessageType);
        if (Main.netMode == NetmodeID.Server)
            packet.Write((byte)Player.whoAmI);
        packet.Write(appearance);
        packet.Send(toWho, ignore);
    }

    internal static void Receive(BinaryReader reader, int sender)
    {
        bool server = Main.netMode == NetmodeID.Server;
        if (!server && Main.netMode != NetmodeID.MultiplayerClient)
            return;

        int index = server ? sender : reader.ReadByte();
        byte flags = reader.ReadByte();
        if ((flags & ~AllowedFlags) != 0 || (uint)index >= Main.maxPlayers ||
            (!server && index == Main.myPlayer) ||
            !Main.player[index].TryGetModPlayer(out ResourceBarAppearancePlayer player))
            return;

        player.appearance = flags;
        if (server)
            player.Send(-1, index);
    }
}
