using System;
using System.Collections.Generic;

namespace OwnCodeArchive
{
// Algorithm-only extraction of our retired local depth-refinement machinery.
// Every geometric input is supplied by the caller. No game-specific data is included.
public static class DepthPartition
{
    public readonly struct Band
    {
        public readonly int Left, Width, Depth;
        public Band(int left, int width, int depth) { Left = left; Width = width; Depth = depth; }
    }
    public readonly struct Span
    {
        public readonly int Column, Top, Bottom, Depth;
        public Span(int column, int top, int bottom, int depth)
        { Column = column; Top = top; Bottom = bottom; Depth = depth; }
    }
    public readonly struct Point
    {
        public readonly float X, Y;
        public Point(float x, float y) { X = x; Y = y; }
    }
    public enum Part { Front, Rear, Foreground, SupportFront, SupportRear }
    public readonly struct Quad
    {
        public readonly int Left, Top, Width, Height, Depth, Ceiling;
        public readonly Part Kind;
        public Quad(int left, int top, int width, int height, int depth, int ceiling, Part kind)
        { Left=left; Top=top; Width=width; Height=height; Depth=depth; Ceiling=ceiling; Kind=kind; }
    }

    // Retains the old per-source-band ceiling before grouping equal effective
    // depths. Taking the minimum after merging would change unrelated bands.
    public static List<Quad> RefineSupport(int width, int height, int scale,
        int columnWidth, int projectionZ, IReadOnlyList<Band> original, IReadOnlyList<Span> supports)
    {
        Dimensions(width, height, scale, columnWidth);
        var xCuts = new SortedSet<int> { 0, width };
        for (int left = checked(columnWidth * scale); left < width; left += checked(columnWidth * scale)) xCuts.Add(left);
        foreach (var band in original) { xCuts.Add(band.Left); xCuts.Add(checked(band.Left + band.Width)); }
        var xs = new List<int>(xCuts);
        var result = new List<Quad>();
        for (int i = 0; i + 1 < xs.Count; i++)
        {
            int left=xs[i], right=xs[i+1], ceiling=CeilingAt(original, left);
            if (left < 0 || right > width) throw new ArgumentException("Source band outside frame.");
            int column=left / checked(columnWidth * scale);
            var yCuts=new SortedSet<int>{0,height};
            foreach(var span in supports) if(span.Column==column)
            {
                int top=checked(span.Top*scale), bottom=checked(span.Bottom*scale);
                if(top<0 || bottom>height || top>bottom) throw new ArgumentException("Support outside frame.");
                yCuts.Add(top); yCuts.Add(bottom);
            }
            var ys=new List<int>(yCuts);
            for(int y=0;y+1<ys.Count;y++)
            {
                int depth=ceiling; bool support=false;
                foreach(var span in supports)
                    if(span.Column==column && ys[y]>=span.Top*scale && ys[y+1]<=span.Bottom*scale)
                    {depth=Math.Min(ceiling, checked(span.Depth-projectionZ));support=true;break;}
                result.Add(new Quad(left,ys[y],right-left,ys[y+1]-ys[y],depth,ceiling,support?Part.SupportRear:Part.Front));
            }
        }
        return result;
    }

