// 集中登錄群系原生敵怪與彈幕，供地表屏障判定例外。
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Occident.Common.Worlds;

// 在 SetStaticDefaults 明確登錄原生敵怪與彈幕型別；出生地點不代表原生身分，登錄跨世界保留、卸載時清除。
public sealed class SporeDreamNativeContent : ModSystem
{
    private static readonly HashSet<int> nativeNPCs=new();
    private static readonly HashSet<int> nativeProjectiles=new();
    internal static bool HasNativeNPCs => nativeNPCs.Count>0;
    public static void RegisterNPC(int type)
    {
        if(type<=0 || type>=NPCLoader.NPCCount) throw new ArgumentOutOfRangeException(nameof(type));
        nativeNPCs.Add(type);
    }
    public static void RegisterProjectile(int type)
    {
        if(type<=0 || type>=ProjectileLoader.ProjectileCount) throw new ArgumentOutOfRangeException(nameof(type));
        nativeProjectiles.Add(type);
    }
    public static bool IsNativeNPCType(int type) => nativeNPCs.Contains(type);
    public static bool IsNativeProjectileType(int type) => nativeProjectiles.Contains(type);
    public static bool IsNativeNPC(NPC npc) => nativeNPCs.Contains(npc.type)
        || (npc.realLife>=0 && npc.realLife<Main.maxNPCs && nativeNPCs.Contains(Main.npc[npc.realLife].type));
    public override void Unload() { nativeNPCs.Clear();nativeProjectiles.Clear(); }
}
