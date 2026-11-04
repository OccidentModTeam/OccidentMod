// 集中巨菇素材裁切、基座幾何、穩定樣式與裝飾間距。
using System;
using Microsoft.Xna.Framework;

namespace Occident.Common.Worlds;

// 以原圖像素定義裁切範圍，分隔線像素不納入繪圖矩形。
internal static class SporeDreamGiantLayout
{
    public const int CapCount=3,CapWidth=126,CapHeight=72,CapStride=130,CapAnchorX=62;
    public const int StemWidth=20,StemHeight=20,StemRows=5,StemRowStride=22,StemColumnStride=22;
    public const int MinimumTrunkSpacing=9;
    public const int MinimumHeight=9,MaximumHeight=16;
    public const int CutTopExtension=4,CutStemHeight=24;

    public static bool TryFoundationProfile(ReadOnlySpan<int> original,int ground,Span<int> result,int stemWidth=1)
    {
        if(stemWidth is <1 or >2 || original.Length!=stemWidth+6 || result.Length<original.Length) return false;
        for(int index=0;index<original.Length;index++) {
            int dx=index-3,old=original[index];
            int distance=dx<0?-dx:dx-stemWidth+1;
            int target=Math.Min(old,ground+Math.Max(0,distance-1));
            if(dx>=0 && dx<stemWidth) target=ground;
            if(target>old || old-target>2 || Math.Abs(old-ground)>4) return false;
            result[index]=target;
        }
        return true;
    }

    public static Rectangle CapFrame(int style) => new(Math.Clamp(style,0,CapCount-1)*CapStride,0,CapWidth,CapHeight);
    public static Rectangle StemFrame(int style,bool decorated,int row,int height,bool down=false)
        => new(Math.Clamp(style,0,StemRows-1)*StemColumnStride,
            ((down?2:0)+(decorated?1:0))*StemRowStride+row,StemWidth,height);
    public static Rectangle CutStemFrame(int style,int row,int height)
        => new(Math.Clamp(style,0,StemRows-1)*18,row,16,height);
    public static Rectangle HorizontalFrame(int style,bool decorated,bool right)
        => new(Math.Clamp(style,0,4)*22,(right?46:0)+(decorated?22:0),20,20);

    // 世界代號與座標產生穩定樣式，避免每幀重新抽樣而閃爍。
    public static uint Hash(int x,int top,int value,int worldId)
    {
        unchecked {
            uint h=(uint)x*73856093u^(uint)top*19349663u^(uint)(value+1)*83492791u^(uint)worldId;
            h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return h^(h>>16);
        }
    }
    public static int Style(int x,int top,int segment,int worldId)
        => segment==0?0:(int)(Hash(x,top,segment,worldId)%StemRows);
    public static int SegmentCount(int bodyPixels) => (Math.Max(0,bodyPixels)+StemHeight-1)/StemHeight;

    // 先決定整株裝飾配額，不逐節獨立抽選；兩端留白，兩個裝飾間至少隔兩個空白節。
    public static (int First,int Second) DecorationSlots(int x,int top,int bodyPixels,int worldId)
    {
        int count=SegmentCount(bodyPixels),eligible=count-4;
        if(eligible<=0) return (-1,-1);
        uint seed=Hash(x,top,7919,worldId);
        if(seed%10<2) return (-1,-1);
        int first=2+(int)((seed>>8)%(uint)eligible);
        if(seed%10!=9) return (first,-1);
        int available=0;
        for(int i=2;i<=count-3;i++) if(Math.Abs(i-first)>=3) available++;
        if(available==0) return (first,-1);
        int choice=(int)((seed>>16)%(uint)available);
        for(int i=2;i<=count-3;i++) if(Math.Abs(i-first)>=3 && choice--==0) return (first,i);
        return (first,-1);
    }

    public static bool Decorated(int x,int top,int bodyPixels,int segment,int worldId)
    {
        var slots=DecorationSlots(x,top,bodyPixels,worldId);
        return segment>=0 && (segment==slots.First || segment==slots.Second);
    }

    // 16 像素物塊可能跨越兩個 20 像素素材格；逐段裁切對應原圖，不拉伸成物塊尺寸。
    public static int SliceHeight(int bodyPixel,int remaining) => Math.Min(remaining,StemHeight-bodyPixel%StemHeight);
}
