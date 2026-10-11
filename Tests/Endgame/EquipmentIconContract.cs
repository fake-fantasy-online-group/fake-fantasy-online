// Executes the actual native loader, 0x804d receiver, UI adapter and ground cache.
// Unity textures and external resource/table IO are deterministic substitutes.
using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Presentation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using Gateway = FakeFantasyOnline.Protocol.PrivateServerGateway;
namespace UnityEngine {
 public sealed class Texture2D { public string Path; }
 public struct Color32 { public Color32(byte r, byte g, byte b, byte a) {} }
 public static class Debug { public static void LogWarning(object value) {} }
 public static class JsonUtility { public static T FromJson<T>(string text) { throw new Exception("unexpected table JSON"); } }
}
namespace FakeFantasyOnline.Application {
 public interface ISkillBookReader { int I32(); int U16(); int Remaining { get; } }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal static class GameTables { public static string LoadText(string name) { return null; } }
 internal sealed class ResLoader {
  public static ResLoader I;
  public readonly Dictionary<string, UnityEngine.Texture2D> Textures = new Dictionary<string, UnityEngine.Texture2D>();
  public readonly List<string> Calls = new List<string>();
  public UnityEngine.Texture2D Add(string path) { if (Textures.TryGetValue(path, out var old)) return old; var t = new UnityEngine.Texture2D {Path=path}; Textures[path]=t; return t; }
  public UnityEngine.Texture2D LoadTex(string path, int frame) { Calls.Add(path); return Textures.TryGetValue(path, out var t) ? t : null; }
  public UnityEngine.Texture2D LoadUI(string path) { return LoadTex(path,0); }
  public UnityEngine.Texture2D LoadGsa(string path) { return LoadTex(path,0); }
 }
 internal partial class NetClient {
  private static readonly Dictionary<int,UnityEngine.Texture2D> _grayIcon = new Dictionary<int,UnityEngine.Texture2D>();
  private static UnityEngine.Texture2D Desaturate(UnityEngine.Texture2D texture) { return texture; }
  internal static void TestReset() { _iconSm=null; _iconBig=null; _mountModel=null; _grayIcon.Clear(); GroundItems.ClearIconCache(); }
  internal static void TestInstalledMapping(int id, int sprite) { EnsureIconMap(); _iconSm[id]=sprite; _iconBig[id]=sprite; }
 }
}
namespace FakeFantasyOnline.Protocol {
 public sealed partial class PrivateServerGateway {
  public static void TestReceive(byte[] payload, FakeFantasyOnline.Application.IItemIconOverrideReceiveContext context) { ReceiveIconOverrides(payload,context); }
 }
}
class EquipmentIconContract {
 static int checks;
 static void Check(bool ok,string why) { checks++; if(!ok) throw new Exception(why); }
 static byte[] Map(params int[] pairs) {
  using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream)) {
   writer.Write((ushort)(pairs.Length/2)); foreach(int value in pairs) writer.Write(value); return stream.ToArray();
  }
 }
 static void Main() {
  NetClient.TestReset(); var loader=new ResLoader(); ResLoader.I=loader;
  var sword=loader.Add("item/item/item1001.gso"); var boots=loader.Add("item/item/item30041.gso");
  // Reproduce a stale installed table mapping without claiming the user's table
  // was available. The server must correct it before equipment is projected.
  NetClient.TestInstalledMapping(22581,1001);
  Check(NetClient.LoadItemIcon(22581)==sword,"stale shoe mapping reproduces sword");
  Check(GroundItems.LoadIconForIndependentRuntime(22581)==sword,"ground cache contains stale texture");
  Gateway.TestReceive(Map(22581,30041),new ClientRootView());
  Check(NetClient.LoadItemIcon(22581)==boots,"server-authoritative shoe sprite");
  Check(GroundItems.LoadIconForIndependentRuntime(22581)==boots,"804d clears stale ground cache through actual adapter");
  Gateway.TestReceive(Map(22581,30041),new ClientRootView());
  Check(NetClient.LoadItemIcon(22581)==boots,"repeat mapping is stable");
  // Prevent recursively treating a sprite ID as another item-table identity.
  NetClient.TestInstalledMapping(30041,1001);
  Check(NetClient.LoadItemIcon(22581)==boots,"sprite identity is not remapped as item identity");
  int[] pairs={2701,2701,2001,2001,2501,2501,1001,1001,1014,1102,2401,2401,2551,2551,2801,2801,2101,2101,2601,2601,1013,1501,24655,37336,24654,37335,24653,37334,40008,37314,40009,37315,40010,37316,57001,57001,57002,57002,57003,57003,57004,57004,57005,57005,57006,57006,20824,22090,22593,40330};
  for(int i=0;i<pairs.Length;i+=2) { loader.Add("item/item/item"+pairs[i+1]+".gso"); NetClient.TestInstalledMapping(pairs[i],1001); }
  Gateway.TestReceive(Map(pairs),new ClientRootView());
  for(int i=0;i<pairs.Length;i+=2) Check(NetClient.LoadItemIcon(pairs[i]).Path=="item/item/item"+pairs[i+1]+".gso","category/base sprite "+pairs[i]);
  Check(NetClient.LoadItemIcon(1001)==sword,"real sword retains original sprite");
  var png=loader.Add("itemicon/item98330.png");
  Check(NetClient.LoadItemIcon(98330)==png,"ordinary PNG-first loading retained");
  Gateway.TestReceive(Map(98330,1001),new ClientRootView());
  Check(NetClient.LoadItemIcon(98330)==png,"explicit per-item PNG keeps native priority over mappings");
  // Missing equipment art must remain absent, never share a generic sword.
  Gateway.TestReceive(Map(22581,30042),new ClientRootView());
  int before=loader.Calls.Count;
  Check(NetClient.LoadItemIcon(22581)==null,"missing equipment resources do not invent sword fallback");
  Check(loader.Calls.Count-before==5,"equipment missing-resource path remains bounded");
  var big=loader.Add("item/itempic/itempic30042.gso");
  Check(NetClient.LoadItemIcon(22581)==big,"native itempic fallback uses same mapped sprite");
  NetClient.TestReset(); ResLoader.I=new ResLoader();
  Check(NetClient.LoadItemIcon(22581)==null,"missing installed table and resources stay empty");
  ResLoader.I=null; Check(NetClient.LoadItemIcon(22581)==null,"missing resource loader stays safe");
  Console.WriteLine("PASS EquipmentIconContract: "+checks+" assertions; actual loader/804d adapter/ground cache; Unity and table IO substituted");
 }
}
