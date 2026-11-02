using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Occident.Common.Worlds;

[Autoload(Side=ModSide.Client)]
public sealed class SporeDreamVisuals : ModSystem
{
    public const int TransitionTicks=420;
    public float Blend { get; private set; }
    private bool? renderSourceDay;
    public override void Load() => On_Main.DoDraw += DrawDream;
    public override void Unload() => On_Main.DoDraw -= DrawDream;
    public override void ClearWorld() => Blend=0;
    public override void PostUpdateEverything()
    {
        if(Main.gameMenu) { Blend=0; return; }
        var p=Main.LocalPlayer.Center.ToTileCoordinates();
        float target=SporeDreamForestSystem.Instance.Contains(p.X,p.Y) ? 1 : 0;
        float step=1f/TransitionTicks;
        Blend=target>Blend ? Math.Min(target,Blend+step) : Math.Max(target,Blend-step);
    }
    private void DrawDream(On_Main.orig_DoDraw orig,Main self,GameTime gameTime)
    {
        if(Main.gameMenu || Blend<=0) { orig(self,gameTime); return; }
        bool day=Main.dayTime; double time=Main.time;
        try {
            // Render scope only. Simulation, server time, spawning and world saves keep actual time.
            renderSourceDay=day;
            (Main.dayTime,Main.time)=DisplayClock(day,time,Blend);
            orig(self,gameTime);
        } finally { Main.dayTime=day; Main.time=time; renderSourceDay=null; }
    }
    public static (bool Day,double Time) DisplayClock(bool day,double time,float blend)
    {
        double cycle=Main.dayLength+Main.nightLength;
        double source=day ? time : Main.dayLength+time;
        double target=day ? Main.dayLength+time/Main.dayLength*Main.nightLength : time/Main.nightLength*Main.dayLength;
        double delta=target-source;
        if(delta>cycle/2) delta-=cycle;
        if(delta< -cycle/2) delta+=cycle;
        double eased=MathHelper.SmoothStep(0,1,MathHelper.Clamp(blend,0,1));
        double clock=(source+delta*eased+cycle)%cycle;
        return clock<Main.dayLength ? (true,clock) : (false,clock-Main.dayLength);
    }
    public override void ModifySunLightColor(ref Color tileColor,ref Color backgroundColor)
    {
        if(Main.gameMenu || Blend<=0) return;
        // Entire viewport uses one transition; no vertical day/night split at the forest border.
        bool sourceDay=renderSourceDay ?? Main.dayTime;
        // Soft blue-grey moonlight preserves readable warm mushroom colors without making night look like day.
        Color terrain=sourceDay ? new Color(125,137,162) : new Color(220,215,198);
        Color sky=sourceDay ? new Color(92,108,142) : new Color(220,215,198);
        tileColor=Color.Lerp(tileColor,terrain,Blend);
        backgroundColor=Color.Lerp(backgroundColor,sky,Blend);
    }
}
