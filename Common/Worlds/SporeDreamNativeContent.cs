using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Occident.Common.Worlds;

// Register explicit NPC/projectile types from SetStaticDefaults. A spawn location
// alone never grants native status. Registrations survive world changes, not unloads.
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
