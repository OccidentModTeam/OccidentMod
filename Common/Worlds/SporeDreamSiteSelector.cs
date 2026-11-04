// 先篩安全森林候選，再逐步增加搜尋密度或縮小範圍。
using System;
using System.Collections.Generic;

namespace Occident.Common.Worlds;

internal static class SporeDreamSiteSelector
{
    internal readonly record struct Column(int Ground,bool Safe,bool Grass,bool Evil,int WaterDepth=0);
    internal sealed record Site(int Left,int Score,int[] Surface);
    internal sealed record Search(List<Site> Sites,string Mode);

    // 所有搜尋階段沿用同樣安全檢查；補救只調整取樣密度、出生點距離，最後才縮小群系寬度。
    internal static Search Find(int worldWidth,int spawn,Func<int,Column> column)
    {
        int[] widths=SporeDreamGenerationScale.Widths(worldWidth);
        var sites=Scan(worldWidth,spawn,widths,column,false);
        if(sites.Count>0)return new(sites,"standard");
        sites=Scan(worldWidth,spawn,widths,column,true);
        if(sites.Count>0)return new(sites,"detailed-safe-search");
        var rescueWidths=new List<int>(widths);
        for(int small=160;small>=64;small-=16)rescueWidths.Add(SporeDreamGenerationScale.Distance(small,worldWidth));
        // 維持位於邪惡環境之前的規則；空間不足時先縮短緩衝，再縮小實際群系寬度。
        sites=Scan(worldWidth,spawn,rescueWidths.ToArray(),column,true,true);
        return new(sites,"compact-safe-search");
    }
    private static List<Site> Scan(int worldWidth,int spawn,int[] widths,Func<int,Column> column,bool detailed,bool compact=false)
    {
        var sites=new List<Site>();
        int preferred=SporeDreamGenerationScale.Widths(worldWidth)[0];
        int searchDistance=SporeDreamGenerationScale.Distance(1100,worldWidth);
        int evilSearchDistance=SporeDreamGenerationScale.Distance(1400,worldWidth);
        int gap=SporeDreamGenerationScale.Distance(compact?16:48,worldWidth);
        int step=detailed?1:SporeDreamGenerationScale.Distance(12,worldWidth);
        foreach(int direction in new[]{-1,1}) {
            int evilDistance=worldWidth;
            // 逐欄檢查邪惡邊界，避免跳欄取樣漏掉很薄的邪惡地形。
            for(int d=80;d<Math.Min(evilSearchDistance,worldWidth/3);d++) {
                int x=spawn+direction*d;
                if(x<180||x>=worldWidth-180)break;
                if(column(x).Evil){evilDistance=d;break;}
            }
            foreach(int width in widths) {
                int minimum=compact?width/2+gap:detailed
                    ?Math.Max(SporeDreamGenerationScale.Distance(120,worldWidth),width/2+gap)
                    :SporeDreamGenerationScale.Distance(220,worldWidth);
                int limit=Math.Min(searchDistance,evilDistance-width/2-gap);
                for(int distance=minimum;distance<limit;distance+=step) {
                    int left=spawn+direction*distance-width/2;
                    if(left<180||left+width>=worldWidth-180)continue;
                    int[] surface=new int[width];int grass=0,min=int.MaxValue,max=0,water=0;bool valid=true;
                    for(int k=0;k<width;k++) {
                        Column c=column(left+k);
                        if(!c.Safe){valid=false;break;}
                        surface[k]=c.Ground;grass+=c.Grass?1:0;water+=c.WaterDepth;
                        min=Math.Min(min,c.Ground);max=Math.Max(max,c.Ground);
                    }
                    if(!valid||grass<width/4||max-min>80)continue;
                    int score=distance+(max-min)*2+(preferred-width)*3+water*2;
                    sites.Add(new(left,score,surface));
                }
            }
        }
        return sites;
    }
}
