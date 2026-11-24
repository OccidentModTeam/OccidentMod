$ErrorActionPreference = 'Stop'
$paths = @(
    'Features/Character/Classes/PotentialState.cs'
    'Integration/TModLoader/PotentialChargePlayer.cs'
    'Networking/PotentialSync.cs'
)
$source = foreach ($path in $paths) {
    $text = Get-Content -LiteralPath (Join-Path $PSScriptRoot "../$path") -Raw
    ($text -replace '(?m)^\s*(?:#nullable|using) [^\r\n]+', '' -replace '(?m)^namespace ([^;]+);', 'namespace $1 {') + '}'
}
$imports = @'
using System;
using System.IO;
using OccidentMod.Features.Character.Classes;
using OccidentMod.Integration.TModLoader;
using OccidentMod.Networking;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Main = Terraria.Main;
using Terraria.GameInput;
using Terraria.ModLoader.IO;
'@
$stubs = @'
namespace Terraria
{
    public static class Main
    {
        public static int netMode;
        public static ulong GameUpdateCount;
        public const int maxPlayers = 2;
        public static Player[] player = { new Player(), new Player { whoAmI = 1 } };
    }

    public class Player
    {
        public int whoAmI;
        public bool active = true, dead, ghost;
        public ModPlayer Charge;
        public bool TryGetModPlayer<T>(out T charge) where T : ModPlayer
        {
            charge = Charge as T;
            return charge != null;
        }
    }
}

namespace OccidentMod
{
    public class OccidentMod
    {
        public static ModKeybind ReleasePotentialKey { get; } = new ModKeybind();
    }
}
namespace Terraria.GameInput { public class TriggersSet { } }
namespace Terraria.ModLoader.IO
{
    public class TagCompound : System.Collections.Generic.Dictionary<string, object>
    {
        public float GetFloat(string key) => TryGetValue(key, out var value) ? (float)value : 0f;
    }
}
namespace Terraria.ID
{
    public static class NetmodeID
    {
        public const int SinglePlayer = 0, MultiplayerClient = 1, Server = 2;
    }
}

namespace Terraria.ModLoader
{
    public class ModPlayer
    {
        public Player Player { get; set; }
        public Mod Mod { get; } = new Mod();
        public virtual void OnEnterWorld() { }
        public virtual void SaveData(TagCompound tag) { }
        public virtual void LoadData(TagCompound tag) { }
        public virtual void ProcessTriggers(TriggersSet triggersSet) { }
        public virtual void PostUpdate() { }
        public virtual void UpdateDead() { }
        public virtual void SyncPlayer(int toWho, int fromWho, bool newPlayer) { }
    }

    public class Mod
    {
        public ModPacket GetPacket() => new ModPacket();
    }

    public class ModKeybind { public bool JustPressed { get; set; } }

