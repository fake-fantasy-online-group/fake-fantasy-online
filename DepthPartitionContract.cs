using System;using System.Collections.Generic;using OwnCodeArchive;using static OwnCodeArchive.DepthPartition;
static class DepthPartitionContract
{
 static int checks;static void Check(bool value){checks++;if(!value)throw new Exception("Synthetic contract failed");}
 static void Main(){
  // Entirely invented tiny inputs; no resource-derived fixtures or coordinates.
  var native=new[]{new Band(0,4,9),new Band(4,4,6)};
  var spans=new[]{new Span(0,2,5,4),new Span(1,1,6,3)};
  foreach(int z in new[]{-2,0,2}){
   var q=RefineSupport(8,7,1,4,z,native,spans);int[,]seen=new int[8,7],order=new int[8,7];
   foreach(var r in q)for(int x=r.Left;x<r.Left+r.Width;x++)for(int y=r.Top;y<r.Top+r.Height;y++){seen[x,y]++;order[x,y]=r.Depth;}
   for(int x=0;x<8;x++)for(int y=0;y<7;y++){Check(seen[x,y]==1);int front=x<4?9:6;bool support=x<4?y>=2&&y<5:y>=1&&y<6;Check(order[x,y]==(support?Math.Min(front,(x<4?4:3)-z):front));}
  }
  var polygon=new[]{new Point(1,1),new Point(7,1),new Point(7,6),new Point(1,6)};
  var island=new[]{new Point(3,2),new Point(5,2),new Point(5,4),new Point(3,4)};
  var a=PartitionAperture(8,7,1,0,0,4,native,polygon,new[]{island},spans,true,null,0);int[,]count=new int[8,7];
  foreach(var r in a)for(int x=r.Left;x<r.Left+r.Width;x++)for(int y=r.Top;y<r.Top+r.Height;y++){count[x,y]++;if(x>=3&&x<5&&y>=2&&y<4)Check(r.Kind==Part.Foreground);}
  for(int x=0;x<8;x++)for(int y=0;y<7;y++)Check(count[x,y]==1);
  int grouped=0;foreach(var group in GroupCompatible(a)){grouped+=group.Count;foreach(var r in group){Check(r.Kind==group[0].Kind&&r.Ceiling==group[0].Ceiling&&r.Depth==group[0].Depth);Check(group.Count*4<=ushort.MaxValue);}}
  Check(grouped==a.Count);Check(InstanceOrder(new Quad(0,0,1,1,4,9,Part.SupportRear),20,2,30)==22);
  Console.WriteLine("PASS "+checks+" wholly synthetic code-only archive checks");
 }
}
