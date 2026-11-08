using System.IO;
using OccidentMod.Gameplay.Classes;

namespace OccidentMod.Networking;

/// <summary>
///     定義玩家職業與休閒模式的網路同步資料。
///     Defines network synchronization data for player
///     classes and casual mode.
/// </summary>
internal readonly record struct PlayerSyncState(
    ClassType Class,
    bool HasInitialClass,
    bool CasualMode)
{
    // 使用集中管理的封包 ID。
    // Use the centralized packet identifier.
    internal const byte MessageType =
        PacketIds.PlayerSyncState;

    /// <summary>
    ///     將玩家狀態寫入封包。
    ///     Write the player state into a packet.
    /// </summary>
    internal void Write(BinaryWriter writer)
    {
        // Bit 0：首次選職；Bit 1：休閒模式。
        // Bit 0: initial class; Bit 1: casual mode.
        writer.Write((byte)Class);

        writer.Write((byte)(
            (HasInitialClass ? 1 : 0) |
            (CasualMode ? 2 : 0)
        ));
    }

    /// <summary>
    ///     正規化玩家狀態，保留已確認的首次選職。
    ///     Normalize player state while preserving
    ///     confirmed initial class selection.
    /// </summary>
    internal PlayerSyncState Normalize(
        bool classicPlayer,
        bool initialClassAlreadyChosen)
    {
        return this with
        {
            CasualMode = CasualMode && classicPlayer,
            HasInitialClass =
            HasInitialClass || initialClassAlreadyChosen
        };
    }

    /// <summary>
    ///     讀取並驗證玩家狀態封包。
    ///     Read and validate a player state packet.
    /// </summary>
    internal static bool TryReadPacket(
        BinaryReader reader,
        int maxPlayers,
        bool fromClient,
        int sender,
        out int playerIndex,
        out PlayerSyncState state,
        byte? messageType = null)
    {
        playerIndex = -1;
        state = default;

        try
        {
            if ((messageType ?? reader.ReadByte()) != MessageType)
                return false;

            int claimedPlayer = reader.ReadByte();
            var classId = (ClassType)reader.ReadByte();
            var flags = reader.ReadByte();

            // 驗證玩家索引、旗標與職業識別。
            // Validate player index, flags and class ID.
            if (claimedPlayer >= maxPlayers ||
                (flags & ~3) != 0 ||
                (classId != ClassType.None &&
                 !ClassCatalog.IsValid(classId)))
                return false;

            // 客戶端只能更新自己的狀態。
            // Clients may only update their own state.
            if (fromClient &&
                (sender < 0 ||
                 sender >= maxPlayers ||
                 claimedPlayer != sender))
                return false;

            playerIndex = claimedPlayer;

            state = new PlayerSyncState(
                classId,
                (flags & 1) != 0,
                (flags & 2) != 0
            );

            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }
}