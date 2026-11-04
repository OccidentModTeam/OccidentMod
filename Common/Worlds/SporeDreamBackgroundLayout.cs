// 計算群系全景圖位置與地下淡出，像素與物塊單位在此轉換。
using System;

namespace Occident.Common.Worlds;

internal static class SporeDreamBackgroundLayout
{
    internal readonly record struct Panorama(int Left,int Top,int Width,int Height,int FogStart)
    {
        public int Bottom => Top+Height;
    }

    // 用整片地表的中位數作背景錨點，降低局部深坑對背景位置的影響。
    internal static float RepresentativeGround(int[] surface)
    {
        if(surface.Length==0) return 0f;
        int[] heights=(int[])surface.Clone();
        Array.Sort(heights);
        return heights[heights.Length/2]*16f;
    }

    internal static Panorama Place(int imageWidth,int imageHeight,int screenWidth,int screenHeight,
        float groundY,float cameraY,float horizontalProgress)
    {
        int scale=Math.Max(2,(int)Math.Ceiling(screenWidth/(float)imageWidth));
        int width=imageWidth*scale,height=imageHeight*scale;
        float bottom=screenHeight*0.92f+(groundY-cameraY-screenHeight*0.65f)*0.2f;
        int top=(int)Math.Round(bottom-height);
        int left=-(int)Math.Round((width-screenWidth)*Math.Clamp(horizontalProgress,0f,1f));
        int fogStart=top+height-Math.Max(64,height/6);
        return new Panorama(left,top,width,height,fogStart);
    }

    // 用物塊深度做平滑淡出，進入地下時逐步隱去地表背景。
    internal static float SurfaceVisibility(float playerY,float localGroundY)
    {
        float depth=(playerY-localGroundY)/16f;
        float t=Math.Clamp((depth-8f)/24f,0f,1f);
        return 1f-t*t*(3f-2f*t);
    }
}
