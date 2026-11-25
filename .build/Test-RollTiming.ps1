$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$header=@'
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using VesperMod.Features.Character.Attributes;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Networking;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
'@
$source=foreach($path in @('Features/Character/Attributes/StaminaState.cs','Integration/TModLoader/Players/StaminaPlayer.cs','Integration/TModLoader/Players/DodgeRollPlayer.cs','Networking/RollSync.cs')){
   ((Get-Content "$repo/$path" -Raw) -replace '(?m)^(using [^;]+;|#nullable enable)\r?\n','' -replace '(?m)^namespace ([\w.]+);','namespace $1 {') + [Environment]::NewLine + '}'
}
$stubs=@'
namespace Microsoft.Xna.Framework {
    public struct Vector2 { public float X,Y; public Vector2(float x,float y){X=x;Y=y;} public static Vector2 operator *(Vector2 a,float b)=>new(a.X*b,a.Y*b); }
    public static class MathHelper { public const float TwoPi=6.283185f, Pi=3.141592f; public static float Lerp(float a,float b,float u)=>a+(b-a)*u; }
}
namespace Terraria.ID { public static class NetmodeID { public const int SinglePlayer=0, MultiplayerClient=1, Server=2; } }
namespace Terraria.GameInput { public sealed class TriggersSet { public bool Left, Right; } }
namespace Terraria.DataStructures { public struct PlayerDrawSet { public bool headOnlyRender; public Vector2 rotationOrigin, Position; public float rotation; } }
namespace VesperMod { public static class VesperMod { public static ModKeybind RollKey = new(); } }
namespace Terraria {
    public static class Main {
        public static int netMode, myPlayer, maxPlayers=2;
        public static ulong GameUpdateCount;
        public static Player[] player={new(0),new(1)};
        public static bool gameMenu, playerInventory, ingameOptionsWindow, mapFullscreen, drawingPlayerChat, blockInput;
    }
    public sealed class Mount { public bool Active; }
    public sealed class Item {}
    public sealed class Player {
        public struct HurtInfo { public bool Dodgeable,PvP; }
        private readonly Dictionary<Type,ModPlayer> mods=new();
        public Player(int who) {whoAmI=who;}
        public bool active=true, dead, ghost, CCed, pulley, tongued, shimmering, shimmerWet, ItemAnimationActive, channel, mouseInterface;
        public bool controlLeft,controlRight,controlJump,controlHook,controlMount,controlUseItem,controlUseTile;
        public int whoAmI, grapCount, dashDelay, direction=1, width=20,height=42;
        public float gravDir=1;
        public Vector2 position, velocity, Size;
        public Mount mount=new();
        public T GetModPlayer<T>() where T:ModPlayer,new() {
            if (!mods.TryGetValue(typeof(T),out var m)) { m=new T{Player=this}; mods.Add(typeof(T),m); }
            return (T)m;
        }
        public bool TryGetModPlayer<T>(out T m) where T:ModPlayer,new(){m=GetModPlayer<T>();return true;}
    }
    public static class Collision { public static bool Grounded=true; public static Vector2 TileCollision(Vector2 a,Vector2 step,int w,int h,int gravDir)=>Grounded?default:step; }
}
namespace Terraria.ModLoader {
    public sealed class ModKeybind { public bool JustPressed=true; }
    public sealed class Mod { public ModPacket GetPacket()=>new(); }
    public sealed class ModPacket : BinaryWriter {
        private readonly MemoryStream buffer;
        public static List<(byte[] Bytes,int To,int Ignore)> Sent = new();
        public ModPacket():this(new MemoryStream()){}
        private ModPacket(MemoryStream stream):base(stream){buffer=stream;}
        public void Send(int toWho=-1,int ignoreClient=-1)=>Sent.Add((buffer.ToArray(),toWho,ignoreClient));
    }
    public abstract class ModPlayer {
        public Player Player=null!;
        public Mod Mod=new();
        public virtual void Initialize(){} public virtual void PostUpdate(){} public virtual void OnEnterWorld(){} public virtual void OnRespawn(){}
        public virtual void PlayerDisconnect(){} public virtual void ProcessTriggers(TriggersSet t){} public virtual void SetControls(){}
        public virtual void PreUpdateMovement(){} public virtual bool CanUseItem(Item item)=>true;
        public virtual bool FreeDodge(Player.HurtInfo info)=>false; public virtual void OnHurt(Player.HurtInfo info){}
        public virtual void UpdateDead(){} public virtual void ModifyDrawInfo(ref PlayerDrawSet draw) {}
    }
}
'@
$test=@'

public static class RollTiming
{
    private static readonly Player.HurtInfo Dodgeable = new() { Dodgeable = true };
    private static readonly Item Item = new();
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static byte[] Take()
    {
        Check(ModPacket.Sent.Count == 1, "Expected exactly one packet");
        var data = ModPacket.Sent[0].Bytes;
        ModPacket.Sent.Clear();
        return data;
    }

    private static void Deliver(byte[] data, int sender)
    {
        using var reader = new BinaryReader(new MemoryStream(data));
        Check(reader.ReadByte() == RollSync.MessageType, "Wrong packet ID");
        RollSync.Receive(reader, sender);
    }

