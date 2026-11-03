using G=Occident.Common.Worlds.SporeDreamGrowthPath;
namespace Occident.Common.Worlds;

// Native pixel transforms, matching SpriteBatch's quarter-turn pixel centers.
internal static class SporeDreamMapGeometry
{
    internal static (int X,int Y) Pixel(G.Piece p,int x,int y)=>p.Kind switch {
        G.Crown=>p.Direction switch {
            G.Right=>(p.X+71-y,p.Y-62+x),
            G.Left=>(p.X-72+y,p.Y+61-x),
            _=>(p.X-62+x,p.Y-72+y)
        },
        G.Vertical=>(p.X-2+x,p.Y+y),
        G.Horizontal=>(p.X+x,p.Y-2+y),
        G.End=>p.Direction switch {
            G.Right=>(p.X+3-y,p.Y-8+x),
            G.Down=>(p.X-8+x,p.Y+3-y),
            G.Left=>(p.X-4+y,p.Y-8+x),
            _=>(p.X-8+x,p.Y-4+y)
        },
        _=>(p.X+x,p.Y+y)
    };
    internal static (int X,int Y) Tile(int x,int y)=>((int)System.MathF.Floor(x/16f),(int)System.MathF.Floor(y/16f));
}
