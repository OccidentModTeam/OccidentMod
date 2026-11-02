using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Occident.Common.Worlds;

public sealed class WorldOptionsSystem : ModSystem
{
    public WorldOptions Options { get; private set; }
    // 只在確認建立時設定，ClearWorld 不清除這份待生成資料。
    internal static WorldOptions? PendingCreation;

    public override void ClearWorld() => Options = default;

    public override void PreWorldGen()
    {
        Options = PendingCreation ?? default;
        PendingCreation = null;
    }

    public override void SaveWorldData(TagCompound tag) => Options.Save(tag);
    public override void SaveWorldHeader(TagCompound tag) => Options.Save(tag);
    public override void LoadWorldData(TagCompound tag) => Options = WorldOptions.Read(tag);
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)Options.Difficulty);
        writer.Write(Options.ExtraLarge);
    }
    public override void NetReceive(BinaryReader reader)
    {
        int difficulty = reader.ReadByte();
        Options = new(difficulty is >= 0 and <= 3 ? (OccidentWorldDifficulty)difficulty : OccidentWorldDifficulty.None,
            reader.ReadBoolean());
    }
    public override void Unload() => PendingCreation = null;
}