    private sealed class Session
    {
        internal readonly Player Owner = new(0);
        internal readonly Player Host = new(0);
        internal readonly DodgeRollPlayer Client;
        internal readonly DodgeRollPlayer Server;

        internal Session()
        {
            Main.GameUpdateCount = 0;
            Collision.Grounded = true;
            ModPacket.Sent.Clear();
            Main.player[1] = new Player(1);
            Client = Owner.GetModPlayer<DodgeRollPlayer>();
            Server = Host.GetModPlayer<DodgeRollPlayer>();
        }

        internal void OnServer(ulong tick, byte[]? packet = null, int sender = 0)
        {
            Main.netMode = NetmodeID.Server;
            Main.myPlayer = 255;
            Main.player[0] = Host;
            Main.GameUpdateCount = tick;
            if (packet is not null) Deliver(packet, sender);
        }

        internal void OnOwner(ulong tick, byte[]? packet = null)
        {
            Main.netMode = NetmodeID.MultiplayerClient;
            Main.myPlayer = 0;
            Main.player[0] = Owner;
            Main.GameUpdateCount = tick;
            if (packet is not null) Deliver(packet, 255);
        }

        internal byte[] Request()
        {
            OnOwner(0);
            Client.ProcessTriggers(new TriggersSet());
            return Take();
        }

        internal byte[] Offer(byte[] request, ulong tick)
        {
            OnServer(tick, request);
            Check(ModPacket.Sent[0].To == 0 && Server.RollId == 0 && Server.Stamina.Current == 50,
                "Offer started or charged the server roll");
            return Take();
        }

        internal byte[] Confirm(byte[] offer, ulong tick)
        {
            OnOwner(tick, offer);
            Client.PreUpdateMovement();
            Check(Client.Stamina.Current == 50 && !Client.FreeDodge(Dodgeable),
                "Offer activated the owner before the grant");
            return Take();
        }

        internal byte[] Grant(byte[] confirm, ulong tick)
        {
            OnServer(tick, confirm);
            Check(ModPacket.Sent[0].To == 0 && Server.RollId == 0 && Server.Stamina.Current == 50,
                "Grant started or charged the server roll");
            return Take();
        }

        internal byte[] Start(byte[] grant, ulong tick)
        {
            OnOwner(tick, grant);
            Check(!Client.FreeDodge(Dodgeable), "Grant started outside a movement tick");
            Client.PreUpdateMovement();
            Check(Client.Stamina.Current == 38 && Client.FreeDodge(Dodgeable) &&
                !Client.FreeDodge(new Player.HurtInfo{Dodgeable=true,PvP=true}),
                "Owner start did not apply the normal dodge rules");
            return Take();
        }
    }

    public static string AtLatency(ulong reply, ulong upstream)
    {
        var s = new Session();
        byte[] request = s.Request();
        byte[] offer = s.Offer(request, upstream);
        s.OnServer(upstream, request);
        Check(ModPacket.Sent.Count == 0, "Duplicate request produced an offer");
        byte[] confirm = s.Confirm(offer, reply);
        byte[] grant = s.Grant(confirm, reply + upstream);
        s.OnServer(reply + upstream, confirm);
        Check(ModPacket.Sent.Count == 0, "Duplicate confirmation reissued a grant");

        ulong startTick = reply * 2;
        byte[] started = s.Start(grant, startTick);
        s.OnServer(startTick + upstream, started, 1);
        Check(s.Server.Stamina.Current == 50 && ModPacket.Sent.Count == 0, "Wrong sender committed an offer");
        s.OnServer(startTick + upstream, started);
        byte[] committed = Take();
        ulong id = s.Server.RollId;
        Check(id != 0 && s.Server.Stamina.Current == 38, "Server did not debit exactly 12");
        s.OnServer(startTick + upstream, started);
        Check(s.Server.Stamina.Current == 38 && ModPacket.Sent.Count == 0, "Duplicate start charged a second time");

        int dodge = 0;
        int motion = 0;
        byte[]? ended = null;
        for (ulong tick = 0; tick < 18; tick++)
        {
            s.OnOwner(startTick + tick);
            if (tick == reply) s.OnOwner(startTick + tick, committed);
            if (tick != 0) s.Client.PreUpdateMovement();
            if (!s.Client.CanUseItem(Item)) motion++;
            if (s.Client.FreeDodge(Dodgeable)) dodge++;
            s.Owner.position.X += 1;
            s.Client.PostUpdate();
            if (ModPacket.Sent.Count != 0) ended = Take();
        }

        Check(dodge == 10 && motion == 18 && ended is not null && s.Client.CanUseItem(Item), "Owner used the wrong timing");
        s.OnServer(startTick + upstream + 17, ended);
        byte[] end = Take();
        Check(s.Server.CanUseItem(Item) && s.Server.Stamina.Current == 38, "Server did not close roll");
        s.OnServer(startTick + upstream + 18, ended);
        Check(ModPacket.Sent.Count == 0, "Duplicate end rebroadcast");
        s.OnOwner(startTick + reply + 18, committed);
        s.OnOwner(startTick + reply + 18, end);
        Check(s.Client.CanUseItem(Item) && !s.Client.FreeDodge(Dodgeable), "Late snapshot reopened a roll");
        return $"Reply {reply,3} ticks ({upstream,3} upstream): start at {startTick,3}, server debit 12 once, owner motion {motion}, dodge {dodge}, closed";
    }

