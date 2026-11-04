// 管理彎曲巨菇的分段幾何、占格、生成、砍斷、存檔與多人同步。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Occident.Content.Tiles.SporeDream;
using G=Occident.Common.Worlds.SporeDreamGrowthPath;

namespace Occident.Common.Worlds;

public sealed class SporeDreamCurvedSystem : ModSystem
{
    private const string Root="OccidentMod/Assets/Sprites/Environment/SporeDreamForest/";
    internal const byte SyncMessage=2,SpringRequest=3,SpringAnimation=4;
    internal int MapRevision {get;private set;}
    private readonly ulong[] springRequestAfter=new ulong[256];
    private const int MaximumPlants=32,MaximumPieces=64;
    private sealed class Plant
    {
        internal Point RootA,RootB;internal List<G.Piece> Pieces=new();
        internal bool Pressing;internal ulong PressStarted;
        internal Plant(Point a,Point b,List<G.Piece> pieces){RootA=a;RootB=b;Pieces=pieces;}
    }
    // plants 保存形狀本體；cells 供挖掘查段落，draws 決定每段從哪個物塊開始繪製。
    private readonly List<Plant> plants=new();
    private readonly Dictionary<Point,(Plant Plant,int Stage)> cells=new();
    private readonly Dictionary<Point,List<(Plant Plant,int Stage)>> draws=new();
    private bool cutting,dirty;
    public static SporeDreamCurvedSystem Instance=>ModContent.GetInstance<SporeDreamCurvedSystem>();
    public override void ClearWorld(){plants.Clear();cells.Clear();draws.Clear();cutting=false;dirty=false;MapRevision++;Array.Clear(springRequestAfter);}
    public override void PostWorldLoad()
    {
        if(Main.netMode!=NetmodeID.MultiplayerClient)RepairGroundEntries();
        if(plants.Count>0)Mod.Logger.Info($"Spore Dream Forest curved paths loaded: {plants.Count} plants, {cells.Count} occupied tiles.");
    }
    // 以原圖頸部為錨點精確計算四分之一圈旋轉邊界，不重採樣素材。
    private void RepairGroundEntries()
    {
        var forest=SporeDreamForestSystem.Instance;int repaired=0;
        if(!forest.Exists)return;
        foreach(Plant plant in plants) {
            if(plant.Pieces.Count<2 || plant.RootA.X<forest.Left || plant.RootA.X>=forest.Right
                || plant.RootA.Y!=forest.Surface[plant.RootA.X-forest.Left])continue;
            G.Piece first=plant.Pieces[0];
            if(first.Kind!=G.Horizontal || first.Y!=plant.RootA.Y*16-16)continue;
            // 原位替換開口的橫向根部，維持後續彎角接點；下緣 4 像素埋入土中。
            plant.Pieces[0]=first with {Kind=G.Turn,Extra=G.Up};repaired++;
        }
        if(repaired>0) {
            Rebuild();dirty=true;
            Mod.Logger.Info($"Spore Dream Forest ground entries repaired: {repaired}; crowns and existing terrain preserved.");
        }
    }
    private bool BetweenUpwardCrowns(int x)
    {
        G.Piece? left=null,right=null;
        foreach(Plant plant in plants) {
            G.Piece crown=plant.Pieces[^1];if(crown.Kind!=G.Crown)continue;
            if(crown.X<x && (left is null || crown.X>left.Value.X))left=crown;
            if(crown.X>x && (right is null || crown.X<right.Value.X))right=crown;
        }
        return left?.Direction==G.Up && right?.Direction==G.Up;
    }
    private static Rectangle CrownBounds(G.Piece p)=>p.Direction switch {
        G.Right=>new(p.X,p.Y-62,72,126),
        G.Left=>new(p.X-72,p.Y-64,72,126),
        _=>new(p.X-62,p.Y-72,126,72)
    };
    private static Rectangle Bounds(G.Piece p)=>p.Kind switch {
        G.Vertical=>new(p.X,p.Y,16,20),G.Horizontal=>new(p.X,p.Y,20,16),G.Turn=>new(p.X,p.Y,20,20),
        G.End=>p.Direction switch {
            G.Up=>new(p.X-8,p.Y,16,20),G.Down=>new(p.X-8,p.Y-20,16,20),
            G.Right=>new(p.X-20,p.Y-8,20,16),_=>new(p.X,p.Y-8,20,16)
        },
        // 菌蓋繪圖較寬，但可挖掘範圍只使用中央接點。
        _=>p.Direction switch {G.Right=>new(p.X,p.Y-8,16,16),G.Left=>new(p.X-16,p.Y-8,16,16),_=>new(p.X-8,p.Y-16,16,16)}
    };
    private static IEnumerable<Point> Footprint(Plant plant,G.Piece p)
    {
        Rectangle r=Bounds(p);
        if(p.Kind==G.Turn && p.Extra==G.Up && p==plant.Pieces[0])
            r.Height=Math.Min(r.Height,plant.RootA.Y*16-r.Y);
        for(int x=(int)Math.Floor(r.Left/16f);x<=(r.Right-1)/16;x++)
            for(int y=(int)Math.Floor(r.Top/16f);y<=(r.Bottom-1)/16;y++) {
                var cell=new Point(x,y);
                if(cell!=plant.RootA && cell!=plant.RootB) yield return cell;
            }
    }
    // 由植株幾何重建占格與繪圖索引；形狀改變後也遞增地圖版本。
    private void Rebuild()
    {
        cells.Clear();draws.Clear();MapRevision++;
        foreach(Plant plant in plants)for(int stage=0;stage<plant.Pieces.Count;stage++) {
            Point? anchor=null;
            foreach(Point p in Footprint(plant,plant.Pieces[stage])) {
                if(!WorldGen.InWorld(p.X,p.Y,1))continue;
                anchor??=p;
                cells.TryAdd(p,(plant,stage));
            }
            if(anchor is Point key) {
                if(!draws.TryGetValue(key,out var list))draws[key]=list=new();
                list.Add((plant,stage));
            }
        }
    }
    private static bool Soil(Tile t)
        =>t.HasTile && (t.TileType==ModContent.TileType<SporeDreamGrass>() || t.TileType==TileID.Mud || t.TileType==TileID.Dirt);
    private static bool RootSafe(Point p)
        =>WorldGen.InWorld(p.X,p.Y,10) && Soil(Main.tile[p.X,p.Y]) && !Main.wallHouse[Main.tile[p.X,p.Y].WallType]
            && Main.tile[p.X,p.Y].LiquidAmount==0;
    private bool SpaceAvailable(Plant plant)
    {
        var forest=SporeDreamForestSystem.Instance;
        int giant=ModContent.TileType<SporeDreamGiant>(),curved=ModContent.TileType<SporeDreamCurvedGiant>(),cover=ModContent.TileType<SporeDreamPlants>();
        var regions=new List<Rectangle>();
        for(int stage=0;stage<plant.Pieces.Count;stage++) {
            G.Piece piece=plant.Pieces[stage];Rectangle rect=Bounds(piece);
            if(piece.Kind==G.Crown)rect=CrownBounds(piece);
            if(rect.Left<(forest.Left+2)*16 || rect.Right>(forest.Right-2)*16 || rect.Top<32)return false;
            // 同一生長路徑中，不相鄰的段落不能回頭交叉。
            if(piece.Kind!=G.Crown)for(int earlier=0;earlier<regions.Count-1;earlier++)
                if(rect.Intersects(regions[earlier]))return false;
            regions.Add(rect);
            for(int x=rect.Left/16;x<=(rect.Right-1)/16;x++)for(int y=rect.Top/16;y<=(rect.Bottom-1)/16;y++) {
                if(!WorldGen.InWorld(x,y,10))return false;
                Point pos=new(x,y);Tile tile=Main.tile[x,y];
                bool root=stage==0 && (pos==plant.RootA || pos==plant.RootB);
                if(tile.LiquidAmount>0 || Main.wallHouse[tile.WallType] || (!root && tile.HasTile && tile.TileType!=cover))return false;
            }
            Rectangle margin=rect;margin.Inflate(4*16,3*16);
            for(int x=Math.Max(1,margin.Left/16);x<=Math.Min(Main.maxTilesX-2,margin.Right/16);x++)
                for(int y=Math.Max(1,margin.Top/16);y<=Math.Min(Main.maxTilesY-2,margin.Bottom/16);y++) {
                    Tile t=Main.tile[x,y];
                    if(t.HasTile && (t.TileType==giant || t.TileType==curved))return false;
                }
        }
        return RootSafe(plant.RootA)&&RootSafe(plant.RootB);
    }
    private void Place(Plant plant)
    {
        plants.Add(plant);Rebuild();int type=ModContent.TileType<SporeDreamCurvedGiant>();
        foreach(var entry in cells)if(ReferenceEquals(entry.Value.Plant,plant)) {
            Tile t=Main.tile[entry.Key.X,entry.Key.Y];t.ClearTile();t.HasTile=true;t.TileType=(ushort)type;
            t.TileFrameX=t.TileFrameY=0;
        }
        dirty=true;
    }
    private sealed record FloorPlan(Plant Plant,int X,int Ground,int Width);
    // 先做不改地形的配置檢查，避免失敗候選留下人工基座。
    private FloorPlan PlanFloor(int x,int ground,int growth)
    {
        var forest=SporeDreamForestSystem.Instance;
        if(Main.netMode==NetmodeID.MultiplayerClient || plants.Count>=MaximumPlants)return null;
        if(growth<0)growth=WorldGen.genRand.NextBool(6)?1:0;
        bool dwarf=growth==1;
        int direction=WorldGen.genRand.NextBool()?G.Left:G.Right;
        int padX=dwarf && direction==G.Left?x-1:x,padWidth=dwarf?2:1;
        if(!forest.PrepareGiantFoundation(padX,ground,false,padWidth))return null;
        for(int attempt=0;attempt<4;attempt++) {
            int startX=x*16+8;
            int segments=dwarf?WorldGen.genRand.Next(0,2):growth==2?WorldGen.genRand.Next(10,13):-1;
            var plant=new Plant(new(x,ground),new(dwarf?(direction==G.Left?x-1:x+1):x,ground),G.Create(startX,ground*16,G.Up,!dwarf && (BetweenUpwardCrowns(startX)?WorldGen.genRand.Next(5)<4:WorldGen.genRand.NextBool(5)),WorldGen.genRand.Next,segments,direction));
            if(!SpaceAvailable(plant))continue;
            return new FloorPlan(plant,padX,ground,padWidth);
        }
        return null;
    }
    public bool TryFloor(int x,int ground,int growth=-1)
    {
        FloorPlan plan=PlanFloor(x,ground,growth);
        if(plan is null || !SporeDreamForestSystem.Instance.PrepareGiantFoundation(plan.X,plan.Ground,true,plan.Width))return false;
        Place(plan.Plant);return true;
    }
    public int TryTallPair()
    {
        if(Main.netMode==NetmodeID.MultiplayerClient || plants.Count+2>MaximumPlants)return 0;
        var forest=SporeDreamForestSystem.Instance;
        var choices=Enumerable.Range(forest.Left+8,Math.Max(0,forest.Surface.Length-16)).ToList();
        int target=forest.Left+forest.Surface.Length/4;
        choices.Sort((a,b)=>Math.Abs(a-target).CompareTo(Math.Abs(b-target)));
        foreach(int first in choices) {
            FloorPlan one=PlanFloor(first,forest.Surface[first-forest.Left],2);
            if(one is null)continue;
            int secondTarget=forest.Left+forest.Surface.Length*3/4;
            foreach(int second in choices.OrderBy(x=>Math.Abs(x-secondTarget))) {
                if(Math.Abs(first-second)<Math.Max(20,forest.Surface.Length/3))continue;
                FloorPlan two=PlanFloor(second,forest.Surface[second-forest.Left],2);
                if(two is null)continue;
                // 先完成兩個位置的規劃再修改地形；較大的間距可避免兩側基座互相影響。
                if(!forest.PrepareGiantFoundation(one.X,one.Ground,true,one.Width)
                    || !forest.PrepareGiantFoundation(two.X,two.Ground,true,two.Width))return 0;
                Place(one.Plant);Place(two.Plant);return 2;
            }
        }
        return 0;
    }
    public int PopulateSides(int budget)
    {
        var forest=SporeDreamForestSystem.Instance;int count=0;
        var candidates=new List<(int X,int Direction)>();
        for(int x=forest.Left+8;x<forest.Right-8;x++)foreach(int direction in new[]{-1,1}) {
            int ground=forest.Surface[x-forest.Left];
            if(forest.Surface[x+direction-forest.Left]>=ground+3)candidates.Add((x,direction));
        }
        for(int i=candidates.Count-1;i>0;i--){int j=WorldGen.genRand.Next(i+1);(candidates[i],candidates[j])=(candidates[j],candidates[i]);}
        foreach(var candidate in candidates) {
            if(count>=budget || plants.Count>=MaximumPlants)break;
            int x=candidate.X,d=candidate.Direction,ground=forest.Surface[x-forest.Left];
            Point a=new(x,ground+1),b=a;
            // 16 像素菇柄接在完整側面物塊上；向內一層與上下自然土塊共同支撐接點。
            bool safe=true;
            for(int dx=0;dx<=1;dx++)for(int y=ground;y<=ground+2;y++)safe&=RootSafe(new Point(x-d*dx,y));
            if(!safe)continue;
            for(int attempt=0;attempt<4;attempt++) {
                int face=(d>0?x+1:x)*16;
                var plant=new Plant(a,b,G.Create(face,(ground+1)*16+8,d>0?G.Right:G.Left,WorldGen.genRand.NextBool(5),WorldGen.genRand.Next));
                if(!SpaceAvailable(plant))continue;
                for(int dx=0;dx<=1;dx++)for(int y=ground;y<=ground+2;y++) {
                    Tile t=Main.tile[x-d*dx,y];t.TileType=(ushort)(dx==0?ModContent.TileType<SporeDreamGrass>():TileID.Mud);
                    t.IsHalfBlock=false;t.Slope=0;t.TileFrameX=t.TileFrameY=0;
                }
                for(int xx=x-2;xx<=x+2;xx++)for(int y=ground-1;y<=ground+4;y++)WorldGen.SquareTileFrame(xx,y,false);
                Place(plant);count++;break;
            }
        }
        return count;
    }
    public void CutAt(int x,int y)
    {
        if(cutting || Main.netMode==NetmodeID.MultiplayerClient || !cells.TryGetValue(new(x,y),out var hit))return;
        Cut(hit.Plant,hit.Stage);
    }
    private void Cut(Plant plant,int stage)
    {
        cutting=true;
        var oldCells=cells.Where(p=>ReferenceEquals(p.Value.Plant,plant)).Select(p=>p.Key).ToArray();
        try {
            // 一次重建剩餘的相連部分，避免逐格遞迴呼叫 KillTile 或重複掉落物品。
            foreach(Point p in oldCells)Main.tile[p.X,p.Y].ClearTile();
            G.Cut(plant.Pieces,stage);
            if(plant.Pieces.Count==0)plants.Remove(plant);
            Rebuild();int type=ModContent.TileType<SporeDreamCurvedGiant>();
            foreach(var entry in cells)if(ReferenceEquals(entry.Value.Plant,plant)) {
                Tile t=Main.tile[entry.Key.X,entry.Key.Y];t.ClearTile();t.HasTile=true;t.TileType=(ushort)type;
            }
            if(Main.netMode==NetmodeID.Server && oldCells.Length>0) {
                int left=oldCells.Min(p=>p.X),top=oldCells.Min(p=>p.Y);
                NetMessage.SendTileSquare(-1,left,top,oldCells.Max(p=>p.X)-left+1,oldCells.Max(p=>p.Y)-top+1);
            }
            dirty=true;
        } finally {cutting=false;}
    }
    public override void PostUpdateWorld()
    {
        if(Main.netMode==NetmodeID.MultiplayerClient)return;
        if(Main.GameUpdateCount%30==0)foreach(Plant plant in plants.ToArray())
            if(!RootSafe(plant.RootA)||!RootSafe(plant.RootB))Cut(plant,0);
        if(dirty && Main.netMode==NetmodeID.Server) {
            var packet=Mod.GetPacket();packet.Write(SyncMessage);NetSend(packet);packet.Send();dirty=false;
        }
    }
    public override void SaveWorldData(TagCompound tag)
    {
        tag["GeometryVersion"]=2;
        tag["CurvedGiants"]=plants.Select(p=>new TagCompound {
            ["A"]=new[]{p.RootA.X,p.RootA.Y},["B"]=new[]{p.RootB.X,p.RootB.Y},
            ["Pieces"]=p.Pieces.SelectMany(s=>new[]{s.Kind,s.X,s.Y,s.Direction,s.Style,s.Extra}).ToArray()
        }).ToList();
    }
    public override void LoadWorldData(TagCompound tag)
    {
        ClearWorld();
        // 寬 36 像素的舊彎曲幾何只存在於未發布的隔離測試世界。
        if(tag.GetInt("GeometryVersion")!=2)return;
        foreach(var data in tag.GetList<TagCompound>("CurvedGiants")) {
            int[] a=data.GetIntArray("A"),b=data.GetIntArray("B"),values=data.GetIntArray("Pieces");
            if(a.Length!=2||b.Length!=2||values.Length%6!=0||values.Length>MaximumPieces*6||plants.Count>=MaximumPlants)continue;
            var pieces=new List<G.Piece>();
            for(int i=0;i<values.Length;i+=6)pieces.Add(new(values[i],values[i+1],values[i+2],values[i+3],values[i+4],values[i+5]));
            plants.Add(new(new(a[0],a[1]),new(b[0],b[1]),pieces));
        }
        Rebuild();
    }
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(plants.Count);
        foreach(Plant p in plants) {
            writer.Write(p.RootA.X);writer.Write(p.RootA.Y);writer.Write(p.RootB.X);writer.Write(p.RootB.Y);writer.Write(p.Pieces.Count);
            foreach(G.Piece s in p.Pieces){writer.Write(s.Kind);writer.Write(s.X);writer.Write(s.Y);writer.Write(s.Direction);writer.Write(s.Style);writer.Write(s.Extra);}
        }
    }
    // 先將整份資料讀入暫存並驗證，通過後才替換目前植株清單。
    public override void NetReceive(BinaryReader reader)
    {
        int count=reader.ReadInt32();if(count<0||count>MaximumPlants)throw new IOException("Invalid curved giant count");
        var received=new List<Plant>();
        for(int i=0;i<count;i++) {
            Point a=new(reader.ReadInt32(),reader.ReadInt32()),b=new(reader.ReadInt32(),reader.ReadInt32());
            int length=reader.ReadInt32();if(length<0||length>MaximumPieces)throw new IOException("Invalid curved giant path");
            var list=new List<G.Piece>();
            for(int j=0;j<length;j++) {
                var p=new G.Piece(reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32());
                if(p.Kind<0||p.Kind>G.End||p.Direction<0||p.Direction>3||p.X<0||p.Y<0||p.X>Main.maxTilesX*16||p.Y>Main.maxTilesY*16)throw new IOException("Invalid curved giant geometry");
                list.Add(p);
            }
            received.Add(new(a,b,list));
        }
        plants.Clear();plants.AddRange(received);Rebuild();
    }
    public void DrawAt(int i,int j,SpriteBatch batch,Vector2 offset,bool stemsOnly)
    {
        if(!draws.TryGetValue(new(i,j),out var list))return;
        foreach(var entry in list) {
            G.Piece p=entry.Plant.Pieces[entry.Stage];if(stemsOnly&&p.Kind==G.Crown)continue;
            DrawPiece(p,batch,offset,Lighting.GetColor(i,j),p.Kind==G.Crown?SpringOffset(entry.Plant):0);
        }
    }
    internal IEnumerable<G.Piece> MapPieces()
    {
        foreach(Plant plant in plants)foreach(var piece in plant.Pieces)yield return piece;
    }
    private Plant SpringAt(Point root)
        =>plants.FirstOrDefault(p=>p.RootA==root && SporeDreamSpringMath.IsSpring(p.Pieces));
    private static float SpringOffset(Plant plant)
    {
        if(!plant.Pressing)return 0;
        ulong elapsed=Main.GameUpdateCount-plant.PressStarted;
        return elapsed>=SporeDreamSpringMath.AnimationTicks?0:SporeDreamSpringMath.Offset((int)elapsed);
    }
    internal bool TrySpringSurface(Point root,float left,float width,out float top,out float depression)
    {
        top=depression=0;Plant plant=SpringAt(root);
        if(plant is null || !SporeDreamSpringMath.TryTop(plant.Pieces[^1],left,width,out top))return false;
        depression=SpringOffset(plant);return true;
    }
    // 只找本次向下位移會穿過的可彈菌蓋，取最先接觸的表面。
    internal bool TrySpringLanding(float left,float width,float before,float after,float velocity,out Point root,out float top)
    {
        root=default;top=float.MaxValue;bool found=false;
        foreach(Plant plant in plants) {
            if(!SporeDreamSpringMath.IsSpring(plant.Pieces)
                || !SporeDreamSpringMath.TryTop(plant.Pieces[^1],left,width,out float surface))continue;
            surface+=SpringOffset(plant);
            if(surface>=top || !SporeDreamSpringMath.Crosses(before,after,surface,velocity))continue;
            found=true;root=plant.RootA;top=surface;
        }
        return found;
    }
    internal int SpringAge(Point root)
    {
        Plant plant=SpringAt(root);
        return plant is null || !plant.Pressing?0:(int)Math.Min(Main.GameUpdateCount-plant.PressStarted,(ulong)SporeDreamSpringMath.AnimationTicks);
    }
    internal bool StartSpring(Point root,bool request=true)
    {
        Plant plant=SpringAt(root);if(plant is null)return false;
        // 收到伺服器回傳時保留所屬玩家已預測的壓縮進度，避免動畫重頭播放。
        if(!plant.Pressing || Main.GameUpdateCount-plant.PressStarted>=SporeDreamSpringMath.PressTicks) {
            plant.Pressing=true;plant.PressStarted=Main.GameUpdateCount;
        }
        if(request && Main.netMode==NetmodeID.MultiplayerClient) {
            var packet=Mod.GetPacket();packet.Write(SpringRequest);packet.Write(root.X);packet.Write(root.Y);packet.Send();
        }
        return true;
    }
    // 伺服器核對玩家位置、狀態與冷卻後，才轉送菌蓋壓縮事件。
    internal void ReceiveSpring(BinaryReader reader,int whoAmI,bool request)
    {
        Point root;
        try {root=new(reader.ReadInt32(),reader.ReadInt32());}catch(EndOfStreamException){return;}
        if(request) {
            if(Main.netMode!=NetmodeID.Server || whoAmI<0 || whoAmI>=Main.maxPlayers || whoAmI>=springRequestAfter.Length)return;
            Player player=Main.player[whoAmI];
            if(!player.active || player.dead || player.ghost || player.gravDir!=1 || player.mount.Active
                || Main.GameUpdateCount<springRequestAfter[whoAmI]
                || !TrySpringSurface(root,player.position.X,player.width,out float top,out float depression)
                || Math.Abs(player.Bottom.Y-top-depression)>64)return;
            springRequestAfter[whoAmI]=Main.GameUpdateCount+SporeDreamSpringMath.CooldownTicks;
            StartSpring(root,false);
            var packet=Mod.GetPacket();packet.Write(SpringAnimation);packet.Write(root.X);packet.Write(root.Y);packet.Send(-1,whoAmI);
        } else if(Main.netMode==NetmodeID.MultiplayerClient)StartSpring(root,false);
    }
    public bool TrySporeAt(int x,int y,out Vector2 center,out float spread)
    {
        center=default;spread=0;
        if(!draws.TryGetValue(new(x,y),out var list))return false;
        foreach(var entry in list) {
            G.Piece p=entry.Plant.Pieces[entry.Stage];
            if(p.Kind==G.Crown){center=CrownBounds(p).Center.ToVector2();spread=45;return true;}
            if(p.Kind is G.Vertical or G.Horizontal && p.Extra==1){Rectangle b=Bounds(p);center=b.Center.ToVector2();spread=14;return true;}
        }
        return false;
    }
    private static void DrawPiece(G.Piece p,SpriteBatch batch,Vector2 offset,Color color,float depression)
    {
        Vector2 screen=-Main.screenPosition+offset;
        void Draw(string file,Rectangle source,int x,int y)
            =>batch.Draw(ModContent.Request<Texture2D>(Root+file).Value,new Vector2(x,y)+screen,source,color);
        if(p.Kind==G.Crown) {
            var texture=ModContent.Request<Texture2D>(Root+"fleshy_mushroom_caps").Value;
            batch.Draw(texture,new Vector2(p.X,p.Y+depression)+screen,SporeDreamGiantLayout.CapFrame(p.Style),color,
                p.Direction*MathHelper.PiOver2,new Vector2(SporeDreamGiantLayout.CapAnchorX,72),1f,SpriteEffects.None,0f);
            return;
        }
        if(p.Kind==G.End) {
            float rotation=p.Direction*MathHelper.PiOver2;var texture=ModContent.Request<Texture2D>(Root+"amanita_crema_tiles_top").Value;
            Vector2 local=Vector2.Transform(new Vector2(-8,-4),Matrix.CreateRotationZ(rotation));
            SpriteEffects effect=p.Direction is G.Left or G.Down?SpriteEffects.FlipHorizontally:SpriteEffects.None;
            batch.Draw(texture,new Vector2(p.X,p.Y)+screen+local,SporeDreamGiantLayout.CutStemFrame(p.Style,0,24),color,rotation,Vector2.Zero,1f,effect,0f);
            return;
        }
        if(p.Kind==G.Vertical) {
            Draw("amanita_crema_tiles_02",SporeDreamGiantLayout.StemFrame(p.Style,p.Extra==1,0,20,p.Direction==G.Down),p.X-2,p.Y);
            return;
        }
        if(p.Kind==G.Horizontal) {
            Draw("amanita_crema_tiles",SporeDreamGiantLayout.HorizontalFrame(p.Style,p.Extra==1,p.Direction==G.Right),p.X,p.Y-2);
            return;
        }
        int corner=G.CornerIndex(p.Extra,p.Direction);
        Draw("amanita_crema_tiles_corner",new(corner*22,(p.Style%3)*22,20,20),p.X,p.Y);
    }
}
