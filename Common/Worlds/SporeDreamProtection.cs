using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
    public static bool Hostile(NPC npc) => !npc.friendly && !npc.townNPC && !NPCID.Sets.CountsAsCritter[npc.type];
    public override bool PreAI(NPC npc)
    {
        if(!Hostile(npc) || SporeDreamNativeContent.IsNativeNPC(npc) || !SporeDreamForestSystem.Instance.Exists) return true;
        Enforce(npc);
        return true;
    }
    public void Enforce(NPC npc)
    {
        if(Main.netMode==NetmodeID.MultiplayerClient || !Hostile(npc) || SporeDreamNativeContent.IsNativeNPC(npc)) return;
        var forest=SporeDreamForestSystem.Instance;
        if(!forest.Exists) return;
        if(Math.Max(npc.Hitbox.Right,npc.Hitbox.Right+npc.velocity.X)<=forest.Left*16
            || Math.Min(npc.Hitbox.Left,npc.Hitbox.Left+npc.velocity.X)>=forest.Right*16) return;
        bool crossing=false;
        // Sweep movement so fast enemies cannot pass entirely through the protected strip.
        int steps=Math.Clamp((int)Math.Ceiling(npc.velocity.Length()/8f),1,512);
        for(int step=0;step<=steps;step++) {
            Rectangle box=npc.Hitbox; box.Offset((int)(npc.velocity.X*step/steps),(int)(npc.velocity.Y*step/steps));
            if(forest.IsSurfaceBarrier(box)) { crossing=true; break; }
        }
        if(!crossing) return;
        // Worms and other realLife-linked bosses move as one group, preserving
        // body offsets, health, AI and velocity rather than stretching the chain.
        int root=npc.realLife>=0 && npc.realLife<Main.maxNPCs ? npc.realLife : npc.whoAmI;
        var group=new List<NPC>();
        Rectangle bounds=npc.Hitbox;
        foreach(NPC member in Main.ActiveNPCs) {
            if(member.whoAmI!=root && member.realLife!=root) continue;
            group.Add(member);bounds=Rectangle.Union(bounds,member.Hitbox);
        }
        if(group.Count==0) group.Add(npc);
        Vector2 destination=forest.OppositeBoundary(bounds,npc.velocity);
        Vector2 offset=destination-new Vector2(bounds.X,bounds.Y);
        // The far side can have a higher hillside. Raise the intact group to a
        // clear exit instead of inserting solid-colliding enemies into terrain.
        bool clear=false;
        for(int step=0;step<=160;step++) {
            clear=true;
            foreach(NPC member in group)
                if(!member.noTileCollide && Collision.SolidCollision(member.position+offset,member.width,member.height)) {clear=false;break;}
            if(clear) break;
            if(bounds.Top+offset.Y<=16) break;
            offset.Y-=16;
        }
        if(!clear) return;
        foreach(NPC member in group) {member.position+=offset;member.netUpdate=true;}
    }
    public override bool CanHitPlayer(NPC npc,Player target,ref int cooldownSlot)
        => SporeDreamNativeContent.IsNativeNPC(npc) || !SporeDreamForestSystem.Instance.IsSurfaceBarrier(target.Hitbox);
    public override void EditSpawnRate(Player player,ref int spawnRate,ref int maxSpawns)
    {
        if(!SporeDreamNativeContent.HasNativeNPCs && SporeDreamForestSystem.Instance.IsSurfaceBarrier(player.Hitbox)) { spawnRate=int.MaxValue; maxSpawns=0; }
    }
    public override void EditSpawnPool(IDictionary<int,float> pool,NPCSpawnInfo spawnInfo)
    {
        if(!SporeDreamForestSystem.Instance.SurfaceContains(spawnInfo.SpawnTileX,spawnInfo.SpawnTileY-1)) return;
        foreach(int type in new List<int>(pool.Keys)) if(!SporeDreamNativeContent.IsNativeNPCType(type)) pool.Remove(type);
    }
}

public sealed class SporeDreamProjectileGuard : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool nativeAttack;
    private bool IsNativeAttack(Projectile projectile) => nativeAttack || SporeDreamNativeContent.IsNativeProjectileType(projectile.type);
    public override void OnSpawn(Projectile projectile,IEntitySource source)
    {
        if(source is EntitySource_Parent parent) {
            nativeAttack=parent.Entity is NPC npc && SporeDreamNativeContent.IsNativeNPC(npc)
                || parent.Entity is Projectile origin && origin.GetGlobalProjectile<SporeDreamProjectileGuard>().IsNativeAttack(origin);
            if(nativeAttack && Main.netMode==NetmodeID.Server) projectile.netUpdate=true;
        }
    }
    public override void SendExtraAI(Projectile projectile,BitWriter bitWriter,BinaryWriter binaryWriter) => bitWriter.WriteBit(nativeAttack);
    public override void ReceiveExtraAI(Projectile projectile,BitReader bitReader,BinaryReader binaryReader) => nativeAttack=bitReader.ReadBit();
    public override bool PreAI(Projectile projectile)
    {
        if(!projectile.hostile || IsNativeAttack(projectile) || !SporeDreamForestSystem.Instance.Exists) return true;
        int steps=Math.Clamp((int)Math.Ceiling(projectile.velocity.Length()/8f),1,512);
        for(int s=0;s<=steps;s++) {
            Rectangle box=projectile.Hitbox; box.Offset((int)(projectile.velocity.X*s/steps),(int)(projectile.velocity.Y*s/steps));
            if(!SporeDreamForestSystem.Instance.IsSurfaceBarrier(box)) continue;
            projectile.Kill(); return false;
        }
        return true;
    }
    public override bool CanHitPlayer(Projectile projectile,Player target)
        => IsNativeAttack(projectile) || !SporeDreamForestSystem.Instance.IsSurfaceBarrier(target.Hitbox);
}

public sealed class SporeDreamBoundarySystem : ModSystem
{
    public override void PostUpdateNPCs()
    {
        if(!SporeDreamForestSystem.Instance.Exists) return;
        foreach(NPC npc in Main.ActiveNPCs) npc.GetGlobalNPC<SporeDreamEnemyGuard>().Enforce(npc);
    }
}
