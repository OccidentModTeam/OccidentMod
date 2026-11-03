using System;
using System.Collections.Generic;

namespace Occident.Common.Worlds;

// Pixel-space geometry. Twenty-pixel artwork is never stretched to a tile.
internal static class SporeDreamGrowthPath
{
    internal const int Up=0,Right=1,Down=2,Left=3;
    internal const int Vertical=0,Horizontal=1,Turn=2,Crown=3,End=4;
    internal readonly record struct Piece(int Kind,int X,int Y,int Direction,int Style,int Extra=0)
    {
        internal (int X,int Y) Exit => Kind switch {
            Vertical => (X+8,Direction==Up?Y:Y+20),
            Horizontal => (Direction==Right?X+20:X,Y+8),
            Turn => (X+Port(CornerIndex(Extra,Direction),Direction).X,Y+Port(CornerIndex(Extra,Direction),Direction).Y),
            _ => (X,Y)
        };
    }
    internal static int DX(int direction)=>direction==Right?1:direction==Left?-1:0;
    internal static int DY(int direction)=>direction==Down?1:direction==Up?-1:0;
    internal static int CornerIndex(int incoming,int outgoing)
    {
        int entry=(incoming+2)%4;
        if((entry+outgoing)%2==0)throw new ArgumentException("An elbow needs one vertical and one horizontal port");
        return (entry==Down||outgoing==Down?2:0)+(entry==Right||outgoing==Right?1:0);
    }
    internal static (int X,int Y) Port(int corner,int direction)
        =>direction switch {
            Up=>(corner%2==1?8:12,0),Down=>(corner%2==1?8:12,20),
            Right=>(20,corner>=2?8:12),_=>(0,corner>=2?8:12)
        };
    internal static void Cut(List<Piece> pieces,int stage)
    {
        if(stage<0||stage>=pieces.Count)throw new ArgumentOutOfRangeException(nameof(stage));
        pieces.RemoveRange(stage,pieces.Count-stage);
        if(pieces.Count==0)return;
        Piece last=pieces[^1];var end=last.Exit;
        pieces.Add(new(End,end.X,end.Y,last.Direction,last.Style%5));
    }
    internal static List<Piece> Create(int x,int y,int initialDirection,bool multiple,Func<int,int,int> next,int uprightSegments=-1,int floorSide=-1)
    {
        var result=new List<Piece>();int direction=initialDirection;
        void Straight(int count) {
            for(int i=0;i<count;i++) {
                int nx=x+DX(direction)*20,ny=y+DY(direction)*20;
                result.Add(new(direction is Up or Down?Vertical:Horizontal,
                    direction is Up or Down?x-8:Math.Min(x,nx),
                    direction is Up or Down?Math.Min(y,ny):y-8,direction,next(0,5)));
                x=nx;y=ny;
            }
        }
        void Bend(int outgoing) {
            int corner=CornerIndex(direction,outgoing);
            var entry=Port(corner,(direction+2)%4);var exit=Port(corner,outgoing);
            int left=x-entry.X,top=y-entry.Y;
            result.Add(new(Turn,left,top,outgoing,next(0,4),direction));
            x=left+exit.X;y=top+exit.Y;direction=outgoing;
        }
        if(direction==Up) {
            // A full ground root has a downward-facing entry, a short lateral
            // section, and an upward exit. Neither end is left horizontal in soil.
            int side=floorSide is Left or Right?floorSide:(next(0,2)==0?Left:Right);
            Bend(side);Straight(1);Bend(Up);
            Straight(uprightSegments>=0?uprightSegments:next(6,10));
            if(multiple && uprightSegments!=0 && uprightSegments!=1) {Bend(side);Straight(1);}
        } else {
            int outward=direction;
            Straight(uprightSegments is 0 or 1?1:next(1,3));Bend(Up);
            // A second elbow is rare and separated by at least 100px of upright stem.
            Straight(uprightSegments>=0?uprightSegments:next(5,9));
            if(multiple && uprightSegments!=0 && uprightSegments!=1) {Bend(outward);Straight(next(1,3));}
        }
        result.Add(new(Crown,x,y,direction,next(0,3)));
        var eligible=new List<int>();
        for(int i=2;i<result.Count-2;i++)if(result[i].Kind==Vertical)eligible.Add(i);
        // Allocate per plant: visible swellings without consecutive tumor cells.
        if(eligible.Count>0 && next(0,10)<9) {
            int quota=eligible.Count>=10?3:eligible.Count>=5?2:1;
            for(int chosen=0;chosen<quota && eligible.Count>0;chosen++) {
                int selected=eligible[next(0,eligible.Count)];
                result[selected]=result[selected] with {Extra=1};
                eligible.RemoveAll(i=>Math.Abs(i-selected)<3);
            }
        }
        return result;
    }
}
