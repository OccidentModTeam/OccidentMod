using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Common.Worlds;

public sealed class SporeDreamConversionGuard : GlobalTile
{
    public override void OnTileConverted(int i,int j,int fromType,int toType,int conversionType)
    {
        if(!SporeDreamForestSystem.Instance.Contains(i,j)) return;
        // Preserve the original substrate as well as custom grass against evil/hallow conversion.
        if(conversionType!=BiomeConversionID.Corruption && conversionType!=BiomeConversionID.Crimson && conversionType!=BiomeConversionID.Hallow) return;
        Main.tile[i,j].TileType=(ushort)fromType;
        WorldGen.SquareTileFrame(i,j);
        if(Main.netMode==NetmodeID.Server) NetMessage.SendTileSquare(-1,i,j,1);
    }
}

public sealed class SporeDreamEnemyGuard : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private Vector2 safePosition;
    private bool hasSafePosition;
    public static bool Hostile(NPC npc) => !npc.friendly && !npc.townNPC && !NPCID.Sets.CountsAsCritter[npc.type];
    public override bool PreAI(NPC npc)
    {
        if(!Hostile(npc) || !SporeDreamForestSystem.Instance.Exists) return true;
        if(!SporeDreamForestSystem.Instance.IsSurfaceBarrier(npc.Hitbox)) { safePosition=npc.position; hasSafePosition=true; }
        Enforce(npc);
        return true;
    }
    public void Enforce(NPC npc)
    {
        if(!Hostile(npc)) return;
        var forest=SporeDreamForestSystem.Instance;
        if(!forest.Exists) return;
        Rectangle predicted=npc.Hitbox;
        predicted.Offset((int)npc.velocity.X,(int)npc.velocity.Y);
        bool crossing=false;
        // Sweep movement so fast enemies cannot pass entirely through the protected strip.
        int steps=Math.Clamp((int)Math.Ceiling(npc.velocity.Length()/8f),1,512);
        for(int step=0;step<=steps;step++) {
            Rectangle box=npc.Hitbox; box.Offset((int)(npc.velocity.X*step/steps),(int)(npc.velocity.Y*step/steps));
            if(forest.IsSurfaceBarrier(box)) { crossing=true; break; }
        }
        if(!crossing) return;
        npc.position=hasSafePosition && !forest.IsSurfaceBarrier(new Rectangle((int)safePosition.X,(int)safePosition.Y,npc.width,npc.height))
            ? safePosition : forest.Outside(npc.Hitbox);
        npc.velocity=Vector2.Zero;
        if(Main.netMode!=NetmodeID.MultiplayerClient) npc.netUpdate=true;
    }
    public override bool CanHitPlayer(NPC npc,Player target,ref int cooldownSlot)
        => !SporeDreamForestSystem.Instance.IsSurfaceBarrier(target.Hitbox);
    public override void EditSpawnRate(Player player,ref int spawnRate,ref int maxSpawns)
    {
        if(SporeDreamForestSystem.Instance.IsSurfaceBarrier(player.Hitbox)) { spawnRate=int.MaxValue; maxSpawns=0; }
    }
    public override void EditSpawnPool(IDictionary<int,float> pool,NPCSpawnInfo spawnInfo)
    {
        if(SporeDreamForestSystem.Instance.SurfaceContains(spawnInfo.SpawnTileX,spawnInfo.SpawnTileY-1)) pool.Clear();
    }
}

public sealed class SporeDreamProjectileGuard : GlobalProjectile
{
    public override bool PreAI(Projectile projectile)
    {
        if(!projectile.hostile || !SporeDreamForestSystem.Instance.Exists) return true;
        int steps=Math.Clamp((int)Math.Ceiling(projectile.velocity.Length()/8f),1,512);
        for(int s=0;s<=steps;s++) {
            Rectangle box=projectile.Hitbox; box.Offset((int)(projectile.velocity.X*s/steps),(int)(projectile.velocity.Y*s/steps));
            if(!SporeDreamForestSystem.Instance.IsSurfaceBarrier(box)) continue;
            projectile.Kill(); return false;
        }
        return true;
    }
    public override bool CanHitPlayer(Projectile projectile,Player target)
        => !SporeDreamForestSystem.Instance.IsSurfaceBarrier(target.Hitbox);
}

public sealed class SporeDreamBoundarySystem : ModSystem
{
    public override void PostUpdateNPCs()
    {
        if(!SporeDreamForestSystem.Instance.Exists) return;
        foreach(NPC npc in Main.ActiveNPCs) npc.GetGlobalNPC<SporeDreamEnemyGuard>().Enforce(npc);
    }
}
