// 玩家同步資料的封包格式與合法性檢查。
using System.IO;
using Occident.Common.Classes;

namespace Occident.Common.Networking;

internal readonly record struct PlayerSyncState(ClassType Class, bool HasInitialClass, bool CasualMode)
{
    internal const byte MessageType = 0;

    // 旗標的第 0 位代表已初選，第 1 位代表休閒模式；讀寫順序必須一致。
    internal void Write(BinaryWriter writer)
    {
        writer.Write((byte)Class);
        writer.Write((byte)((HasInitialClass ? 1 : 0) | (CasualMode ? 2 : 0)));
    }

    // 保留伺服器已知的初選狀態，且只有經典角色能啟用休閒模式。
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
            // 客戶端只能更新自己的玩家欄位，必須同時核對封包代號與實際發送者。
            if (fromClient && (sender < 0 || sender >= maxPlayers || claimedPlayer != sender)) return false;
            playerIndex = claimedPlayer;
            state = new(classId, (flags & 1) != 0, (flags & 2) != 0);
            return true;
        }
        catch (EndOfStreamException) { return false; }
    }
}
