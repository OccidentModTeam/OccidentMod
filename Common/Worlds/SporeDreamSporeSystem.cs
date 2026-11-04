// 在鏡頭附近的巨菇生成、更新並繪製有限數量的孢子粒子。
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Occident.Content.Tiles.SporeDream;

namespace Occident.Common.Worlds;

[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamSporeSystem : ModSystem
{
    private sealed class Spore
    {
        public Vector2 Position,Velocity;
        public int Age,Life;
        public float Phase;
    }

    private const int MaxSpores=36;
    private readonly List<Spore> spores=new();
    private int tick;

    public override void ClearWorld() { spores.Clear(); tick=0; }

    public override void PostUpdateEverything()
    {
        if(Main.gameMenu || Main.gamePaused) return;
        for(int i=spores.Count-1;i>=0;i--) {
            Spore spore=spores[i];
            if(++spore.Age>=spore.Life) { spores.RemoveAt(i); continue; }
            spore.Position+=spore.Velocity+new Vector2(MathF.Sin(spore.Age*.035f+spore.Phase)*.06f,0f);
        }
        if(++tick%10!=0 || spores.Count>=MaxSpores) return;
        var forest=SporeDreamForestSystem.Instance;
        var playerTile=Main.LocalPlayer.Center.ToTileCoordinates();
        if(!forest.Contains(playerTile.X,playerTile.Y)) return;
        int giant=ModContent.TileType<SporeDreamGiant>(),plants=ModContent.TileType<SporeDreamPlants>();
        int fruit=ModContent.TileType<PandoraPlant>();
        int left=Math.Max(forest.Left,Math.Max(1,(int)(Main.screenPosition.X/16f)-2));
        int right=Math.Min(forest.Right-1,Math.Min(Main.maxTilesX-2,(int)((Main.screenPosition.X+Main.screenWidth)/16f)+2));
        int top=Math.Max(1,(int)(Main.screenPosition.Y/16f)-4);
        int bottom=Math.Min(Main.maxTilesY-2,(int)((Main.screenPosition.Y+Main.screenHeight)/16f)+2);
        // 交替掃描方向，避免粒子數上限總是優先保留畫面同一側。
        bool reverse=Main.rand.NextBool();
        for(int n=0;n<=right-left;n++) {
            int x=reverse ? right-n : left+n;
            for(int y=top;y<=bottom;y++) {
                Tile tile=Main.tile[x,y];
                if(!tile.HasTile) continue;
                if(tile.TileType==plants && Main.rand.NextBool(60))
                    Emit(new Vector2(x*16+8,y*16-3),8f);
                else if(tile.TileType==fruit && tile.TileFrameX%36==0 && tile.TileFrameY==0 && Main.rand.NextBool(45))
                    Emit(new Vector2(x*16+16,y*16+4),12f);
                else if(tile.TileType==ModContent.TileType<SporeDreamCurvedGiant>() && Main.rand.NextBool(30)
                    && SporeDreamCurvedSystem.Instance.TrySporeAt(x,y,out var center,out float spread))
                    Emit(center,spread);
                else if(tile.TileType==giant) {
                    if(!SporeDreamGiant.IsPrimaryTile(x,y)) continue;
                    if(tile.TileFrameY==0) {
                        if(Main.rand.NextBool(25)) {
                            Rectangle cap=SporeDreamGiant.CapFrames[SporeDreamGiant.CapStyle(x,y)];
                            Emit(new Vector2(SporeDreamGiant.StemCenterX(x,y),y*16+16-cap.Height*.65f),cap.Width*.38f);
                        }
                    } else if(Main.rand.NextBool(60) && SporeDreamGiant.HasDecoratedStemAt(x,y))
                        Emit(new Vector2(x*16+8,y*16+8),14f);
                }
            }
        }
    }

    private void Emit(Vector2 center,float spread)
    {
        if(spores.Count>=MaxSpores) return;
        spores.Add(new Spore {
            Position=center+new Vector2(Main.rand.NextFloat(-spread,spread),Main.rand.NextFloat(-5f,3f)),
            Velocity=new Vector2(Main.rand.NextFloat(-.12f,.12f),Main.rand.NextFloat(-.23f,-.1f)),
            Life=Main.rand.Next(100,170),Phase=Main.rand.NextFloat(MathHelper.TwoPi)
        });
    }

    public override void PostDrawTiles()
    {
        if(Main.gameMenu || spores.Count==0) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,
            DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        try {
            foreach(Spore spore in spores) {
                float progress=spore.Age/(float)spore.Life;
                float opacity=MathF.Sin(progress*MathHelper.Pi)*.48f;
                Vector2 position=spore.Position-Main.screenPosition;
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)position.X,(int)position.Y,2,2),new Color(218,202,196)*opacity);
            }
        }
        finally { Main.spriteBatch.End(); }
    }
}
