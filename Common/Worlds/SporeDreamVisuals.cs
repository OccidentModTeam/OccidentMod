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
    private bool? entryDay;
    private double entryTime;
    private bool wasInside;
    private int transitionProgress;
    // Vanilla Crimson shades its background independently from foreground tiles.
    // This bright panorama needs its own neutral exposure adjustment; this is an
    // art-specific starting point, not a copied vanilla Crimson constant.
    public float BackgroundBrightness => 0.78f;
    public Color BackgroundTint(float opacity)
    {
        int level=(int)Math.Round(255f*BackgroundBrightness);
        // Brightness changes RGB only. Alpha belongs solely to the biome fade.
        return new Color(level,level,level)*MathHelper.Clamp(opacity,0f,1f);
    }
    public (Color Top, Color Horizon) SkyPalette => (entryDay ?? Main.dayTime)
        ? (new Color(78,12,22), new Color(157,37,45))
        : (new Color(159,166,170), new Color(222,225,224));
    public override void Load()
    {
        On_Main.DrawStarsInBackground += DrawStars;
        On_Main.DrawSunAndMoon += DrawSunAndMoon;
    }
    public override void Unload()
    {
        On_Main.DrawStarsInBackground -= DrawStars;
        On_Main.DrawSunAndMoon -= DrawSunAndMoon;
    }
    public override void ClearWorld()
    {
        Blend=0;
        entryDay=null;
        entryTime=0;
        wasInside=false;
        transitionProgress=0;
    }
    public override void PostUpdateEverything()
    {
        if(Main.gameMenu) { ClearWorld(); return; }
        var p=Main.LocalPlayer.Center.ToTileCoordinates();
        bool inside=SporeDreamForestSystem.Instance.Contains(p.X,p.Y);
        if(inside && !wasInside) {
            // Capture simulation time once. The local sky then stays at this phase.
            entryDay=Main.dayTime;
            entryTime=Main.time;
        }
        wasInside=inside;
        transitionProgress=Math.Clamp(transitionProgress+(inside ? 1 : -1),0,TransitionTicks);
        Blend=transitionProgress/(float)TransitionTicks;
        if(Blend<=0) entryDay=null;
    }
    private void ApplySkyClock()
    {
        var frozen=DisplayClock(entryDay ?? Main.dayTime,entryDay.HasValue ? entryTime : Main.time,1f);
        (Main.dayTime,Main.time)=InterpolateClock(Main.dayTime,Main.time,frozen.Day,frozen.Time,Blend);
    }
    private void DrawStars(On_Main.orig_DrawStarsInBackground orig,Main self,Main.SceneArea sceneArea,bool artificial)
    {
        if(Main.gameMenu || Blend<=0) { orig(self,sceneArea,artificial); return; }
        bool day=Main.dayTime; double time=Main.time;
        try {
            ApplySkyClock();
            orig(self,sceneArea,artificial);
        } finally { Main.dayTime=day; Main.time=time; }
    }
    private void DrawSunAndMoon(On_Main.orig_DrawSunAndMoon orig,Main self,Main.SceneArea sceneArea,
        Color moonColor,Color sunColor,float mushroomInfluence)
    {
        if(Main.gameMenu || Blend<=0) { orig(self,sceneArea,moonColor,sunColor,mushroomInfluence); return; }
        bool day=Main.dayTime; double time=Main.time;
        try {
            // Only celestial rendering sees the frozen clock. World lighting,
            // biome artwork, weather and simulation keep their original inputs.
            ApplySkyClock();
            orig(self,sceneArea,moonColor,sunColor,mushroomInfluence);
        } finally { Main.dayTime=day; Main.time=time; }
    }
    public static (bool Day,double Time) DisplayClock(bool day,double time,float blend)
    {
        bool targetDay=!day;
        double targetTime=day ? time/Main.dayLength*Main.nightLength : time/Main.nightLength*Main.dayLength;
        return InterpolateClock(day,time,targetDay,targetTime,blend);
    }
    public static (bool Day,double Time) InterpolateClock(bool day,double time,bool targetDay,double targetTime,float blend)
    {
        double cycle=Main.dayLength+Main.nightLength;
        double source=day ? time : Main.dayLength+time;
        double target=targetDay ? targetTime : Main.dayLength+targetTime;
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
        // Both visual sky phases use neutral daytime sunlight. The red/pale
        // palette belongs exclusively to the sky layer. Terrain still uses the
        // normal light map, so blocks continue to obstruct sunlight underground.
        tileColor=Color.Lerp(tileColor,Color.White,Blend);
        backgroundColor=Color.Lerp(backgroundColor,Color.White,Blend);
    }
}
