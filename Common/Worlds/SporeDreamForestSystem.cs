// 管理孢夢之森範圍、地表高度、新世界生成及既有群系資料更新。
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Enums;
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
    // 索引為世界 X 減去 Left；每個值是該欄地表的 Y，兩者單位皆為物塊。
    public int[] Surface { get; private set; } = Array.Empty<int>();
    private const int CurrentVegetationVersion=2;
    private int vegetationVersion;
    private const int CurrentSoilVersion=2;
    private int soilVersion;
    private bool giantFoundationsUpdated;
    public int Right => Left + Surface.Length;
    public bool Exists => Left >= 0 && Surface.Length > 0;
    public static SporeDreamForestSystem Instance => ModContent.GetInstance<SporeDreamForestSystem>();

    public override void ClearWorld() { Left = -1; Surface = Array.Empty<int>(); vegetationVersion=0; soilVersion=0; giantFoundationsUpdated=false; }
    // 只更新已存在的群系；版本欄位讓土壤與植被修正不會每次載入重做。
    public override void PostWorldLoad()
    {
        if(!Exists) return;
        if(Main.netMode!=NetmodeID.MultiplayerClient) {ClearNativeTrees();ClearSurfaceRubble();}
        if(soilVersion<CurrentSoilVersion && Main.netMode!=NetmodeID.MultiplayerClient) {
            int converted=0;
            for(int x=Left;x<Right;x++) {
                int ground=Surface[x-Left];
                for(int y=Math.Max(1,ground);y<=Math.Min(Main.maxTilesY-2,ground+UndergroundDepth);y++) {
                    Tile t=Main.tile[x,y];
                    // 保留建築、其他物塊與既有地形形狀，只轉換符合條件的土壤。
                    if(!t.HasTile || t.TileType!=TileID.Dirt || Main.wallHouse[t.WallType]) continue;
                    t.TileType=TileID.Mud; t.TileFrameX=t.TileFrameY=0;
                    converted++;
                }
            }
            int repaired=RepairBuriedSoilShapes();
            soilVersion=CurrentSoilVersion;
            Mod.Logger.Info($"Spore Dream Forest soil updated once: converted {converted} dirt tiles to mud, repaired {repaired} buried slopes/half-blocks.");
        }
        // 土壤全部轉換完後才刷新鄰接框架，避免用到轉換中的地形狀態。
        for(int x=Math.Max(1,Left-1);x<=Math.Min(Main.maxTilesX-2,Right);x++) {
            int ground=Surface[Math.Clamp(x-Left,0,Surface.Length-1)];
            for(int y=Math.Max(1,ground-1);y<=Math.Min(Main.maxTilesY-2,ground+UndergroundDepth+1);y++) {
                Tile t=Main.tile[x,y];
                if(t.HasTile && (t.TileType==TileID.Dirt || t.TileType==TileID.Mud || t.TileType==TileID.Grass || t.TileType==ModContent.TileType<SporeDreamGrass>()))
                    WorldGen.SquareTileFrame(x,y,false);
            }
        }
        if(vegetationVersion<CurrentVegetationVersion && Main.netMode!=NetmodeID.MultiplayerClient) {
            int added=PopulateGiants();
            vegetationVersion=CurrentVegetationVersion;
            Mod.Logger.Info($"Spore Dream Forest vegetation updated once: added {added} giants in clear spaces.");
        }
        if(!giantFoundationsUpdated && Main.netMode!=NetmodeID.MultiplayerClient) {
            int repaired=0,type=ModContent.TileType<SporeDreamGiant>();
            for(int x=Left+3;x<Right-4;x++)
                for(int y=Math.Max(1,Surface[x-Left]-40);y<=Surface[x-Left];y++) {
                    Tile t=Main.tile[x,y];
                    if(!t.HasTile || t.TileType!=type || !SporeDreamGiant.IsPairedTile(x,y)
                        || !SporeDreamGiant.IsPrimaryTile(x,y)) continue;
                    SporeDreamGiant.GetStemBounds(x,y,out _,out int bottom,out _);
                    if(PrepareGiantFoundation(x,bottom,true,2)) repaired++;
                    break;
                }
            giantFoundationsUpdated=true;
            Mod.Logger.Info($"Spore Dream Forest paired giant foundations updated once: {repaired} natural root pads repaired.");
        }
    }
    // 此範圍包括地表下方；敵人屏障另用較窄的 IsSurfaceBarrier 判定。
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
    public Vector2 OppositeBoundary(Rectangle hitbox,Vector2 velocity)
    {
        bool exitRight=hitbox.Right<=Left*16 ? true : hitbox.Left>=Right*16 ? false
            : Math.Abs(velocity.X)>.01f ? velocity.X>0 : hitbox.Center.X<(Left+Right)*8;
        return new Vector2(exitRight ? Right*16+32 : Left*16-hitbox.Width-32,hitbox.Y);
    }
    public override void SaveWorldData(TagCompound tag)
    {
        if (!Exists) return;
        tag["SporeDreamLeft"] = Left;
        tag["SporeDreamSurface"] = Surface;
        tag["SporeDreamVegetationVersion"]=vegetationVersion;
        tag["SporeDreamSoilVersion"]=soilVersion;
        tag["SporeDreamGiantFoundationsUpdated"]=giantFoundationsUpdated;
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
        soilVersion=tag.GetInt("SporeDreamSoilVersion");
        giantFoundationsUpdated=tag.GetBool("SporeDreamGiantFoundationsUpdated");
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
    // 插入原版世界生成流程；一般載入舊世界不會經過這個生成步驟。
    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        => tasks.Add(new PassLegacy("Spore Dream Forest", Generate));

    private static bool IsVanillaTree(ushort type)
        =>type is TileID.Trees or TileID.VanityTreeSakura or TileID.VanityTreeYellowWillow;
    private void ClearNativeTrees()
    {
        // 只移除根部位於群系內的完整樹木，保留群系外的樹。
        for(int x=Left;x<Right;x++) {
            int ground=Surface[x-Left];
            for(int y=Math.Max(1,ground-4);y<=ground+1;y++) {
                Tile root=Main.tile[x,y],soil=Main.tile[x,y+1];
                if(!root.HasTile || !IsVanillaTree(root.TileType) || !soil.HasTile || !Main.tileSolid[soil.TileType])continue;
                WorldGen.KillTile(x,y,noItem:true);
                if(Main.netMode==NetmodeID.Server)NetMessage.SendTileSquare(-1,x-3,Math.Max(1,y-40),7,44);
                break;
            }
        }
    }
    private void ClearSurfaceRubble()
    {
        // 原版裝飾堆在本生成步驟之前放置；地面降低後不會自行跟隨，需避免懸空。
        int removed=0;
        for(int x=Left;x<Right;x++) {
            int ground=Surface[x-Left];
            for(int y=Math.Max(1,ground-64);y<=Math.Min(Main.maxTilesY-2,ground+3);y++) {
                Tile tile=Main.tile[x,y];
                if(!tile.HasTile || Main.wallHouse[tile.WallType]
                    || tile.TileType is not (TileID.SmallPiles or TileID.LargePiles or TileID.LargePiles2))continue;
                WorldGen.KillTile(x,y,noItem:true);removed++;
            }
        }
        if(removed>0)Mod.Logger.Info($"Spore Dream Forest native surface rubble cleared: {removed} pile cells/objects.");
    }
    private static bool Evil(ushort type) => type == TileID.CorruptGrass || type == TileID.CrimsonGrass ||
        type == TileID.Ebonstone || type == TileID.Crimstone || type == TileID.Ebonsand || type == TileID.Crimsand;
    private static int Ground(int x,int terrainTop)
    {
        for (int y=terrainTop; y<Math.Min(Main.maxTilesY-100, (int)Main.worldSurface+100); y++) {
            Tile t = Main.tile[x,y];
            if (!t.HasTile || !Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType])continue;
            // 雲塊可能低於一般地表高度，不能因此否決浮島下方真正的森林地面。
            if(t.TileType is TileID.Cloud or TileID.RainCloud or TileID.SnowCloud)continue;
            if(t.TileType is TileID.Grass or TileID.Dirt or TileID.Stone) {
                Tile lower=Main.tile[x,y+32];
                if(!lower.HasTile || !Main.tileSolid[lower.TileType]) {
                    // 淺薄浮空土塊下方若仍是天空，就不視為陸地。
                    bool sky=true;
                    for(int q=y+24;q<=y+40;q++)if(Main.tile[x,q].HasTile && Main.tileSolid[Main.tile[x,q].TileType]){sky=false;break;}
                    if(sky)continue;
                }
            }
            return y;
        }
        return -1;
    }
    // 先尋找安全位置，再處理地形、巨菇與地被；失敗時保留原版世界。
    private void Generate(GenerationProgress progress, GameConfiguration config)
    {
        progress.Message = "Spore Dream Forest";
        int spawn = Main.spawnTileX > 0 ? Main.spawnTileX : Main.maxTilesX/2;
        // 從原版陸地表面高度帶開始掃描，避免把高空浮島與雲層誤認為森林地面。
        int terrainTop=(int)GenVars.worldSurfaceLow;
        if(terrainTop<80 || terrainTop>Main.worldSurface)
            terrainTop=Math.Max(80,Main.spawnTileY-SporeDreamGenerationScale.Distance(80,Main.maxTilesX));
        terrainTop=Math.Max(60,terrainTop-24);
        var columns=new SporeDreamSiteSelector.Column[Main.maxTilesX];
        var read=new bool[Main.maxTilesX];
        SporeDreamSiteSelector.Column Column(int x) {
            if(read[x])return columns[x];
            read[x]=true;
            int y=Ground(x,terrainTop);
            if(y<80 || y>(int)Main.worldSurface+40 || y+UndergroundDepth>=Main.maxTilesY-1)
                return columns[x]=new(y,false,false,false);
            ushort type=Main.tile[x,y].TileType;
            bool evil=Evil(type)||Evil(Main.tile[x,y+3].TileType);
            bool safe=type is TileID.Grass or TileID.Dirt or TileID.ClayBlock or TileID.Stone;
            int waterDepth=0;
            safe&=!Main.wallHouse[Main.tile[x,y-1].WallType];
            // 少量地表積水不否決整片森林；深水與非水液體仍受保護，不在此排空。
            if(safe) for(int waterY=y-1;waterY>=Math.Max(1,y-5);waterY--) {
                Tile water=Main.tile[x,waterY];
                if(water.LiquidAmount==0)break;
                if(water.LiquidType!=LiquidID.Water || ++waterDepth>4){safe=false;break;}
            }
            if(safe) for(int q=Math.Max(0,y-12);q<=y+UndergroundDepth;q++) {
                Tile tile=Main.tile[x,q];
                if(Main.wallHouse[tile.WallType] || (tile.HasTile &&
                    (Evil(tile.TileType)||tile.TileType==TileID.Containers||tile.TileType==TileID.Containers2))) {safe=false;break;}
            }
            return columns[x]=new(y,safe,type==TileID.Grass,evil,waterDepth);
        }
        var search=SporeDreamSiteSelector.Find(Main.maxTilesX,spawn,Column);
        if(search.Sites.Count==0) {
            var spans=new List<string>();var blockers=new Dictionary<ushort,int>();
            int start=-1;
            for(int x=Math.Max(180,spawn-SporeDreamGenerationScale.Distance(1100,Main.maxTilesX));x<Math.Min(Main.maxTilesX-180,spawn+SporeDreamGenerationScale.Distance(1100,Main.maxTilesX));x++) {
                var c=Column(x);
                if(c.Safe){if(start<0)start=x;}
                else {
                    if(start>=0&&x-start>=12)spans.Add($"{start}..{x-1}({x-start})");start=-1;
                    if(c.Ground>0){ushort t=Main.tile[x,c.Ground].TileType;blockers.TryGetValue(t,out int n);blockers[t]=n+1;}
                }
            }
            Mod.Logger.Warn($"Spore Dream Forest safe spans: {string.Join(';',spans)}; blocked surface types: {string.Join(';',blockers)}");
            throw new InvalidOperationException($"Spore Dream Forest is required, but no safe forest site remains after standard, detailed and narrow searches. World generation stopped instead of saving a world without this biome. spawn={spawn}, terrainTop={terrainTop}, world={Main.maxTilesX}.");
        }
        // 先保留正常寬度候選，避免較近的窄候選擠掉完整森林。
        int widest=0;
        foreach(var site in search.Sites)widest=Math.Max(widest,site.Surface.Length);
        int bestScore=int.MaxValue;
        foreach(var site in search.Sites)if(site.Surface.Length==widest)bestScore=Math.Min(bestScore,site.Score);
        int scoreSlack=SporeDreamGenerationScale.Distance(240,Main.maxTilesX);
        var choices=search.Sites.FindAll(site=>site.Surface.Length==widest && site.Score<=bestScore+scoreSlack);
        var selected=choices[WorldGen.genRand.Next(choices.Count)];
        int bestLeft=selected.Left;int[] bestSurface=selected.Surface;
        Mod.Logger.Info($"Spore Dream Forest site selected: search={search.Mode}, candidates={search.Sites.Count}, terrainTop={terrainTop}, width={bestSurface.Length}.");
        Left=bestLeft; Surface=bestSurface;
        int drainedWater=0;
        for(int x=Left;x<Right;x++) {
            int ground=Surface[x-Left],depth=Column(x).WaterDepth;
            for(int y=ground-depth;y<ground;y++) {
                Main.tile[x,y].LiquidAmount=0;drainedWater++;
            }
        }
        if(drainedWater>0)Mod.Logger.Info($"Spore Dream Forest prepared {drainedWater} shallow water cells; deep water and other liquids were preserved.");
        ClearNativeTrees();
        VaryTerrain();
        ClearSurfaceRubble();
        int grass=ModContent.TileType<SporeDreamGrass>();
        for(int k=0;k<Surface.Length;k++) {
            int x=Left+k, ground=Surface[k];
            for(int y=Math.Max(1,ground-45);y<ground;y++) {
                Tile t=Main.tile[x,y];
                if(t.HasTile && (t.TileType==TileID.Plants || t.TileType==TileID.Plants2 || t.TileType==TileID.Sunflower)) t.ClearTile();
            }
            for(int y=ground;y<=ground+UndergroundDepth;y++) {
                Tile t=Main.tile[x,y];
                if(t.HasTile && (t.TileType==TileID.Grass || (t.TileType==TileID.Dirt && (y==ground || !Main.tile[x,y-1].HasTile)))) {
                    t.TileType=(ushort)grass; t.TileFrameX=t.TileFrameY=0;
                }
                else if(t.HasTile && t.TileType==TileID.Dirt) {
                    t.TileType=TileID.Mud; t.TileFrameX=t.TileFrameY=0;
                }
            }
        }
        RepairBuriedSoilShapes();
        for(int k=0;k<Surface.Length;k++) for(int y=Surface[k]-1;y<=Surface[k]+UndergroundDepth;y++) WorldGen.SquareTileFrame(Left+k,y);
        PopulateGiants();
        vegetationVersion=CurrentVegetationVersion;
        soilVersion=CurrentSoilVersion;
        giantFoundationsUpdated=true;
        // 巨菇配置完成後再放地被，避免小植株誤擋巨菇的空間檢查。
        for(int x=Left+4;x<Right-4;x++) {
            int y=Surface[x-Left];
            if(WorldGen.genRand.NextBool(110) && CanPlaceFruit(x,y)) WorldGen.PlaceObject(x,y-1,ModContent.TileType<PandoraPlant>(),true,WorldGen.genRand.Next(3));
            else if(WorldGen.genRand.NextBool(3)) WorldGen.PlaceObject(x,y-1,ModContent.TileType<SporeDreamPlants>(),true,WorldGen.genRand.Next(SporeDreamPlants.StyleCount));
        }
        Mod.Logger.Info($"Spore Dream Forest generated: x={Left}..{Right-1}, width={Surface.Length}, underground depth={UndergroundDepth}");
    }
    private static bool NaturalSoil(ushort type) =>
        type is TileID.Grass or TileID.Dirt or TileID.Stone or TileID.ClayBlock;

    private int RepairBuriedSoilShapes()
    {
        int repaired=0,grass=ModContent.TileType<SporeDreamGrass>();
        for(int x=Left;x<Right;x++) for(int y=Math.Max(1,Surface[x-Left]);y<=Math.Min(Main.maxTilesY-2,Surface[x-Left]+UndergroundDepth);y++) {
            Tile tile=Main.tile[x,y];
            if(!tile.HasTile || (tile.TileType!=grass && tile.TileType!=TileID.Mud && tile.TileType!=TileID.Dirt)
                || (!tile.IsHalfBlock && tile.Slope==SlopeType.Solid) || Main.wallHouse[tile.WallType]) continue;
            // 只修復埋在土中的形狀殘留，保留外露斜坡、洞穴、平台與建築房間。
            if(!WorldGen.SolidTile(x-1,y) || !WorldGen.SolidTile(x+1,y)
                || !WorldGen.SolidTile(x,y-1) || !WorldGen.SolidTile(x,y+1)) continue;
            tile.IsHalfBlock=false;
            tile.Slope=SlopeType.Solid;
            repaired++;
        }
        return repaired;
    }

    private void VaryTerrain()
    {
        int[] offsets=SporeDreamTerrainProfile.CreateOffsets(Surface.Length,WorldGen.genRand.Next);
        for(int k=0;k<Surface.Length;k++) {
            int x=Left+k,oldGround=Surface[k],newGround=oldGround+offsets[k];
            bool safe=true;
            for(int y=Math.Min(oldGround,newGround);y<=Math.Max(oldGround,newGround)+1;y++) {
                Tile t=Main.tile[x,y];
                if(t.LiquidAmount>0 || Main.wallHouse[t.WallType]) { safe=false; break; }
                if(y>=oldGround && (!t.HasTile || !NaturalSoil(t.TileType))) { safe=false; break; }
                if(y<oldGround && t.HasTile && t.TileType is not (TileID.Trees or TileID.Plants or TileID.Plants2 or TileID.Sunflower)) { safe=false; break; }
            }
            if(!safe) offsets[k]=0;
        }
        // 靠近受保護的地形欄位時平滑收斂，避免在洞穴旁造出斷崖。
        for(int k=1;k<offsets.Length;k++)
            offsets[k]=Math.Clamp(offsets[k],Math.Min(0,offsets[k-1]-1),Math.Max(0,offsets[k-1]+1));
        for(int k=offsets.Length-2;k>=0;k--)
            offsets[k]=Math.Clamp(offsets[k],Math.Min(0,offsets[k+1]-1),Math.Max(0,offsets[k+1]+1));
        for(int k=0;k<Surface.Length;k++) {
            int x=Left+k,oldGround=Surface[k],newGround=oldGround+offsets[k];
            for(int y=newGround;y<oldGround;y++) {
                Tile t=Main.tile[x,y];t.ClearTile();t.HasTile=true;t.TileType=TileID.Dirt;
                t.IsHalfBlock=false;t.Slope=SlopeType.Solid;
            }
            for(int y=oldGround;y<newGround;y++) Main.tile[x,y].ClearTile();
            Surface[k]=newGround;
        }
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
        // 間距限制同樣用於日後生長，避免只是延後出現過密植株。
        for(int i=Math.Max(1,x-28);i<=Math.Min(Main.maxTilesX-2,x+28);i++)
            for(int y=Math.Max(1,ground-16);y<=Math.Min(Main.maxTilesY-2,ground+16);y++)
                if(Main.tile[i,y].HasTile && Main.tile[i,y].TileType==fruit) return false;
        return true;
    }
    // 新 16 像素菇柄使用單欄支撐與兩側自然土肩；width=2 僅相容舊雙欄存檔。規劃階段只讀取，失敗時不留下修改過的基座。
    // commit=false 只驗證並規劃，通過後才以 commit=true 實際修改土壤。
    public bool PrepareGiantFoundation(int x,int ground,bool apply,int width=1)
    {
        if(!Exists || width is <1 or >2 || x-3<Left || x+width+2>=Right) return false;
        int grass=ModContent.TileType<SporeDreamGrass>(),plants=ModContent.TileType<SporeDreamPlants>();
        var tops=new int[width+6];
        if(!SporeDreamGiantLayout.TryFoundationProfile(Surface.AsSpan(x-3-Left,width+6),ground,tops,width)) return false;
        for(int dx=-3;dx<=width+2;dx++) {
            int column=x+dx,old=Surface[column-Left];
            int target=tops[dx+3];
            // 整個基座下方必須有實際土壤，不跨接洞穴，也不覆蓋家具、果實、菇柄或房屋牆。
            for(int y=target;y<=Math.Max(old+1,target+1);y++) {
                Tile t=Main.tile[column,y];
                if(t.LiquidAmount>0 || Main.wallHouse[t.WallType]) return false;
                if(y>=old && (!t.HasTile || (t.TileType!=grass && t.TileType!=TileID.Mud && t.TileType!=TileID.Dirt))) return false;
                if(y<old && t.HasTile && t.TileType!=plants) return false;
            }
        }
        if(!apply) return true;
        for(int dx=-3;dx<=width+2;dx++) {
            int column=x+dx,old=Surface[column-Left],target=tops[dx+3];
            for(int y=target;y<=Math.Max(old+1,target+1);y++) {
                Tile t=Main.tile[column,y];t.ClearTile();t.HasTile=true;
                t.TileType=(ushort)(y==target?grass:TileID.Mud);
                t.IsHalfBlock=false;t.Slope=SlopeType.Solid;
            }
            Surface[column-Left]=target;
        }
        for(int dx=-4;dx<=width+3;dx++)
            for(int y=ground-4;y<=ground+6;y++) WorldGen.SquareTileFrame(x+dx,y,false);
        return true;
    }
    public int PopulateGiants()
    {
        if(!Exists || Main.netMode==NetmodeID.MultiplayerClient) return 0;
        int curvedBudget=Math.Max(1,(int)(Surface.Length/32f*.25f));
        bool tallPair=WorldGen.genRand.NextBool(2);
        int tallCount=tallPair?SporeDreamCurvedSystem.Instance.TryTallPair():0;
        if(tallPair && tallCount==0)Mod.Logger.Info("Spore Dream Forest tall pair skipped: no two safely separated clear sites.");
        int curvedCount=SporeDreamCurvedSystem.Instance.PopulateSides(curvedBudget);
        int count=curvedCount+tallCount;
        // 打亂候選順序，避免每次都從最左側開始種滿。
        var candidates=new List<int>();
        for(int x=Left+5;x<Right-5;x++) candidates.Add(x);
        for(int k=candidates.Count-1;k>0;k--) {
            int other=WorldGen.genRand.Next(k+1);
            (candidates[k],candidates[other])=(candidates[other],candidates[k]);
        }
        foreach(int x in candidates) {
            if(WorldGen.genRand.Next(100)>=SporeDreamGenerationScale.GiantCandidatePercent) continue;
            int y=Surface[x-Left];
            // 新地面巨菇皆使用短彎根；位置不合格就略過，不退回直柱外形。
            if(!SporeDreamCurvedSystem.Instance.TryFloor(x,y)) continue;
            curvedCount++;
            count++;
            if(Main.netMode==NetmodeID.Server) NetMessage.SendTileSquare(-1,x-5,y-22,12,30);
        }
        Mod.Logger.Info($"Spore Dream Forest giants: {count} total, {curvedCount} curved; tall={tallCount}, tall-pair-requested={tallPair}, width={Surface.Length}, candidate chance={SporeDreamGenerationScale.GiantCandidatePercent}%.");
        return count;
    }
}
