#nullable enable

using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.World;

/// <summary>
///     管理自訂世界設定的生命週期、儲存與多人同步。
///     Manages the lifecycle, persistence, and multiplayer
///     synchronization of custom world options.
/// </summary>
public sealed class WorldOptionsSystem : ModSystem
{
    /// <summary>
    ///     下一個新世界的待建立設定。
    ///     Pending options for the next world creation.
    /// </summary>
    internal static WorldOptions? PendingCreation;

    /// <summary>
    ///     目前世界的自訂設定。
    ///     Custom options for the current world.
    /// </summary>
    public WorldOptions Options { get; private set; }

    /// <summary>
    ///     清除已載入世界的設定。
    ///     Clear current world options.
    /// </summary>
    public override void ClearWorld()
    {
        Options = default;

        // 不在此清除 PendingCreation。
        // clearWorld 會在建立新世界時執行，
        // 尺寸掛鉤仍需要讀取待建立設定。
        //
        // Do not clear pending creation options here.
        // The world size hook still needs them.
    }

    /// <summary>
    ///     在世界生成前套用自訂選項。
    ///     Apply custom options before world generation.
    /// </summary>
    public override void PreWorldGen()
    {
        // v0.2.0 暫停超大型世界，保留舊存檔格式。
// Disable extra-large creation while preserving legacy save data.
        Options = (PendingCreation ?? default) with { ExtraLarge = false };

        // 保留待建立設定直到 PostWorldGen。
        // Preserve pending options until generation finishes.
        //
        // ExtraLargeWorldSizeSystem may need to read
        // PendingCreation during clearWorld.
    }

    /// <summary>
    ///     世界生成完成後清除待建立設定。
    ///     Clear pending options after generation completes.
    /// </summary>
    public override void PostWorldGen()
    {
        PendingCreation = null;
    }

    /// <summary>
    ///     載入既有世界時清除未使用的建立選項。
    ///     Clear stale creation options when loading a world.
    /// </summary>
    public override void OnWorldLoad()
    {
        PendingCreation = null;
    }

    /// <summary>
    ///     儲存目前世界設定。
    ///     Save current world options.
    /// </summary>
    public override void SaveWorldData(TagCompound tag)
    {
        Options.Save(tag);
    }

    /// <summary>
    ///     儲存世界列表可讀取的設定標頭。
    ///     Save options to the world selection header.
    /// </summary>
    public override void SaveWorldHeader(TagCompound tag)
    {
        Options.Save(tag);
    }

    /// <summary>
    ///     從世界存檔還原自訂選項。
    ///     Restore options from world save data.
    /// </summary>
    public override void LoadWorldData(TagCompound tag)
    {
        Options = WorldOptions.Read(tag);
    }

    /// <summary>
    ///     同步世界設定到多人客戶端。
    ///     Synchronize world options to clients.
    /// </summary>
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)Options.Difficulty);
        writer.Write(Options.ExtraLarge);
    }

    /// <summary>
    ///     讀取並驗證多人世界設定。
    ///     Read and validate synchronized world options.
    /// </summary>
    public override void NetReceive(BinaryReader reader)
    {
        int rawDifficulty = reader.ReadByte();
        var extraLarge = reader.ReadBoolean();

        var difficulty =
            ParseNetworkDifficulty(rawDifficulty);

        Options = new WorldOptions(
            difficulty,
            extraLarge);
    }

    /// <summary>
    ///     檢查多人傳入的難度值。
    ///     Validate network difficulty identifiers.
    /// </summary>
    private static OccidentWorldDifficulty
        ParseNetworkDifficulty(int value)
    {
        return value is >= 0 and <= 3
            ? (OccidentWorldDifficulty)value
            : OccidentWorldDifficulty.None;
    }

    /// <summary>
    ///     模組卸載時清除待建立設定。
    ///     Clear pending options when the mod unloads.
    /// </summary>
    public override void Unload()
    {
        PendingCreation = null;
    }
}