    public static string Boundaries()
    {
        var s = new Session();
        var offer = s.Offer(s.Request(), 0);
        s.OnOwner(120, offer);
        s.Client.PreUpdateMovement();
        s.Client.PostUpdate();
        Check(ModPacket.Sent.Count == 0 && s.Client.Stamina.Current == 50 && s.Server.Stamina.Current == 50 &&
            !s.Client.FreeDodge(Dodgeable), "Offer at timeout was accepted");

        s = new Session();
        offer = s.Offer(s.Request(), 0);
        var confirm = s.Confirm(offer, 20);
        var grant = s.Grant(confirm, 20);
        s.OnOwner(140, grant);
        s.Client.PreUpdateMovement();
        s.Client.PostUpdate();
        var cancel = Take();
        s.OnServer(140, cancel);
        Check(s.Server.Stamina.Current == 50 && s.Client.Stamina.Current == 50 && !s.Client.FreeDodge(Dodgeable),
            "Grant at deadline started or charged");

        s = new Session();
        offer = s.Offer(s.Request(), 0);
        confirm = s.Confirm(offer, 1);
        Collision.Grounded = false;
        s.OnServer(2, confirm);
        var rejection = Take();
        s.OnOwner(3, rejection);
        Check(s.Client.Stamina.Current == 50 && s.Server.Stamina.Current == 50 && !s.Client.FreeDodge(Dodgeable),
            "Server failed to revalidate grounding");
        Collision.Grounded = true;

        s = new Session();
        offer = s.Offer(s.Request(), 0);
        grant = s.Grant(s.Confirm(offer, 1), 2);
        s.Owner.mount.Active = true;
        s.OnOwner(3, grant);
        s.Client.PreUpdateMovement();
        cancel = Take();
        s.OnServer(4, cancel);
        Check(s.Server.Stamina.Current == 50 && !s.Client.FreeDodge(Dodgeable), "Ineligible owner started from a grant");

        s = new Session();
        offer = s.Offer(s.Request(), 0);
        grant = s.Grant(s.Confirm(offer, 1), 2);
        s.OnOwner(3);
        s.Owner.dead = true;
        s.Client.UpdateDead();
        cancel = Take();
        s.OnServer(4, cancel);
        s.Server.PlayerDisconnect();
        s.Owner.dead = false;
        s.OnOwner(5);
        s.Client.OnEnterWorld();
        s.OnOwner(6, grant);
        s.Client.PreUpdateMovement();
        Check(s.Server.Stamina.Current == 50 && s.Client.Stamina.Current == 50 &&
            !s.Client.FreeDodge(Dodgeable) && ModPacket.Sent.Count == 0, "Previous session grant activated after death/reconnect");

        s = new Session();
        offer = s.Offer(s.Request(), 0);
        grant = s.Grant(s.Confirm(offer, 1), 2);
        var started = s.Start(grant, 3);
        s.OnServer(242, started);
        rejection = Take();
        s.OnOwner(243, rejection);
        Check(s.Server.Stamina.Current == 50 && s.Client.Stamina.Current == 50 && !s.Client.FreeDodge(Dodgeable),
            "Expired server lease committed");

        s = new Session();
        offer = s.Offer(s.Request(), 0);
        grant = s.Grant(s.Confirm(offer, 1), 2);
        started = s.Start(grant, 3);
        s.OnServer(8, started);
        Take();
        for (ulong tick = 8; tick < 25; tick++)
        {
            s.OnServer(tick);
            s.Server.PostUpdate();
            Check(!s.Server.CanUseItem(Item) && ModPacket.Sent.Count == 0, "Server ended before 18 committed ticks");
        }
        s.OnServer(25);
        s.Server.PostUpdate();
        Take();
        Check(s.Server.CanUseItem(Item) && s.Server.Stamina.Current == 38, "Server fallback did not end or charged again");

        s = new Session();
        s.OnOwner(0);
        Main.netMode = NetmodeID.SinglePlayer;
        s.Client.ProcessTriggers(new TriggersSet());
        Check(s.Client.Stamina.Current == 38 && s.Client.FreeDodge(Dodgeable) &&
            ModPacket.Sent.Count == 0, "Single-player started through networking");
        return "Boundaries passed: offer/grant timeout, cancel, server/owner eligibility, death/reconnect, lease expiry and single-player";
    }
}

'@
Add-Type -TypeDefinition ((@($header,$stubs)+$source+@($test)) -join [Environment]::NewLine)
foreach ($reply in 0, 6, 20, 36, 119) { [RollTiming]::AtLatency($reply, [Math]::Floor($reply / 2)) }
[RollTiming]::AtLatency(36, 32)
[RollTiming]::Boundaries()
