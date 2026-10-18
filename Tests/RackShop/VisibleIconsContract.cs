using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class Texture2D { public int Id; public Texture2D(int id) { Id=id; } }
 public struct Rect { }
 public static class Debug { public static int Warnings; public static void LogWarning(string text) { Warnings++; } }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public class ResLoader { public static ResLoader I = new ResLoader(); }
 public static class NetClient {
  public static readonly List<int> Loaded = new List<int>();
  public static bool Refreshed;
  public static Texture2D LoadItemIcon(int id) { Loaded.Add(id); if(id==6000) throw new Exception("bad resource"); return id==5000 && !Refreshed ? null : new Texture2D(Refreshed ? id+10000 : id); }
 }
 public static class ItemIconUI { public static void Draw(Rect r, Texture2D icon, float s) { } }
 public interface IRackShopGoodsReceiveBuffer { void Add(WorldRackShopGood row); void SetBlocked(int index,byte blocked); void Commit(IReadOnlyList<WorldRackShopCategory> cats,bool holiday,bool refunds); }
 public class WorldRackShopGood { public int Id,Weight,Price,Remain; public string Name,Description,Part; public byte Group,Flags,Blocked; }
 public class WorldRackShopCategory { public byte Id; public string Name; }
 public sealed partial class RackShopDlg {
  public struct Good { public int id,weight,price,remain; public string name,desc,part; public byte group,flags,blocked; public Texture2D icon; }
  private static List<Good> goods;
  private static RackShopDlg I;
  private int viewCount, page;
  private void PruneCart() { }
  public static void SetCats(List<(byte,string)> cats) { }
  public static void SetHoliday(bool value) { }
  public static void SetRefundTab(bool value) { }
  private float s=1;
  static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
  public static void Main() {
   var window = new RackShopDlg(); I=window;
   var buffer = ReferenceRackShopReceipt.CreateGoodsBuffer(996);
   for(int i=1;i<=996;i++) buffer.Add(new WorldRackShopGood{Id=i,Name="item"+i,Price=i,Remain=-1});
   buffer.SetBlocked(20,1);
   buffer.Commit(new List<WorldRackShopCategory>(),false,true);
   Check(goods.Count==996 && goods[20].blocked==1 && goods[20].price==21,"receipt preserves catalog and masks");
   Check(NetClient.Loaded.Count==0,"catalog construction performs no icon reads");
   for(int i=0;i<5;i++) window.DrawGoodIcon(new Rect(),goods[i]);
   for(int i=0;i<12;i++) window.DrawGoodIcon(new Rect(),goods[i]);
   Check(NetClient.Loaded.Count==12,"first page plus featured rows loads only twelve unique icons");
   for(int pass=0;pass<3;pass++) for(int i=0;i<12;i++) window.DrawGoodIcon(new Rect(),goods[i]);
   Check(NetClient.Loaded.Count==12,"layout/repaint and reopening reuse icons");
   for(int i=12;i<24;i++) Check(window.ResolveGoodIcon(goods[i]).Id==i+1,"second page resolves its own item");
   Check(NetClient.Loaded.Count==24,"page switch loads twelve new icons");
   window.DrawGoodIcon(new Rect(),goods[995]);
   Check(NetClient.Loaded.Count==25,"cart/filter row outside current page can resolve");
   window.ResolveGoodIcon(new Good{id=90000001}); window.ResolveGoodIcon(new Good{id=90000002});
   Check(NetClient.Loaded.Count==26 && NetClient.Loaded[25]==3385,"pet rows share mapped icon");
   for(int i=0;i<3;i++) Check(window.ResolveGoodIcon(new Good{id=5000})==null,"missing icon keeps blank placeholder");
   Check(NetClient.Loaded.Count==27,"missing icon read once");
   for(int i=0;i<3;i++) Check(window.ResolveGoodIcon(new Good{id=6000})==null,"bad icon is nonfatal");
   Check(NetClient.Loaded.Count==28 && Debug.Warnings==1,"decoder failure logged/read once");
   ResLoader.I=null; Check(window.ResolveGoodIcon(new Good{id=7000})==null,"not ready stays blank");
   ResLoader.I=new ResLoader(); Check(window.ResolveGoodIcon(new Good{id=7000}).Id==7000,"not-ready result retried later");
   var supplied=new Texture2D(123); Check(window.ResolveGoodIcon(new Good{id=7001,icon=supplied})==supplied,"existing icon preserved");
   Check(window.ResolveGoodIcon(new Good{id=0})==null,"empty cart lookup does not read invalid id");
   NetClient.Refreshed=true;
   Check(window.ResolveGoodIcon(goods[0]).Id==1,"cached mapping held until new catalog");
   int beforeRefresh=NetClient.Loaded.Count;
   buffer.Commit(new List<WorldRackShopCategory>(),false,true);
   Check(NetClient.Loaded.Count==beforeRefresh,"replacement catalog still performs no eager reads");
   Check(window.viewCount==-1 && window.page==0,"replacement retains native view/page reset");
   Check(window.ResolveGoodIcon(goods[0]).Id==10001,"new receipt refreshes changed icon mapping");
   Check(window.ResolveGoodIcon(new Good{id=5000}).Id==15000,"new receipt retries previously missing icon");
   Check(NetClient.Loaded.Count==beforeRefresh+2,"only displayed replacement icons loaded");
   Console.WriteLine("PASS: visible rack icons, paging, reuse, cart/filter, pets, missing/corrupt resources, late loader, refreshed mapping/missing icons");
  }
 }
}