    // Complementary texel partition. Preserved islands win over supports. A
    // separate component's depth is a caller parameter, never a built-in sample.
    public static List<Quad> PartitionAperture(int width, int height, int scale,
        int trimLeft, int trimTop, int columnWidth, IReadOnlyList<Band> original,
        Point[] rearPolygon, Point[][] preserved, IReadOnlyList<Span> supports,
        bool permitFrontSupports, Point[] separateComponent, int separateDepth)
    {
        Dimensions(width,height,scale,columnWidth);
        var result=new List<Quad>();
        var rearEdges=new List<int>(); var componentEdges=new List<int>();
        for(int x=0;x<width;x++)
        {
            float logicalX=trimLeft+(x+0.5f)/scale;
            int column=(int)logicalX/columnWidth;
            var cuts=new SortedSet<int>{0,height};
            Sample(rearPolygon,logicalX,rearEdges,cuts,height,trimTop,scale);
            Sample(separateComponent,logicalX,componentEdges,cuts,height,trimTop,scale);
            var foreground=new List<int>[preserved.Length];
            for(int p=0;p<preserved.Length;p++)
            {foreground[p]=new List<int>();Sample(preserved[p],logicalX,foreground[p],cuts,height,trimTop,scale);}
            foreach(var span in supports) if(span.Column==column)
            {cuts.Add(Clamp(checked((span.Top-trimTop)*scale),height));cuts.Add(Clamp(checked((span.Bottom-trimTop)*scale),height));}
            var rows=new List<int>(cuts);int ceiling=CeilingAt(original,x);
            for(int i=0;i+1<rows.Count;i++)
            {
                int a=rows[i],b=rows[i+1];if(a==b)continue;
                bool front=false;foreach(var edges in foreground)if(Contains(edges,a,b)){front=true;break;}
                bool rear=Contains(rearEdges,a,b)&&!front;int support=int.MaxValue;
                if(!front)
                {
                    if(rear||permitFrontSupports)foreach(var span in supports)
                        if(span.Column==column&&a>=(span.Top-trimTop)*scale&&b<=(span.Bottom-trimTop)*scale){support=span.Depth;break;}
                    if(!rear&&Contains(componentEdges,a,b))support=separateDepth;
                }
                Part kind=front?Part.Foreground:support!=int.MaxValue?(rear?Part.SupportRear:Part.SupportFront):rear?Part.Rear:Part.Front;
                result.Add(new Quad(x,a,1,b-a,support,ceiling,kind));
            }
        }
        return result;
    }

    // Only for PartitionAperture output (RefineSupport already resolves depth).
    // Instance placement resolves source ceiling and paired ordering separately;
    // a cache should store local parameters, not a different instance's result.
    public static int InstanceOrder(Quad quad,int sourceY,int projectionZ,int pairedOrder)
    {
        int original=checked(sourceY+quad.Ceiling);
        switch(quad.Kind)
        {
            case Part.Foreground:return Math.Max(original,checked(pairedOrder+1));
            case Part.Rear:return Math.Min(original,checked(pairedOrder-1));
            case Part.SupportFront:return Math.Min(original,checked(sourceY+quad.Depth-projectionZ));
            case Part.SupportRear:return Math.Min(Math.Min(original,checked(sourceY+quad.Depth-projectionZ)),checked(pairedOrder-1));
            default:return original;
        }
    }

    // Equal depth alone is insufficient for unresolved paired geometry: keep
    // original ceiling and front/rear identity until instance order is resolved.
    public static List<List<Quad>> GroupCompatible(IReadOnlyList<Quad> quads)
    {
        const int quadsPerMesh=ushort.MaxValue/4;
        var result=new List<List<Quad>>();
        var groups=new Dictionary<(int,int,Part),List<Quad>>();
        foreach(var q in quads)
        {
            var key=(q.Ceiling,q.Depth,q.Kind);
            if(!groups.TryGetValue(key,out var group)||group.Count==quadsPerMesh)
            {groups[key]=group=new List<Quad>();result.Add(group);}
            group.Add(q);
        }
        return result;
    }
    private static int CeilingAt(IReadOnlyList<Band> bands,int x)
    {foreach(var b in bands)if(x>=b.Left&&x<checked(b.Left+b.Width))return b.Depth;throw new ArgumentException("Missing source band.");}
    private static int Clamp(int value,int height)=>Math.Max(0,Math.Min(height,value));
    private static void Dimensions(int width,int height,int scale,int column)
    {if(width<=0||height<=0||scale<=0||column<=0)throw new ArgumentException("Invalid dimensions.");}
    private static bool Contains(List<int> edges,int top,int bottom)
    {for(int i=0;i+1<edges.Count;i+=2)if(top>=edges[i]&&bottom<=edges[i+1])return true;return false;}
    private static void Sample(Point[] polygon,float x,List<int> edges,SortedSet<int> cuts,int height,int trimTop,int scale)
    {
        edges.Clear();if(polygon==null)return;
        var crossings=new List<float>();
        for(int i=0;i<polygon.Length;i++)
        {
            Point a=polygon[i],b=polygon[(i+1)%polygon.Length];
            if((a.X<=x&&b.X>x)||(b.X<=x&&a.X>x))crossings.Add(a.Y+(x-a.X)*(b.Y-a.Y)/(b.X-a.X));
        }
        crossings.Sort();if(crossings.Count%2!=0)return;
        foreach(float y in crossings){int edge=Clamp((int)Math.Ceiling((y-trimTop)*scale),height);edges.Add(edge);cuts.Add(edge);}
    }
}
}
