// 維護進出群系的視覺混合，只在天體繪製期間暫用反轉顯示時鐘。
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
    // 背景與前景亮度分開控制；此全景圖使用獨立的中性曝光值，並非直接沿用原版猩紅背景常數。
    public float BackgroundBrightness => 0.78f;
    public Color BackgroundTint(float opacity)
    {
        int level=(int)Math.Round(255f*BackgroundBrightness);
        // 亮度只調整色彩通道，透明度交由群系淡入淡出控制。
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
            // 進入群系時擷取一次世界時間，之後本地天空停在這個對應時段。
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
            // 只在繪製天體時暫用顯示時鐘；世界照明、背景圖、天氣與模擬仍使用原本資料。
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
    // 將日夜映射到同一循環，取較短方向平滑過渡，最後轉回原版時間格式。
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
        // 兩種天空狀態皆用中性日光；紅色與淺色只作用於天空層。地形仍使用原版光照圖，地下物塊照常遮光。
        tileColor=Color.Lerp(tileColor,Color.White,Blend);
        backgroundColor=Color.Lerp(backgroundColor,Color.White,Blend);
    }
}
