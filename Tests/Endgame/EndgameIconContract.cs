using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public sealed class Texture2D {}
 public static class Debug { public static int Warnings; public static void LogWarning(object value) { Warnings++; } }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal sealed class ResLoader {
  public readonly List<string> Calls = new List<string>();
  public string Available;
  public UnityEngine.Texture2D LoadTex(string path, int frame) { Calls.Add(path); return path == Available ? new UnityEngine.Texture2D() : null; }
 }
 internal partial class NetClient {
  internal static UnityEngine.Texture2D TestFallback(int id, ResLoader loader) { return LoadEndgameIconFallback(id, loader); }
  internal static void ResetWarnings() { missingEndgameIcons.Clear(); UnityEngine.Debug.Warnings = 0; }
 }
}
class EndgameIconContract {
 static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
 static void Main() {
  var runeIcons = new HashSet<int>();
  for (int i=0; i<28; i++) runeIcons.Add(NetClient.EndgameFallbackIconID(1900028001+i));
  Check(runeIcons.Count == 7 && !runeIcons.Contains(0), "seven elemental fallbacks");
  var allIcons = new HashSet<int>(runeIcons);
  foreach (int id in new [] {1900028101,1900028201,1900028202,1900028203}) allIcons.Add(NetClient.EndgameFallbackIconID(id));
  Check(allIcons.Count == 11, "key and materials retain distinct fallback motifs");
  foreach (int id in new [] {0,1900028000,1900028029,1900028100,1900028204,int.MaxValue}) {
   var loader = new ResLoader();
   Check(NetClient.EndgameFallbackIconID(id) == 0 && NetClient.TestFallback(id, loader) == null && loader.Calls.Count == 0, "unknown item isolation");
  }
  NetClient.ResetWarnings();
  var small = new ResLoader {Available="item/item/item3303.gso"};
  Check(NetClient.TestFallback(1900028101, small) != null && small.Calls.Count == 1, "direct sprite fallback");
  Check(UnityEngine.Debug.Warnings == 1, "missing primary diagnostic");
  NetClient.TestFallback(1900028101, small);
  Check(UnityEngine.Debug.Warnings == 1, "diagnostic deduplication");
  var big = new ResLoader {Available="item/itempic/itempic3559.gso"};
  Check(NetClient.TestFallback(1900028201, big) != null && big.Calls.Count == 2, "itempic fallback");
  var missing = new ResLoader();
  Check(NetClient.TestFallback(1900028202, missing) == null && missing.Calls.Count == 2, "all resources absent is bounded and safe");
  Console.WriteLine("PASS EndgameIconContract: 28 runes, 11 semantic fallbacks, unknown IDs, missing resources, bounded loads, diagnostics");
 }
}
