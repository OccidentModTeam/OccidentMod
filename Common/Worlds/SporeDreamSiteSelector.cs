using System;
using System.Collections.Generic;

namespace Occident.Common.Worlds;

internal static class SporeDreamSiteSelector
{
    internal readonly record struct Column(int Ground,bool Safe,bool Grass,bool Evil,int WaterDepth=0);
    internal sealed record Site(int Left,int Score,int[] Surface);
    internal sealed record Search(List<Site> Sites,string Mode);

    // All passes retain the same safety checks. Rescue changes only the sampling
    // density, distance from spawn and, as a last resort, forest width.
    internal static Search Find(int worldWidth,int spawn,Func<int,Column> column)
    {
        int[] widths=SporeDreamGenerationScale.Widths(worldWidth);
        var sites=Scan(worldWidth,spawn,widths,column,false);
        if(sites.Count>0)return new(sites,"standard");
        sites=Scan(worldWidth,spawn,widths,column,true);
        if(sites.Count>0)return new(sites,"detailed-safe-search");
        var rescueWidths=new List<int>(widths);
        for(int small=160;small>=64;small-=16)rescueWidths.Add(SporeDreamGenerationScale.Distance(small,worldWidth));
        // Keep the original pre-evil placement rule. In a cramped starting
        // forest, use less empty buffer before reducing the actual biome width.
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
            // Inspect every column, not every fourth: thin evil edges also count.
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
