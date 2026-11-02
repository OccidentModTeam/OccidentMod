using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using Terraria.GameContent.Generation;
using Occident.Content.Tiles.SporeDream;

namespace Occident.Common.Worlds;

public sealed class SporeDreamForestSystem : ModSystem
{
    public const int UndergroundDepth = 35;
    public int Left { get; private set; } = -1;
    public int[] Surface { get; private set; } = Array.Empty<int>();
    private const int CurrentVegetationVersion=2;
    private int vegetationVersion;
    public int Right => Left + Surface.Length;
    public bool Exists => Left >= 0 && Surface.Length > 0;
    public static SporeDreamForestSystem Instance => ModContent.GetInstance<SporeDreamForestSystem>();

    public override void ClearWorld() { Left = -1; Surface = Array.Empty<int>(); vegetationVersion=0; }
    public override void PostWorldLoad()
    {
        if(!Exists) return;
        // Refresh saved frames after merge-rule updates; no tiles or terrain are replaced.
        for(int x=Math.Max(1,Left-1);x<=Math.Min(Main.maxTilesX-2,Right);x++) {
            int ground=Surface[Math.Clamp(x-Left,0,Surface.Length-1)];
            for(int y=Math.Max(1,ground-1);y<=Math.Min(Main.maxTilesY-2,ground+UndergroundDepth+1);y++) {
                Tile t=Main.tile[x,y];
                if(t.HasTile && (t.TileType==TileID.Dirt || t.TileType==TileID.Grass || t.TileType==ModContent.TileType<SporeDreamGrass>()))
                    WorldGen.SquareTileFrame(x,y,false);
            }
        }
        if(vegetationVersion<CurrentVegetationVersion && Main.netMode!=NetmodeID.MultiplayerClient) {
            int added=PopulateGiants();
            vegetationVersion=CurrentVegetationVersion;
            Mod.Logger.Info($"Spore Dream Forest vegetation updated once: added {added} giants in clear spaces.");
        }
    }
    public bool Contains(int x, int y) => Exists && x >= Left && x < Right && y >= 0 && y <= Surface[x-Left] + UndergroundDepth;
    public bool SurfaceContains(int x, int y) => Exists && x >= Left && x < Right && y >= 0 && y <= Surface[x-Left];
    public bool IsSurfaceBarrier(Rectangle hitbox)
    {
        if (!Exists || hitbox.Right <= Left*16 || hitbox.Left >= Right*16 || hitbox.Bottom < 0) return false;
        int start = Math.Max(Left, hitbox.Left / 16);
        int end = Math.Min(Right-1, (hitbox.Right-1)/16);
        for (int x=start; x<=end; x++) if (hitbox.Top < (Surface[x-Left]+1)*16) return true;
        return false;
    }
    public Vector2 Outside(Rectangle hitbox)
    {
        int left = Left*16-hitbox.Width-2, right = Right*16+2;
        return new Vector2(Math.Abs(hitbox.X-left) <= Math.Abs(hitbox.X-right) ? left : right, hitbox.Y);
    }
    public override void SaveWorldData(TagCompound tag)
    {
        if (!Exists) return;
        tag["SporeDreamLeft"] = Left;
        tag["SporeDreamSurface"] = Surface;
        tag["SporeDreamVegetationVersion"]=vegetationVersion;
    }
    public override void LoadWorldData(TagCompound tag)
    {
        ClearWorld();
        if (!tag.ContainsKey("SporeDreamLeft")) return;
        int left = tag.GetInt("SporeDreamLeft");
        int[] surface = tag.GetIntArray("SporeDreamSurface");
        if (left < 1 || surface.Length == 0 || left+surface.Length >= Main.maxTilesX) return;
        foreach (int y in surface) if (y < 0 || y+UndergroundDepth >= Main.maxTilesY) return;
        Left = left; Surface = surface; vegetationVersion=tag.GetInt("SporeDreamVegetationVersion");
    }
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Left); writer.Write(Surface.Length);
        foreach (int y in Surface) writer.Write(y);
    }
    public override void NetReceive(BinaryReader reader)
    {
        int left = reader.ReadInt32(), count = reader.ReadInt32();
        if (count < 0 || count > Main.maxTilesX) throw new IOException("Invalid Spore Dream Forest width");
        int[] surface = new int[count];
        for (int i=0; i<count; i++) surface[i] = reader.ReadInt32();
        if(count>0 && (left<1 || left+count>=Main.maxTilesX)) throw new IOException("Invalid Spore Dream Forest bounds");
        foreach(int y in surface) if(y<0 || y+UndergroundDepth>=Main.maxTilesY) throw new IOException("Invalid Spore Dream Forest depth");
        Left = left; Surface = surface;
    }
    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        => tasks.Add(new PassLegacy("Spore Dream Forest", Generate));

    private static bool Evil(ushort type) => type == TileID.CorruptGrass || type == TileID.CrimsonGrass ||
        type == TileID.Ebonstone || type == TileID.Crimstone || type == TileID.Ebonsand || type == TileID.Crimsand;
    private static int Ground(int x)
    {
        for (int y=60; y<Math.Min(Main.maxTilesY-100, (int)Main.worldSurface+100); y++) {
            Tile t = Main.tile[x,y];
            if (t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType]) return y;
        }
        return -1;
    }
    private void Generate(GenerationProgress progress, GameConfiguration config)
    {
        progress.Message = "Spore Dream Forest";
        int spawn = Main.spawnTileX > 0 ? Main.spawnTileX : Main.maxTilesX/2;
        int preferredWidth = Math.Clamp(Main.maxTilesX/25, 160, 240);
        int bestLeft=-1, bestScore=int.MaxValue;
        var sites=new List<(int Left,int Score,int[] Surface)>();
        int[] bestSurface = Array.Empty<int>();
        foreach(int width in new[]{preferredWidth,128,96}) foreach (int direction in new[] { -1, 1 }) {
            int evilDistance=Main.maxTilesX;
            for (int d=80; d<Math.Min(1400, Main.maxTilesX/3); d+=4) {
                int x=spawn+direction*d;
                if (x<180 || x>=Main.maxTilesX-180) break;
                int y=Ground(x);
                if (y>0 && (Evil(Main.tile[x,y].TileType) || Evil(Main.tile[x,y+3].TileType))) { evilDistance=d; break; }
            }
            for (int distance=220; distance<Math.Min(1100, evilDistance-width/2-48); distance+=12) {
                int left=spawn+direction*distance-width/2;
                if (left<180 || left+width>=Main.maxTilesX-180) continue;
                int[] surface=new int[width]; int forest=0, min=int.MaxValue, max=0; bool valid=true;
                for (int k=0; k<width; k++) {
                    int y=Ground(left+k); surface[k]=y;
                    if (y<80 || y>(int)Main.worldSurface+40) { valid=false; break; }
                    ushort type=Main.tile[left+k,y].TileType;
                    if (type!=TileID.Grass && type!=TileID.Dirt && type!=TileID.ClayBlock && type!=TileID.Stone) { valid=false; break; }
                    if (Main.tile[left+k,y-1].LiquidAmount>0 || Main.wallHouse[Main.tile[left+k,y-1].WallType]) { valid=false; break; }
                    for (int q=Math.Max(0,y-12); q<=y+UndergroundDepth; q++) {
                        Tile t=Main.tile[left+k,q];
                        if (t.HasTile && (Evil(t.TileType) || t.TileType==TileID.Containers || t.TileType==TileID.Containers2)) { valid=false; break; }
                    }
                    if (!valid) break;
                    if (type==TileID.Grass) forest++;
                    min=Math.Min(min,y); max=Math.Max(max,y);
                }
                if (!valid || forest<width/4 || max-min>80) continue;
                int score=distance+(max-min)*2+(preferredWidth-width)*3;
                sites.Add((left,score,surface));
                bestScore=Math.Min(bestScore,score);
            }
        }
        // Seeded randomness preserves reproducibility without always choosing the nearest site.
        var choices=sites.FindAll(site=>site.Score<=bestScore+240);
        if(choices.Count>0) {
            var selected=choices[WorldGen.genRand.Next(choices.Count)];
            bestLeft=selected.Left; bestSurface=selected.Surface;
        }
        if(bestLeft<0) { Mod.Logger.Warn("No safe forest site before evil was found; Spore Dream Forest was not forced over other biomes."); return; }
        Left=bestLeft; Surface=bestSurface;
        int grass=ModContent.TileType<SporeDreamGrass>();
        for(int k=0;k<Surface.Length;k++) {
            int x=Left+k, ground=Surface[k];
            for(int y=Math.Max(1,ground-45);y<ground;y++) {
                Tile t=Main.tile[x,y];
                if(t.HasTile && (t.TileType==TileID.Trees || t.TileType==TileID.Plants || t.TileType==TileID.Plants2 || t.TileType==TileID.Sunflower)) t.ClearTile();
            }
            for(int y=ground;y<=ground+UndergroundDepth;y++) {
                Tile t=Main.tile[x,y];
                if(t.HasTile && (t.TileType==TileID.Grass || (t.TileType==TileID.Dirt && (y==ground || !Main.tile[x,y-1].HasTile)))) {
                    t.TileType=(ushort)grass; t.TileFrameX=t.TileFrameY=0;
                }
            }
        }
        for(int k=0;k<Surface.Length;k++) for(int y=Surface[k]-1;y<=Surface[k]+UndergroundDepth;y++) WorldGen.SquareTileFrame(Left+k,y);
        PopulateGiants();
        vegetationVersion=CurrentVegetationVersion;
        // Place ground cover afterward so small plants cannot accidentally veto a giant's clearance test.
        for(int x=Left+4;x<Right-4;x++) {
            int y=Surface[x-Left];
            if(WorldGen.genRand.NextBool(110) && CanPlaceFruit(x,y)) WorldGen.PlaceObject(x,y-1,ModContent.TileType<PandoraPlant>(),true,WorldGen.genRand.Next(3));
            else if(WorldGen.genRand.NextBool(3)) WorldGen.PlaceObject(x,y-1,ModContent.TileType<SporeDreamPlants>(),true,WorldGen.genRand.Next(5));
        }
        Mod.Logger.Info($"Spore Dream Forest generated: x={Left}..{Right-1}, width={Surface.Length}, underground depth={UndergroundDepth}");
    }
    public bool CanPlaceFruit(int x,int ground)
    {
        if(!Contains(x,ground) || !WorldGen.InWorld(x+1,ground,2)) return false;
        int grass=ModContent.TileType<SporeDreamGrass>(),fruit=ModContent.TileType<PandoraPlant>();
        for(int dx=0;dx<2;dx++) {
            Tile support=Main.tile[x+dx,ground];
            if(!support.HasTile || support.TileType!=grass || support.IsHalfBlock || support.Slope!=0) return false;
            for(int dy=1;dy<=2;dy++)
                if(Main.tile[x+dx,ground-dy].HasTile || Main.tile[x+dx,ground-dy].LiquidAmount>0) return false;
        }
        // Spacing also limits long-term regrowth, rather than merely slowing overcrowding.
        for(int i=Math.Max(1,x-28);i<=Math.Min(Main.maxTilesX-2,x+28);i++)
            for(int y=Math.Max(1,ground-16);y<=Math.Min(Main.maxTilesY-2,ground+16);y++)
                if(Main.tile[i,y].HasTile && Main.tile[i,y].TileType==fruit) return false;
        return true;
    }
    public int PopulateGiants()
    {
        if(!Exists || Main.netMode==NetmodeID.MultiplayerClient) return 0;
        int count=0;
        // Randomized candidate order avoids filling consistently from the left edge.
        var candidates=new List<int>();
        for(int x=Left+4;x<Right-4;x++) candidates.Add(x);
        for(int k=candidates.Count-1;k>0;k--) {
            int other=WorldGen.genRand.Next(k+1);
            (candidates[k],candidates[other])=(candidates[other],candidates[k]);
        }
        foreach(int x in candidates) {
            if(!WorldGen.genRand.NextBool(3)) continue;
            int y=Surface[x-Left];
            if(!SporeDreamGiant.TryGrow(x,y)) continue;
            count++;
            if(Main.netMode==NetmodeID.Server) NetMessage.SendTileSquare(-1,x-3,y-20,7,21);
        }
        return count;
    }
}
