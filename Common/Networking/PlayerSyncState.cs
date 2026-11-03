using System.IO;
using Occident.Common.Classes;

namespace Occident.Common.Networking;

internal readonly record struct PlayerSyncState(ClassType Class, bool HasInitialClass, bool CasualMode)
{
    internal const byte MessageType = 0;

    internal void Write(BinaryWriter writer)
    {
        writer.Write((byte)Class);
        writer.Write((byte)((HasInitialClass ? 1 : 0) | (CasualMode ? 2 : 0)));
    }

    internal PlayerSyncState Normalize(bool classicPlayer, bool initialClassAlreadyChosen) =>
        this with { CasualMode = CasualMode && classicPlayer,
            HasInitialClass = HasInitialClass || initialClassAlreadyChosen };

    internal static bool TryReadPacket(BinaryReader reader, int maxPlayers, bool fromClient, int sender,
        out int playerIndex, out PlayerSyncState state, byte? messageType=null)
    {
        playerIndex = -1;
        state = default;
        try {
            if ((messageType ?? reader.ReadByte()) != MessageType) return false;
            int claimedPlayer = reader.ReadByte();
            var classId = (ClassType)reader.ReadByte();
            byte flags = reader.ReadByte();
            if (claimedPlayer >= maxPlayers || (flags & ~3) != 0
                || (classId != ClassType.None && !ClassCatalog.IsValid(classId))) return false;
            // A client may only update its own slot; never trust the claimed index alone.
            if (fromClient && (sender < 0 || sender >= maxPlayers || claimedPlayer != sender)) return false;
            playerIndex = claimedPlayer;
            state = new(classId, (flags & 1) != 0, (flags & 2) != 0);
            return true;
        }
        catch (EndOfStreamException) { return false; }
    }
}