    public class ModPacket : BinaryWriter
    {
        public static int Sends;
        public ModPacket() : base(new MemoryStream()) { }
        public void Send(int toWho = -1) => Sends++;
    }
}
'@
$tests = @'
public static class PotentialSyncChecks
{
    public static void Run()
    {
        var first = new PotentialChargePlayer { Player = Main.player[0] };
        var second = new PotentialChargePlayer { Player = Main.player[1] };
        Main.player[0].Charge = first;
        Main.player[1].Charge = second;

        Main.netMode = NetmodeID.Server;
        Receive(2, 0, 100, 99);
        Receive(1, 0, 101, 100);
        Expect(first.Potential.Current == 0, "Server accepted a client snapshot.");
        Main.GameUpdateCount = 1;
        first.PostUpdate();
        Expect(first.Potential.Current > 0 && second.Potential.Current == 0, "Server did not charge only the updated player.");
        Expect(ModPacket.Sends == 0, "Server sent an update outside the sync interval.");
        Main.GameUpdateCount = 12;
        first.PostUpdate();
        float serverCharge = first.Potential.Current;
        Expect(ModPacket.Sends == 1, "Server did not send a timed update.");

        Main.netMode = NetmodeID.MultiplayerClient;
        first.PostUpdate();
        first.SyncPlayer(-1, -1, false);
        Expect(first.Potential.Current == serverCharge && ModPacket.Sends == 1, "Client awarded or published charge.");

        Receive(2, 0, 3, 40);
        Receive(2, 0, 2, 80);
        Receive(2, 0, 3, 85);
        Receive(2, 0, 4, float.NaN);
        Receive(2, 0, 4, 101);
        Receive(2, 0, 4, 75, 2);
        Receive(1, 0, 4, 70);
        Receive(2, 2, 4, 70);
        Receive(2, 0, 4, 70, truncated: true);
        Expect(first.Potential.Current == 40 && second.Potential.Current == 0, "Invalid snapshot changed the mirror.");
        Receive(2, 0, 4, 45);
        Expect(first.Potential.Current == 45, "Invalid snapshot consumed a revision.");

        Main.netMode = NetmodeID.Server;
        RequestRelease(sender: 0);
        Expect(!first.Potential.IsActive, "Release request bypassed the full requirement.");
        second.Potential.Add(100);
        RequestRelease(sender: 0);
        Expect(!second.Potential.IsActive, "Release request selected a different player.");
        RequestRelease(sender: 1);
        Expect(second.Potential.IsActive && ModPacket.Sends == 2, "Server did not start the sender's release.");
        second.PostUpdate();
        float afterOneTick = second.Potential.Current;
        RequestRelease(sender: 1);
        second.PostUpdate();
        Expect(second.Potential.Current < afterOneTick && ModPacket.Sends == 2, "Duplicate release restarted the drain.");

        Main.netMode = NetmodeID.SinglePlayer;
        first.Potential.Reset();
        for (int i = 0; i < 600; i++)
            first.PostUpdate();
        Expect(first.Potential.Current == 50, "Expected half charge after 10 seconds.");
        for (int i = 0; i < 599; i++)
            first.PostUpdate();
        Expect(!first.Potential.IsFull, "Potential filled before 20 seconds.");
        first.PostUpdate();
        Expect(first.Potential.IsFull && ModPacket.Sends == 2, "Potential did not fill at exactly 20 seconds.");
        first.PostUpdate();
        Expect(first.Potential.Current == 100, "Potential exceeded its maximum.");

        first.Potential.Reset();
        first.Potential.Add(46.75f);
        var tag = new TagCompound();
        first.SaveData(tag);
        var loaded = new PotentialChargePlayer { Player = Main.player[0] };
        loaded.LoadData(tag);
        loaded.OnEnterWorld();
        Expect(loaded.Potential.Current == 46.75f, "Fractional potential did not survive re-entry.");
        loaded.TryRelease();
        Expect(!loaded.Potential.IsActive, "Partial potential was released.");
        loaded.Potential.Add(100);
        global::OccidentMod.OccidentMod.ReleasePotentialKey.JustPressed = true;
        loaded.ProcessTriggers(new TriggersSet());
        Expect(loaded.Potential.IsActive && loaded.Potential.Current == 100, "Full release did not start.");
        for (int i = 0; i < 150; i++)
            loaded.PostUpdate();
        Expect(loaded.Potential.IsActive && loaded.Potential.Current == 50, "Mid-release drain or charge lock failed.");
        loaded.SaveData(tag);
        for (int i = 0; i < 150; i++)
            loaded.PostUpdate();
        Expect(!loaded.Potential.IsActive && loaded.Potential.Current == 0, "Release did not finish.");
        loaded.PostUpdate();
        Expect(loaded.Potential.Current > 0, "Passive charging did not resume after release.");
        var reloaded = new PotentialChargePlayer { Player = Main.player[0] };
        reloaded.LoadData(tag);
        Expect(!reloaded.Potential.IsActive && reloaded.Potential.Current == 50, "Mid-release save was not restored.");
    }

    private static void Receive(byte kind, byte index, ulong revision, float current, byte active = 0, bool truncated = false)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(kind);
        writer.Write(index);
        writer.Write(revision);
        writer.Write(current);
        if (!truncated)
            writer.Write(active);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        try { PotentialSync.Receive(reader, 0, reader.ReadByte()); }
        catch (EndOfStreamException) { }
    }

    private static void RequestRelease(int sender)
    {
        using var reader = new BinaryReader(new MemoryStream(new byte[] { 3 }));
        PotentialSync.Receive(reader, sender, reader.ReadByte());
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
'@
Add-Type -TypeDefinition (($imports, $stubs, ($source -join "`n"), $tests) -join "`n")
[PotentialSyncChecks]::Run()
'Potential sync authority and snapshot tests passed.'
