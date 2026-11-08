#nullable enable

namespace OccidentMod.Networking;

/// <summary>
///     模組網路封包 ID。
///     Centralized mod packet identifiers.
/// </summary>
internal static class PacketIds
{
    internal const byte PlayerSyncState = 0;
    internal const byte SporeDreamTeleportRequest = 1;
    internal const byte SporeDreamCurvedSync = 2;
    internal const byte SporeDreamSpringRequest = 3;
    internal const byte SporeDreamSpringAnimation = 4;

    // 客戶端請求種植完整矮菇。
    // Client requests dwarf mushroom placement.
    internal const byte SporeDreamDwarfPlaceRequest = 5;
}