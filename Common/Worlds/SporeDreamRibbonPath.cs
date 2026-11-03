using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Occident.Common.Worlds;

internal static class SporeDreamRibbonPath
{
    internal readonly record struct Sample(Vector2 Position,bool Front,float Progress,float Radius);

    // Trace the entire biome before viewport culling so depth never restarts at the camera edge.
    internal static void Append(float left,float right,float phase,Func<float,float> ground,
        Func<Vector2,bool> touchesStem,List<Sample> output)
    {
        if(right<=left) return;
        int steps=Math.Max(1,(int)Math.Ceiling((right-left)/4f));
        bool touching=false,front=false;
        for(int i=0;i<=steps;i++) {
            float progress=i/(float)steps;
            float x=left+(right-left)*progress;
            float wave=x/185f+phase;
            float y=ground(x)-102f+MathF.Sin(wave)*42f+MathF.Sin(x/83f-phase*.63f)*15f;
            var position=new Vector2(x,y);
            bool contact=touchesStem(position);
            if(contact&&!touching) front=!front;
            touching=contact;
            // Broad and fine waves create smooth, independently travelling thick and thin sections.
            float thickness=(MathF.Sin(x/137f-phase*.71f)+1f)*.5f;
            float detail=(MathF.Sin(x/51f+phase*.43f)+1f)*.5f;
            float radius=10f+38f*thickness*thickness+8f*detail;
            output.Add(new(position,front,progress,radius));
        }
    }
}
